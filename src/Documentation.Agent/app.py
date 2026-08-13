import asyncio
import json
import logging
import math
import os
import re
import time
import uuid
from contextlib import asynccontextmanager
from contextvars import ContextVar
from typing import Annotated, Any

import grpc
import psycopg
from documentation import embeddings_pb2, embeddings_pb2_grpc
from fastapi import FastAPI, HTTPException, Request
from langchain.agents import create_agent
from langchain.agents.middleware import ModelFallbackMiddleware
from langchain.tools import tool
from langchain_openai import ChatOpenAI
from psycopg.rows import dict_row
from pydantic import BaseModel, StringConstraints

EMBEDDING_DIMENSIONS = 768
EMBEDDING_GRPC_ADDRESS = os.getenv("EMBEDDING_GRPC_ADDRESS", "localhost:8080")
EMBEDDING_GRPC_DEADLINE_SECONDS = 100
AGENT_MODEL = os.getenv("AGENT_MODEL", "nvidia/nemotron-3-ultra-550b-a55b")
AGENT_FALLBACK_MODEL = os.getenv("AGENT_FALLBACK_MODEL", "nvidia/nemotron-3-super-120b-a12b")
LLM_MAX_TOKENS = int(os.getenv("LLM_MAX_TOKENS", "512"))
LLM_BASE_URL = os.getenv("LLM_BASE_URL", "https://integrate.api.nvidia.com/v1")
LLM_API_KEY = os.getenv("LLM_API_KEY", "")
DATABASE_DSN = os.getenv(
    "DATABASE_DSN",
    "host=localhost port=5432 dbname=documentation_portal user=postgres password=postgres",
)

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
logger = logging.getLogger("documentation-agent")
logger.setLevel(logging.INFO)
correlation_id: ContextVar[str] = ContextVar("correlation_id", default="-")
CORRELATION_ID_PATTERN = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$")
embedding_client: "EmbeddingClient | None" = None
supervisor_agent: Any | None = None

ChatMessage = Annotated[str, StringConstraints(strip_whitespace=True, min_length=1, max_length=10_000)]


class ChatRequest(BaseModel):
    message: ChatMessage


class ChatResponse(BaseModel):
    answer: str


def _log(level: int, step: str, outcome: str, *, started: float | None = None, **fields: Any) -> None:
    values = [f"CorrelationId={correlation_id.get()}", f"Step={step}", f"Outcome={outcome}"]
    if started is not None:
        values.append(f"ElapsedMs={int((time.perf_counter() - started) * 1000)}")
    values.extend(f"{name}={value}" for name, value in fields.items())
    logger.log(level, " ".join(values))


class EmbeddingClient:
    def __init__(self, address: str):
        self.channel = grpc.aio.insecure_channel(address)
        self.stub = embeddings_pb2_grpc.EmbeddingServiceStub(self.channel)

    async def embed_query(self, text: str) -> list[float]:
        started = time.perf_counter()
        _log(logging.INFO, "embedding_query", "started")
        response = await self.stub.EmbedQuery(
            embeddings_pb2.EmbedRequest(text=text),
            timeout=EMBEDDING_GRPC_DEADLINE_SECONDS,
            metadata=(("x-correlation-id", correlation_id.get()),),
        )
        embedding = [float(value) for value in response.embedding]
        if len(embedding) != EMBEDDING_DIMENSIONS or not all(math.isfinite(value) for value in embedding):
            raise RuntimeError("Embedding service must return 768 finite dimensions.")
        _log(logging.INFO, "embedding_query", "completed", started=started, Dimensions=len(embedding))
        return embedding

    async def ready(self) -> None:
        await asyncio.wait_for(self.channel.channel_ready(), timeout=5)

    async def close(self) -> None:
        await self.channel.close()


def _get_embedding_client() -> EmbeddingClient:
    global embedding_client
    if embedding_client is None:
        embedding_client = EmbeddingClient(EMBEDDING_GRPC_ADDRESS)
    return embedding_client


def _vector_literal(embedding: list[float]) -> str:
    return "[" + ",".join(format(value, ".9g") for value in embedding) + "]"


def _check_database() -> None:
    with psycopg.connect(DATABASE_DSN, connect_timeout=5) as connection:
        connection.execute("SELECT 1")


