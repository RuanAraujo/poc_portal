import logging
import time
from contextvars import ContextVar
from typing import Any

correlation_id: ContextVar[str] = ContextVar("correlation_id", default="-")
logger = logging.getLogger("documentation-agent")


def configure_logging() -> None:
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
    logger.setLevel(logging.INFO)


def log_event(step: str, outcome: str, *, started: float | None = None, level: int = logging.INFO, **fields: Any) -> None:
    values = [f"CorrelationId={correlation_id.get()}", f"Step={step}", f"Outcome={outcome}"]
    if started is not None:
        values.append(f"ElapsedMs={int((time.perf_counter() - started) * 1000)}")
    values.extend(f"{name}={value}" for name, value in fields.items())
    logger.log(level, " ".join(values))
