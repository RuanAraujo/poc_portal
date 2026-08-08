import asyncio
import json
import logging
import math
import os
import threading
from contextlib import asynccontextmanager
from typing import Annotated, Any

import psycopg
from fastapi import FastAPI, HTTPException
from langchain.agents import create_agent
from langchain.tools import tool
from langchain_openai import ChatOpenAI
from psycopg.rows import dict_row
from pydantic import BaseModel, StringConstraints
from sentence_transformers import SentenceTransformer

EMBEDDING_MODEL = os.getenv("EMBEDDING_MODEL", "google/embeddinggemma-300m")
EMBEDDING_DIMENSIONS = int(os.getenv("EMBEDDING_DIMENSIONS", "768"))
AGENT_MODEL = os.getenv("AGENT_MODEL", "qwen3:4b")
LLM_MAX_TOKENS = int(os.getenv("LLM_MAX_TOKENS", "512"))
LLM_BASE_URL = os.getenv("LLM_BASE_URL", "http://localhost:11434/v1")
LLM_API_KEY = os.getenv("LLM_API_KEY", "ollama")
DATABASE_DSN = os.getenv(
    "DATABASE_DSN",
    "host=localhost port=5432 dbname=documentation_portal user=postgres password=postgres",
)

if EMBEDDING_DIMENSIONS != 768:
    raise RuntimeError("EmbeddingGemma must use 768 dimensions in this service.")

logger = logging.getLogger("documentation-agent")
embedding_model: SentenceTransformer | None = None
supervisor_agent: Any | None = None
# ponytail: one CPU model is serialized; add batching/worker coordination only if throughput requires it.
embedding_lock = threading.Lock()

EmbeddingText = Annotated[str, StringConstraints(strip_whitespace=True, min_length=1, max_length=200_000)]
ChatMessage = Annotated[str, StringConstraints(strip_whitespace=True, min_length=1, max_length=10_000)]


class EmbeddingRequest(BaseModel):
    text: EmbeddingText


class EmbeddingResponse(BaseModel):
    embedding: list[float]
    dimensions: int


class ChatRequest(BaseModel):
    message: ChatMessage


class ChatResponse(BaseModel):
    answer: str


def _load_embedding_model() -> SentenceTransformer:
    model = SentenceTransformer(EMBEDDING_MODEL, device="cpu")
    probe = model.encode_document("health check", normalize_embeddings=True)
    if len(probe) != EMBEDDING_DIMENSIONS:
        raise RuntimeError(
            f"{EMBEDDING_MODEL} returned {len(probe)} dimensions; {EMBEDDING_DIMENSIONS} are required."
        )
    return model


def _normalize(values: Any) -> list[float]:
    vector = [float(value) for value in values.tolist()]
    magnitude = math.sqrt(sum(value * value for value in vector))
    if len(vector) != EMBEDDING_DIMENSIONS or magnitude == 0:
        raise RuntimeError("EmbeddingGemma returned an invalid embedding.")
    return [value / magnitude for value in vector]


def _encode_document(text: str) -> list[float]:
    if embedding_model is None:
        raise RuntimeError("EmbeddingGemma is not loaded.")
    with embedding_lock:
        return _normalize(embedding_model.encode_document(text, normalize_embeddings=True))


def _encode_query(text: str) -> list[float]:
    if embedding_model is None:
        raise RuntimeError("EmbeddingGemma is not loaded.")
    with embedding_lock:
        return _normalize(embedding_model.encode_query(text, normalize_embeddings=True))


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

    return [
        {
            **row,
            "score": float(row["score"]),
            "content": row["content"][:4_000],
        }
        for row in rows
    ]


@tool
async def search_system_knowledge(query: str) -> str:
    """Search indexed API documentation.

    Returns a JSON array with up to three results. Each result contains api_id,
    api_name, version, environment, chunk_type, metadata, content, document_id,
    version_id, and cosine score. Returns an empty array when nothing is indexed.
    """
    try:
        embedding = await asyncio.to_thread(_encode_query, query)
        results = await asyncio.to_thread(_search_database, embedding)
        return json.dumps(results, ensure_ascii=False)
    except psycopg.Error:
        logger.exception("Vector search failed.")
        return json.dumps({"error": "The vector knowledge base is unavailable."})


def _message_text(message: Any) -> str:
    text = getattr(message, "text", "")
    if isinstance(text, str) and text:
        return text.rsplit("</think>", 1)[-1].strip()

    content = getattr(message, "content", "")
    if isinstance(content, str):
        return content.rsplit("</think>", 1)[-1].strip()
    if isinstance(content, list):
        text = "\n".join(
            block.get("text", "")
            for block in content
            if isinstance(block, dict) and block.get("text")
        )
        return text.rsplit("</think>", 1)[-1].strip()
    return str(content)


def _get_supervisor() -> Any:
    global supervisor_agent
    if supervisor_agent is not None:
        return supervisor_agent
    model = ChatOpenAI(
        model=AGENT_MODEL,
        api_key=LLM_API_KEY,
        base_url=LLM_BASE_URL,
        max_tokens=LLM_MAX_TOKENS,
        reasoning_effort="none",
    )
    feature_agent = create_agent(
        model=model,
        tools=[search_system_knowledge],
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
        result = await feature_agent.ainvoke(
            {"messages": [{"role": "user", "content": request}]}
        )
        return _message_text(result["messages"][-1])

    supervisor_agent = create_agent(
        model=model,
        tools=[consult_feature_integration_specialist],
        name="documentation_supervisor",
        system_prompt=(
            "Você coordena o consultor de integração do Documentation Portal. "
            "Sempre delegue a solicitação recebida para consult_feature_integration_specialist "
            "e entregue ao usuário uma resposta concisa e fundamentada."
        ),
    )
    return supervisor_agent


@asynccontextmanager
async def lifespan(_: FastAPI):
    global embedding_model
    embedding_model = await asyncio.to_thread(_load_embedding_model)
    logger.info("Embedding model %s loaded locally on CPU.", EMBEDDING_MODEL)
    yield
    embedding_model = None


app = FastAPI(title="Documentation Agent", version="1.0.0", lifespan=lifespan)


@app.get("/health")
async def health() -> dict[str, str]:
    if embedding_model is None:
        raise HTTPException(status_code=503, detail="EmbeddingGemma is not loaded.")
    try:
        await asyncio.to_thread(_check_database)
    except psycopg.Error as exception:
        raise HTTPException(status_code=503, detail="PostgreSQL is unavailable.") from exception
    return {"status": "healthy"}


@app.post("/internal/embeddings", response_model=EmbeddingResponse)
def create_document_embedding(request: EmbeddingRequest) -> EmbeddingResponse:
    try:
        embedding = _encode_document(request.text)
    except RuntimeError as exception:
        raise HTTPException(status_code=503, detail=str(exception)) from exception
    return EmbeddingResponse(embedding=embedding, dimensions=len(embedding))


@app.post("/api/agents/chat", response_model=ChatResponse)
async def chat(request: ChatRequest) -> ChatResponse:
    try:
        supervisor = _get_supervisor()
        result = await supervisor.ainvoke(
            {"messages": [{"role": "user", "content": request.message}]}
        )
        return ChatResponse(answer=_message_text(result["messages"][-1]))
    except HTTPException:
        raise
    except Exception as exception:
        logger.exception("Agent invocation failed.")
        raise HTTPException(status_code=502, detail="Agent invocation failed.") from exception