def _search_database(embedding: list[float]) -> list[dict[str, Any]]:
    vector = _vector_literal(embedding)
    sql = """
        SELECT
            documentation."ApiId" AS api_id,
            documentation."Name" AS api_name,
            version."Version" AS version,
            version."Environment" AS environment,
            chunk.chunk_type,
            chunk.metadata,
            chunk.content,
            chunk.document_id::text AS document_id,
            chunk.version_id::text AS version_id,
            1 - (chunk.embedding <=> %s::vector) AS score
        FROM ingestion.document_chunks AS chunk
        INNER JOIN documentation.api_documentations AS documentation
            ON documentation."Id" = chunk.document_id
        INNER JOIN documentation.documentation_versions AS version
            ON version."Id" = chunk.version_id
        WHERE version."Status" = 'Available'
        ORDER BY chunk.embedding <=> %s::vector
        LIMIT 3;
    """
    with psycopg.connect(DATABASE_DSN, row_factory=dict_row, connect_timeout=5) as connection:
        rows = connection.execute(sql, (vector, vector)).fetchall()
    return [{**row, "score": float(row["score"]), "content": row["content"][:4_000]} for row in rows]


@tool
async def search_system_knowledge(query: str) -> str:
    """Search indexed API documentation."""
    started = time.perf_counter()
    _log(logging.INFO, "knowledge_search", "started")
    try:
        embedding = await _get_embedding_client().embed_query(query)
        results = await asyncio.to_thread(_search_database, embedding)
        _log(
            logging.INFO,
            "knowledge_search",
            "completed",
            started=started,
            ResultCount=len(results),
            BestScore=max((result["score"] for result in results), default=0),
        )
        return json.dumps(results, ensure_ascii=False)
    except (grpc.RpcError, RuntimeError) as exception:
        _log(
            logging.ERROR,
            "knowledge_search",
            "embedding_failed",
            started=started,
            ErrorType=type(exception).__name__,
        )
        return json.dumps({"error": "The embedding service is unavailable."})
    except psycopg.Error as exception:
        _log(
            logging.ERROR,
            "knowledge_search",
            "database_failed",
            started=started,
            ErrorType=type(exception).__name__,
        )
        return json.dumps({"error": "The vector knowledge base is unavailable."})


def _message_text(message: Any) -> str:
    text = getattr(message, "text", "")
    if isinstance(text, str) and text:
        return text.rsplit("</think>", 1)[-1].strip()
    content = getattr(message, "content", "")
    if isinstance(content, str):
        return content.rsplit("</think>", 1)[-1].strip()
    if isinstance(content, list):
        text = "\n".join(block.get("text", "") for block in content if isinstance(block, dict) and block.get("text"))
        return text.rsplit("</think>", 1)[-1].strip()
    return str(content)


def _models() -> tuple[ChatOpenAI, ChatOpenAI]:
    options = dict(api_key=LLM_API_KEY, base_url=LLM_BASE_URL, max_tokens=LLM_MAX_TOKENS, reasoning_effort="none")
    primary = ChatOpenAI(model=AGENT_MODEL, **options)
    fallback = ChatOpenAI(model=AGENT_FALLBACK_MODEL, **options)
    return primary, fallback


