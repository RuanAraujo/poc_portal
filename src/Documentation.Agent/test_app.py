import asyncio
from types import SimpleNamespace
import unittest
from unittest.mock import AsyncMock, Mock, call, patch

import grpc
from langchain.agents.middleware.types import ModelRequest

import app
from fastapi.testclient import TestClient


class FailingSupervisor:
    async def ainvoke(self, _):
        raise RuntimeError("both NVIDIA models unavailable")


class SuccessfulSupervisor:
    async def ainvoke(self, _):
        return {"messages": [SimpleNamespace(text="answer", content="")]}


class AgentTests(unittest.TestCase):
    def setUp(self):
        self.previous_client = app.embedding_client
        self.previous_supervisor = app.supervisor_agent

    def tearDown(self):
        app.embedding_client = self.previous_client
        app.supervisor_agent = self.previous_supervisor

    def test_embedding_client_queries_grpc_with_deadline_and_requires_768_dimensions(self):
        stub = SimpleNamespace(EmbedQuery=AsyncMock(return_value=SimpleNamespace(embedding=[1.0] * 768)))
        client = app.EmbeddingClient.__new__(app.EmbeddingClient)
        client.stub = stub

        token = app.correlation_id.set("grpc-correlation")
        try:
            embedding = asyncio.run(client.embed_query("query"))
        finally:
            app.correlation_id.reset(token)

        self.assertEqual(len(embedding), 768)
        request = stub.EmbedQuery.await_args.args[0]
        self.assertEqual(request.text, "query")
        self.assertEqual(stub.EmbedQuery.await_args.kwargs["timeout"], 100)
        self.assertEqual(stub.EmbedQuery.await_args.kwargs["metadata"], (("x-correlation-id", "grpc-correlation"),))

    def test_embedding_client_rejects_wrong_dimension(self):
        client = app.EmbeddingClient.__new__(app.EmbeddingClient)
        client.stub = SimpleNamespace(EmbedQuery=AsyncMock(return_value=SimpleNamespace(embedding=[1.0])))

        with self.assertRaisesRegex(RuntimeError, "768 finite dimensions"):
            asyncio.run(client.embed_query("query"))

    def test_embedding_rpc_error_is_returned_by_the_search_tool(self):
        client = Mock()
        client.embed_query = AsyncMock(side_effect=grpc.RpcError())
        app.embedding_client = client

        result = asyncio.run(app.search_system_knowledge.coroutine("query"))

        self.assertEqual(result, '{"error": "The embedding service is unavailable."}')

    def test_vector_literal_is_pgvector_compatible(self):
        self.assertEqual(app._vector_literal([0.5, -0.25]), "[0.5,-0.25]")

    def test_message_text_hides_model_thinking(self):
        message = type("Message", (), {"text": "", "content": "<think>secret</think>answer"})()
        self.assertEqual(app._message_text(message), "answer")

    def test_native_fallback_uses_secondary_after_primary_fails(self):
        primary, secondary = object(), object()
        middleware = app.ModelFallbackMiddleware(secondary)
        attempts = []

        async def handler(request):
            attempts.append(request.model)
            if request.model is primary:
                raise RuntimeError("primary unavailable")
            return "secondary answer"

        result = asyncio.run(middleware.awrap_model_call(ModelRequest(model=primary, messages=[]), handler))

        self.assertEqual(result, "secondary answer")
        self.assertEqual(attempts, [primary, secondary])

    def test_chat_maps_exhausted_fallbacks_to_bad_gateway(self):
        app.supervisor_agent = FailingSupervisor()
        response = TestClient(app.app).post("/api/agents/chat", json={"message": "hello"})

        self.assertEqual(response.status_code, 502)
        self.assertEqual(response.json()["detail"], "Agent invocation failed.")

    def test_chat_propagates_valid_correlation_id(self):
        app.supervisor_agent = SuccessfulSupervisor()

        with self.assertLogs(app.logger, level="INFO") as captured:
            response = TestClient(app.app).post(
                "/api/agents/chat",
                headers={"X-Correlation-ID": "request-123"},
                json={"message": "hello"},
            )

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.headers["X-Correlation-ID"], "request-123")
        self.assertIn("CorrelationId=request-123 Step=agent_invocation Outcome=completed", "\n".join(captured.output))

    def test_invalid_correlation_id_is_replaced_and_sensitive_input_is_not_logged(self):
        secret = "SECRET-PROMPT-MARKER"

        class SensitiveFailure:
            async def ainvoke(self, _):
                raise RuntimeError(secret)

        app.supervisor_agent = SensitiveFailure()
        with self.assertLogs(app.logger, level="INFO") as captured:
            response = TestClient(app.app).post(
                "/api/agents/chat",
                headers={"X-Correlation-ID": "invalid id"},
                json={"message": secret},
            )

        self.assertRegex(response.headers["X-Correlation-ID"], r"^[0-9a-f]{32}$")
        logs = "\n".join(captured.output)
        self.assertNotIn(secret, logs)
        self.assertIn("Step=agent_invocation Outcome=failed", logs)

    @patch.object(app, "_search_database")
    def test_knowledge_search_logs_counts_without_query_or_content(self, search_database):
        secret = "SECRET-QUERY-MARKER"
        search_database.return_value = [{"score": 0.75, "content": "SECRET-CONTENT-MARKER"}]
        client = Mock()
        client.embed_query = AsyncMock(return_value=[1.0] * 768)
        app.embedding_client = client
        token = app.correlation_id.set("search-123")
        try:
            with self.assertLogs(app.logger, level="INFO") as captured:
                asyncio.run(app.search_system_knowledge.coroutine(secret))
        finally:
            app.correlation_id.reset(token)

        logs = "\n".join(captured.output)
        self.assertIn("CorrelationId=search-123 Step=knowledge_search Outcome=completed", logs)
        self.assertIn("ResultCount=1", logs)
        self.assertNotIn(secret, logs)
        self.assertNotIn("SECRET-CONTENT-MARKER", logs)

    @patch.object(app, "create_agent")
    @patch.object(app, "ModelFallbackMiddleware")
    @patch.object(app, "ChatOpenAI")
    def test_feature_and_supervisor_use_nvidia_primary_and_native_fallback(
        self, chat_openai, fallback_middleware, create_agent
    ):
        app.supervisor_agent = None
        primary, fallback = Mock(), Mock()
        chat_openai.side_effect = [primary, fallback]
        feature_agent, supervisor = Mock(), Mock()
        create_agent.side_effect = [feature_agent, supervisor]

        self.assertIs(app._get_supervisor(), supervisor)

        options = dict(api_key=app.LLM_API_KEY, base_url=app.LLM_BASE_URL, max_tokens=512, reasoning_effort="none")
        self.assertEqual(
            chat_openai.call_args_list,
            [
                call(model=app.AGENT_MODEL, **options),
                call(model=app.AGENT_FALLBACK_MODEL, **options),
            ],
        )
        self.assertEqual(fallback_middleware.call_args_list, [call(fallback), call(fallback)])
        self.assertEqual(create_agent.call_args_list[0].kwargs["middleware"], [fallback_middleware.return_value])
        self.assertEqual(create_agent.call_args_list[1].kwargs["middleware"], [fallback_middleware.return_value])


if __name__ == "__main__":
    unittest.main()
