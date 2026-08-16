import json
import logging
import time
from collections.abc import Callable

from langchain.tools import tool

from documentation_agent.application.errors import EmbeddingUnavailable, KnowledgeBaseUnavailable
from documentation_agent.application.services import KnowledgeSearchUseCase


def create_search_system_knowledge(search: KnowledgeSearchUseCase, log: Callable[..., object]):
    @tool
    async def search_system_knowledge(query: str) -> str:
        """Search indexed API documentation."""
        started = time.perf_counter()
        log("knowledge_search", "started")
        try:
            results = await search.execute(query)
            log(
                "knowledge_search",
                "completed",
                started=started,
                ResultCount=len(results),
                BestScore=max((result.score for result in results), default=0),
            )
            return json.dumps([result.as_dict() for result in results], ensure_ascii=False)
        except EmbeddingUnavailable as exception:
            log(
                "knowledge_search",
                "embedding_failed",
                started=started,
                level=logging.ERROR,
                ErrorType=type(exception.__cause__ or exception).__name__,
            )
            return json.dumps({"error": "The embedding service is unavailable."})
        except KnowledgeBaseUnavailable as exception:
            log(
                "knowledge_search",
                "database_failed",
                started=started,
                level=logging.ERROR,
                ErrorType=type(exception.__cause__ or exception).__name__,
            )
            return json.dumps({"error": "The vector knowledge base is unavailable."})

    return search_system_knowledge