def _get_supervisor() -> Any:
    global supervisor_agent
    if supervisor_agent is not None:
        return supervisor_agent
    model, fallback = _models()
    _log(logging.INFO, "agent_initialization", "started", PrimaryModel=AGENT_MODEL, FallbackModel=AGENT_FALLBACK_MODEL)
    feature_agent = create_agent(
        model=model,
        tools=[search_system_knowledge],
        middleware=[ModelFallbackMiddleware(fallback)],
        name="feature_integration_agent",
        system_prompt=(
            "Você é um consultor técnico do Documentation Portal. Antes de responder, "
            "sempre use search_system_knowledge para consultar a documentação indexada. "
            "Não invente contratos ausentes. Responda em português com resumo, componentes "
            "impactados, contratos/fluxo e testes recomendados. Cite api_id, versão e ambiente "
            "dos chunks usados. O supervisor verá apenas sua resposta final."
        ),
    )

    @tool
    async def consult_feature_integration_specialist(request: str) -> str:
        """Delegate a Documentation Portal feature-integration question to the specialist."""
        started = time.perf_counter()
        _log(logging.INFO, "specialist_delegation", "started")
        try:
            result = await feature_agent.ainvoke({"messages": [{"role": "user", "content": request}]})
            answer = _message_text(result["messages"][-1])
            _log(logging.INFO, "specialist_delegation", "completed", started=started, AnswerLength=len(answer))
            return answer
        except Exception as exception:
            _log(
                logging.ERROR,
                "specialist_delegation",
                "failed",
                started=started,
                ErrorType=type(exception).__name__,
            )
            raise

    supervisor_agent = create_agent(
        model=model,
        tools=[consult_feature_integration_specialist],
        middleware=[ModelFallbackMiddleware(fallback)],
        name="documentation_supervisor",
        system_prompt=(
            "Você coordena o consultor de integração do Documentation Portal. "
            "Sempre delegue a solicitação recebida para consult_feature_integration_specialist "
            "e entregue ao usuário uma resposta concisa e fundamentada."
        ),
    )
    _log(logging.INFO, "agent_initialization", "completed")
    return supervisor_agent


@asynccontextmanager
async def lifespan(_: FastAPI):
    global embedding_client
    client = _get_embedding_client()
    _log(logging.INFO, "service_lifecycle", "started")
    yield
    await client.close()
    embedding_client = None
    _log(logging.INFO, "service_lifecycle", "stopped")


app = FastAPI(title="Documentation Agent", version="1.0.0", lifespan=lifespan)


@app.middleware("http")
async def correlate_request(request: Request, call_next: Any):
    supplied_ids = request.headers.getlist("X-Correlation-ID")
    supplied_id = supplied_ids[0] if len(supplied_ids) == 1 else ""
    request_id = supplied_id if CORRELATION_ID_PATTERN.fullmatch(supplied_id) else uuid.uuid4().hex
    token = correlation_id.set(request_id)
    started = time.perf_counter()
    try:
        response = await call_next(request)
        response.headers["X-Correlation-ID"] = request_id
        if request.url.path != "/health" or response.status_code >= 400:
            _log(
                logging.INFO if response.status_code < 400 else logging.WARNING,
                "http_request",
                "completed" if response.status_code < 400 else "failed",
                started=started,
                Method=request.method,
                Path=request.url.path,
                StatusCode=response.status_code,
            )
        return response
    except Exception as exception:
        _log(
            logging.ERROR,
            "http_request",
            "failed",
            started=started,
            Method=request.method,
            Path=request.url.path,
            ErrorType=type(exception).__name__,
        )
        raise
    finally:
        correlation_id.reset(token)


@app.get("/health")
async def health() -> dict[str, str]:
    try:
        await asyncio.gather(asyncio.to_thread(_check_database), _get_embedding_client().ready())
    except psycopg.Error as exception:
        _log(logging.WARNING, "health_check", "failed", Dependency="postgres", ErrorType=type(exception).__name__)
        raise HTTPException(status_code=503, detail="PostgreSQL is unavailable.") from exception
    except (asyncio.TimeoutError, grpc.RpcError) as exception:
        _log(logging.WARNING, "health_check", "failed", Dependency="embeddings", ErrorType=type(exception).__name__)
        raise HTTPException(status_code=503, detail="Embedding service is unavailable.") from exception
    return {"status": "healthy"}


@app.post("/api/agents/chat", response_model=ChatResponse)
async def chat(request: ChatRequest) -> ChatResponse:
    started = time.perf_counter()
    _log(logging.INFO, "agent_invocation", "started")
    try:
        result = await _get_supervisor().ainvoke({"messages": [{"role": "user", "content": request.message}]})
        answer = _message_text(result["messages"][-1])
        _log(logging.INFO, "agent_invocation", "completed", started=started, AnswerLength=len(answer))
        return ChatResponse(answer=answer)
    except Exception as exception:
        _log(
            logging.ERROR,
            "agent_invocation",
            "failed",
            started=started,
            ErrorType=type(exception).__name__,
        )
        raise HTTPException(status_code=502, detail="Agent invocation failed.") from exception
