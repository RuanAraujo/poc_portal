import math
import unittest
from unittest.mock import call, patch

import app
from fastapi.testclient import TestClient


class StubVector(list):
    def tolist(self):
        return list(self)


class StubEmbeddingModel:
    def __init__(self):
        self.calls = []

    def encode_document(self, text, normalize_embeddings=True):
        self.calls.append(("document", text, normalize_embeddings))
        return StubVector([1.0] * app.EMBEDDING_DIMENSIONS)

    def encode_query(self, text, normalize_embeddings=True):
        self.calls.append(("query", text, normalize_embeddings))
        return StubVector([2.0] * app.EMBEDDING_DIMENSIONS)


class FailingSupervisor:
    async def ainvoke(self, _):
        raise RuntimeError("LLM unavailable")


class EmbeddingTests(unittest.TestCase):
    def setUp(self):
        self.previous_model = app.embedding_model
        self.previous_supervisor = app.supervisor_agent
        self.model = StubEmbeddingModel()
        app.embedding_model = self.model

    def tearDown(self):
        app.embedding_model = self.previous_model
        app.supervisor_agent = self.previous_supervisor

    def test_document_and_query_embeddings_are_normalized_and_distinct(self):
        document = app._encode_document("document")
        query = app._encode_query("query")

        self.assertEqual([call[0] for call in self.model.calls], ["document", "query"])
        self.assertEqual(len(document), 768)
        self.assertEqual(len(query), 768)
        self.assertAlmostEqual(math.sqrt(sum(value * value for value in document)), 1.0)
        self.assertAlmostEqual(math.sqrt(sum(value * value for value in query)), 1.0)

    def test_vector_literal_is_pgvector_compatible(self):
        literal = app._vector_literal([0.5, -0.25])
        self.assertEqual(literal, "[0.5,-0.25]")

    def test_message_text_hides_model_thinking(self):
        message = type("Message", (), {"text": "", "content": "<think>secret</think>answer"})()
        self.assertEqual(app._message_text(message), "answer")

    def test_internal_embedding_endpoint_returns_768_dimensions(self):
        response = TestClient(app.app).post("/internal/embeddings", json={"text": "document"})

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["dimensions"], 768)
        self.assertEqual(len(response.json()["embedding"]), 768)

    def test_chat_maps_llm_failures_to_bad_gateway(self):
        app.supervisor_agent = FailingSupervisor()
        response = TestClient(app.app).post("/api/agents/chat", json={"message": "hello"})

        self.assertEqual(response.status_code, 502)
        self.assertEqual(response.json()["detail"], "Agent invocation failed.")

    @patch.object(app, "create_agent")
    @patch.object(app, "ChatOpenAI")
    def test_supervisor_uses_configured_openai_compatible_llm(self, chat_openai, create_agent):
        app.supervisor_agent = None
        expected_agent = create_agent.return_value

        supervisor = app._get_supervisor()

        chat_openai.assert_called_once_with(
            model=app.AGENT_MODEL,
            api_key=app.LLM_API_KEY,
            base_url=app.LLM_BASE_URL,
            max_tokens=app.LLM_MAX_TOKENS,
            reasoning_effort="none",
        )
        self.assertIs(supervisor, expected_agent)


if __name__ == "__main__":
    unittest.main()
