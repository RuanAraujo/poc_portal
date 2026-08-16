import asyncio
from collections.abc import AsyncIterator
from dataclasses import dataclass

from documentation_agent.domain import KnowledgeChunk

from .ports import AgentGateway, EmbeddingGateway, KnowledgeRepository


@dataclass
class KnowledgeSearchUseCase:
    embeddings: EmbeddingGateway
    repository: KnowledgeRepository

    async def execute(self, query: str) -> list[KnowledgeChunk]:
        embedding = await self.embeddings.embed_query(query)
        return await asyncio.to_thread(self.repository.search, embedding)


@dataclass
class HealthUseCase:
    embeddings: EmbeddingGateway
    repository: KnowledgeRepository

    async def execute(self) -> None:
        await asyncio.gather(asyncio.to_thread(self.repository.check), self.embeddings.ready())


@dataclass
class ChatUseCase:
    agent: AgentGateway

    def execute(self, message: str) -> AsyncIterator[str]:
        return self.agent.respond(message)
