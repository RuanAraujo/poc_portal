import ast
import asyncio
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import AsyncMock, Mock, call, patch

import app
from fastapi import FastAPI
from fastapi.testclient import TestClient
from langchain.agents.middleware import ModelFallbackMiddleware
from langchain.agents.middleware.types import ModelRequest

from documentation_agent.application.errors import AgentInvocationFailed, EmbeddingUnavailable, KnowledgeBaseUnavailable
from documentation_agent.application.services import ChatUseCase, HealthUseCase, KnowledgeSearchUseCase
from documentation_agent.infrastructure.agents import LangChainAgentGateway, message_text
from documentation_agent.infrastructure.agents.gateway import ThoughtFilter
from documentation_agent.infrastructure.config import Settings
from documentation_agent.infrastructure.embeddings import EMBEDDING_GRPC_DEADLINE_SECONDS, GrpcEmbeddingGateway
from documentation_agent.infrastructure.repositories import vector_literal
from documentation_agent.infrastructure.tools import create_search_system_knowledge
from documentation_agent.interface_adapters.http import create_router
from documentation_agent.interface_adapters.observability import logger
from documentation_agent.interface_adapters.web import add_web_concerns


class FailingAgent:
    async def respond(self, _):
        raise AgentInvocationFailed() from RuntimeError("SECRET-PROMPT-MARKER")
        yield ""


class SuccessfulAgent:
    async def respond(self, _):
        yield "answer"


class HealthyEmbeddings:
    async def start(self): pass
    async def ready(self): pass


class HealthyRepository:
    def check(self): pass


def make_test_app(agent=None, embeddings=None, repository=None):
    application = FastAPI()
    add_web_concerns(application, "http://localhost:3000")
    application.include_router(create_router(ChatUseCase(agent or SuccessfulAgent()), HealthUseCase(embeddings or HealthyEmbeddings(), repository or HealthyRepository())))
    return application


