import logging
import re
import time
import uuid
from typing import Any
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from .observability import correlation_id, log_event
CORRELATION_ID_PATTERN = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$")
def add_web_concerns(app: FastAPI, portal_origin: str) -> None:
    app.add_middleware(CORSMiddleware, allow_origins=[portal_origin], allow_methods=["POST"], allow_headers=["*"])
    @app.middleware("http")
    async def correlate_request(request: Request, call_next: Any):
        values = request.headers.getlist("X-Correlation-ID")
        request_id = values[0] if len(values) == 1 and CORRELATION_ID_PATTERN.fullmatch(values[0]) else uuid.uuid4().hex
        token = correlation_id.set(request_id); started = time.perf_counter()
        try:
            response = await call_next(request); response.headers["X-Correlation-ID"] = request_id
            if request.url.path != "/health" or response.status_code >= 400: log_event("http_request", "completed" if response.status_code < 400 else "failed", started=started, level=logging.INFO if response.status_code < 400 else logging.WARNING, Method=request.method, Path=request.url.path, StatusCode=response.status_code)
            return response
        except Exception as exception:
            log_event("http_request", "failed", started=started, level=logging.ERROR, Method=request.method, Path=request.url.path, ErrorType=type(exception).__name__); raise
        finally: correlation_id.reset(token)
