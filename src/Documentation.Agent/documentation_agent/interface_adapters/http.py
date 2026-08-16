import logging
import time
from collections.abc import AsyncIterator
from typing import Annotated

from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, StringConstraints

from documentation_agent.application.errors import AgentInvocationFailed, EmbeddingUnavailable, KnowledgeBaseUnavailable
from documentation_agent.application.services import ChatUseCase, HealthUseCase

from .observability import log_event

ChatMessage = Annotated[str, StringConstraints(strip_whitespace=True, min_length=1, max_length=10_000)]


def error_type(exception: Exception) -> str:
    return type(exception.__cause__ or exception).__name__


class ChatRequest(BaseModel):
    message: ChatMessage


def create_router(chat: ChatUseCase, health: HealthUseCase) -> APIRouter:
    router = APIRouter()

    @router.get("/health")
    async def get_health() -> dict[str, str]:
        try:
            await health.execute()
        except KnowledgeBaseUnavailable as exception:
            log_event("health_check", "failed", level=logging.WARNING, Dependency="postgres", ErrorType=error_type(exception))
            raise HTTPException(503, "PostgreSQL is unavailable.") from exception
        except EmbeddingUnavailable as exception:
            log_event("health_check", "failed", level=logging.WARNING, Dependency="embeddings", ErrorType=error_type(exception))
            raise HTTPException(503, "Embedding service is unavailable.") from exception
        return {"status": "healthy"}

    @router.post("/api/agents/chat")
    async def post_chat(request: ChatRequest) -> StreamingResponse:
        started = time.perf_counter()
        log_event("agent_invocation", "started")
        try:
            stream = chat.execute(request.message)
            first = await anext(stream, None)
            if first is None:
                raise AgentInvocationFailed()
        except Exception as exception:
            log_event("agent_invocation", "failed", started=started, level=logging.ERROR, ErrorType=error_type(exception))
            raise HTTPException(502, "Agent invocation failed.") from exception

        async def response() -> AsyncIterator[str]:
            length = len(first or "")
            try:
                if first:
                    yield first
                async for chunk in stream:
                    length += len(chunk)
                    yield chunk
                log_event("agent_invocation", "completed", started=started, AnswerLength=length)
            except Exception as exception:
                log_event("agent_invocation", "failed", started=started, level=logging.ERROR, ErrorType=error_type(exception))
                raise

        return StreamingResponse(response(), media_type="text/plain; charset=utf-8")

    return router