class AgentTests(unittest.TestCase):
    def test_embedding_client_queries_grpc_with_deadline_and_correlation(self):
        stub = SimpleNamespace(EmbedQuery=AsyncMock(return_value=SimpleNamespace(embedding=[1.0] * 768)))
        client = GrpcEmbeddingGateway("address", Mock(), lambda: "grpc-correlation", stub=stub)

        embedding = asyncio.run(client.embed_query("query"))

        self.assertEqual(len(embedding), 768)
        self.assertEqual(stub.EmbedQuery.await_args.args[0].text, "query")
        self.assertEqual(stub.EmbedQuery.await_args.kwargs["timeout"], EMBEDDING_GRPC_DEADLINE_SECONDS)
        self.assertEqual(stub.EmbedQuery.await_args.kwargs["metadata"], (("x-correlation-id", "grpc-correlation"),))

    def test_embedding_client_rejects_wrong_dimension(self):
        stub = SimpleNamespace(EmbedQuery=AsyncMock(return_value=SimpleNamespace(embedding=[1.0])))
        client = GrpcEmbeddingGateway("address", Mock(), lambda: "-", stub=stub)

        with self.assertRaises(EmbeddingUnavailable):
            asyncio.run(client.embed_query("query"))

    def test_embedding_gateway_lazily_starts_and_resets_after_close(self):
        channel = SimpleNamespace(close=AsyncMock())
        stub = SimpleNamespace(EmbedQuery=AsyncMock())
        client = GrpcEmbeddingGateway("address", Mock(), lambda: "-", channel=channel, stub=stub)

        asyncio.run(client.start())
        asyncio.run(client.close())
        asyncio.run(client.close())

        channel.close.assert_awaited_once()
        self.assertIsNone(client.channel)
        self.assertIsNone(client.stub)

    def test_application_lifecycle_starts_and_closes_embeddings(self):
        embeddings = Mock(start=AsyncMock(), close=AsyncMock())
        with patch.object(app, "GrpcEmbeddingGateway", return_value=embeddings):
            with TestClient(app.create_app()):
                pass
        embeddings.start.assert_awaited_once()
        embeddings.close.assert_awaited_once()

    def test_vector_literal_is_pgvector_compatible(self):
        self.assertEqual(vector_literal([0.5, -0.25]), "[0.5,-0.25]")

    def test_message_text_hides_model_thinking(self):
        self.assertEqual(message_text(SimpleNamespace(text="", content="<think>secret</think>answer")), "answer")

    def test_native_fallback_uses_secondary_after_primary_fails(self):
        primary, fallback, attempts = object(), object(), []

        async def handler(request):
            attempts.append(request.model)
            if request.model is primary:
                raise RuntimeError("primary unavailable")
            return "fallback answer"

        result = asyncio.run(ModelFallbackMiddleware(fallback).awrap_model_call(ModelRequest(model=primary, messages=[]), handler))
        self.assertEqual(result, "fallback answer")
        self.assertEqual(attempts, [primary, fallback])

    @patch("documentation_agent.infrastructure.agents.gateway.create_agent")
    @patch("documentation_agent.infrastructure.agents.gateway.ModelFallbackMiddleware")
    @patch("documentation_agent.infrastructure.agents.gateway.ChatOpenAI")
    def test_langchain_gateway_configures_models_and_fallbacks(self, chat_openai, fallback_middleware, create_agent):
        primary, fallback = Mock(), Mock()
        chat_openai.side_effect = [primary, fallback]
        create_agent.side_effect = [Mock(), Mock()]
        settings = Settings(llm_api_key="key", llm_base_url="url", llm_max_tokens=123)
        gateway = LangChainAgentGateway(settings, Mock(), Mock())

        gateway._supervisor()

        options = dict(api_key="key", base_url="url", max_tokens=123, reasoning_effort="none", streaming=True)
        self.assertEqual(chat_openai.call_args_list, [call(model=settings.agent_model, **options), call(model=settings.agent_fallback_model, **options)])
        self.assertEqual(fallback_middleware.call_args_list, [call(fallback), call(fallback)])
        self.assertEqual(create_agent.call_args_list[0].kwargs["middleware"], [fallback_middleware.return_value])
        self.assertEqual(create_agent.call_args_list[1].kwargs["middleware"], [fallback_middleware.return_value])

    def test_streams_only_final_supervisor_tokens_and_filters_fragmented_thinking(self):
        async def events():
            yield "messages", (SimpleNamespace(content="specialist"), {"langgraph_node": "feature_integration_agent"})
            yield "messages", (SimpleNamespace(content="delegating"), {"langgraph_node": "model"})
            yield "updates", {"tools": {"messages": [SimpleNamespace(name="consult_feature_integration_specialist")]}}
            yield "messages", (SimpleNamespace(content="before<th"), {"langgraph_node": "model"})
            yield "messages", (SimpleNamespace(content="ink>secret</think>after"), {"langgraph_node": "model"})
            yield "messages", (SimpleNamespace(content=[{"type": "reasoning", "text": "private"}, {"type": "text", "text": " answer"}]), {"langgraph_node": "model"})

        gateway = LangChainAgentGateway(Settings(), Mock(), Mock())
        gateway.supervisor = SimpleNamespace(astream=lambda *_args, **_kwargs: events())

        self.assertEqual(asyncio.run(self._collect(gateway.respond("question"))), "beforeafter answer")

    def test_streams_direct_supervisor_answer_without_specialist(self):
        async def events():
            yield "messages", (SimpleNamespace(content="<think>private</thi"), {"langgraph_node": "model"})
            yield "messages", (SimpleNamespace(content="nk>Olá!"), {"langgraph_node": "model"})
            yield "updates", {"model": {"messages": [SimpleNamespace()]}}

        gateway = LangChainAgentGateway(Settings(), Mock(), Mock())
        gateway.supervisor = SimpleNamespace(astream=lambda *_args, **_kwargs: events())

        self.assertEqual(asyncio.run(self._collect(gateway.respond("olá"))), "Olá!")

    def test_thought_filter_drops_unclosed_thinking(self):
        filter = ThoughtFilter()
        self.assertEqual(filter.filter("answer<think>secret"), "answer")
        self.assertEqual(filter.finish(), "")

    def test_extracted_search_tool_returns_dependency_errors(self):
        log = Mock()
        search = KnowledgeSearchUseCase(Mock(embed_query=AsyncMock(side_effect=EmbeddingUnavailable())), Mock())
        tool = create_search_system_knowledge(search, log)
        self.assertEqual(asyncio.run(tool.coroutine("query")), '{"error": "The embedding service is unavailable."}')

        search = KnowledgeSearchUseCase(
            Mock(embed_query=AsyncMock(return_value=[1.0])),
            Mock(search=Mock(side_effect=KnowledgeBaseUnavailable())),
        )
        tool = create_search_system_knowledge(search, log)
        self.assertEqual(asyncio.run(tool.coroutine("query")), '{"error": "The vector knowledge base is unavailable."}')

    def test_chat_maps_agent_failure_to_bad_gateway_without_secret_logs(self):
        with self.assertLogs(logger, level="INFO") as captured:
            response = TestClient(make_test_app(agent=FailingAgent())).post("/api/agents/chat", json={"message": "SECRET-PROMPT-MARKER"})

        self.assertEqual(response.status_code, 502)
        self.assertEqual(response.json()["detail"], "Agent invocation failed.")
        self.assertNotIn("SECRET-PROMPT-MARKER", "\n".join(captured.output))

    def test_chat_streams_plain_text_and_rejects_empty_response(self):
        response = TestClient(make_test_app()).post("/api/agents/chat", json={"message": "hello"})

        class EmptyAgent:
            async def respond(self, _):
                if False:
                    yield ""

        empty = TestClient(make_test_app(agent=EmptyAgent())).post("/api/agents/chat", json={"message": "hello"})
        self.assertEqual((response.status_code, response.text), (200, "answer"))
        self.assertEqual(response.headers["content-type"], "text/plain; charset=utf-8")
        self.assertEqual((empty.status_code, empty.json()["detail"]), (502, "Agent invocation failed."))

    @staticmethod
    async def _collect(stream):
        return "".join([chunk async for chunk in stream])

    def test_chat_cors_correlation_and_validation(self):
        client = TestClient(make_test_app())
        allowed = client.options("/api/agents/chat", headers={"Origin": "http://localhost:3000", "Access-Control-Request-Method": "POST"})
        denied = client.options("/api/agents/chat", headers={"Origin": "http://localhost:3001", "Access-Control-Request-Method": "POST"})
        valid = client.post("/api/agents/chat", headers={"X-Correlation-ID": "request-123"}, json={"message": "hello"})
        invalid = client.post("/api/agents/chat", headers={"X-Correlation-ID": "invalid id"}, json={"message": "SECRET-PROMPT-MARKER"})

        self.assertEqual((allowed.status_code, denied.status_code, valid.status_code), (200, 400, 200))
        self.assertEqual(allowed.headers["access-control-allow-origin"], "http://localhost:3000")
        self.assertEqual(valid.headers["X-Correlation-ID"], "request-123")
        self.assertRegex(invalid.headers["X-Correlation-ID"], r"^[0-9a-f]{32}$")
        self.assertEqual(client.post("/api/agents/chat", json={"message": " "}).status_code, 422)

    def test_health_maps_dependencies_to_503(self):
        class BadDatabase(HealthyRepository):
            def check(self):
                raise KnowledgeBaseUnavailable() from RuntimeError("database cause")

        class BadEmbedding(HealthyEmbeddings):
            async def ready(self):
                raise EmbeddingUnavailable() from RuntimeError("embedding cause")

        database_response = TestClient(make_test_app(repository=BadDatabase())).get("/health")
        embedding_response = TestClient(make_test_app(embeddings=BadEmbedding())).get("/health")

        self.assertEqual((database_response.status_code, database_response.json()["detail"]), (503, "PostgreSQL is unavailable."))
        self.assertEqual((embedding_response.status_code, embedding_response.json()["detail"]), (503, "Embedding service is unavailable."))

    def test_core_layers_do_not_import_frameworks(self):
        forbidden = {"fastapi", "grpc", "psycopg", "langchain", "langchain_openai"}
        root = Path(__file__).parent / "documentation_agent"
        for layer in ("domain", "application"):
            for source in (root / layer).glob("*.py"):
                tree = ast.parse(source.read_text())
                modules = [node.module.split(".")[0] for node in ast.walk(tree) if isinstance(node, ast.ImportFrom) and node.module]
                modules += [name.name.split(".")[0] for node in ast.walk(tree) if isinstance(node, ast.Import) for name in node.names]
                self.assertFalse(forbidden.intersection(modules), source)


if __name__ == "__main__":
    unittest.main()
