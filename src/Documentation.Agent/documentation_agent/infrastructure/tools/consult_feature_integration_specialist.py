import logging
import time
from collections.abc import Callable
from typing import Any

from langchain.tools import tool

from documentation_agent.infrastructure.text import message_text


def create_consult_feature_integration_specialist(feature_agent: Any, log: Callable[..., object]):
    @tool
    async def consult_feature_integration_specialist(request: str) -> str:
        """Delegate a Documentation Portal feature-integration question to the specialist."""
        started = time.perf_counter()
        log("specialist_delegation", "started")
        try:
            result = await feature_agent.ainvoke({"messages": [{"role": "user", "content": request}]})
            answer = message_text(result["messages"][-1])
            log("specialist_delegation", "completed", started=started, AnswerLength=len(answer))
            return answer
        except Exception as exception:
            log(
                "specialist_delegation",
                "failed",
                started=started,
                level=logging.ERROR,
                ErrorType=type(exception).__name__,
            )
            raise

    return consult_feature_integration_specialist
