from collections.abc import AsyncIterator
from typing import Protocol

from documentation_agent.domain import KnowledgeChunk


class EmbeddingGateway(Protocol):
    async def start(self) -> None: ...

    async def embed_query(self, text: str) -> list[float]: ...

    async def ready(self) -> None: ...

    async def close(self) -> None: ...


class KnowledgeRepository(Protocol):
    def search(self, embedding: list[float]) -> list[KnowledgeChunk]: ...

    def check(self) -> None: ...


class AgentGateway(Protocol):
    def respond(self, message: str) -> AsyncIterator[str]: ...
