import asyncio
import math
import time
from collections.abc import Callable

import grpc

from documentation import embeddings_pb2, embeddings_pb2_grpc
from documentation_agent.application.errors import EmbeddingUnavailable

EMBEDDING_DIMENSIONS = 768
EMBEDDING_GRPC_DEADLINE_SECONDS = 100


class GrpcEmbeddingGateway:
    def __init__(self, address: str, log: Callable[..., object], get_correlation_id: Callable[[], str], channel=None, stub=None):
        self.address = address
        self.log = log
        self.get_correlation_id = get_correlation_id
        self.channel = channel
        self.stub = stub

    async def start(self) -> None:
        if self.stub is not None:
            return
        self.channel = self.channel or grpc.aio.insecure_channel(self.address)
        self.stub = embeddings_pb2_grpc.EmbeddingServiceStub(self.channel)

    async def embed_query(self, text: str) -> list[float]:
        await self.start()
        started = time.perf_counter()
        self.log("embedding_query", "started")
        try:
            response = await self.stub.EmbedQuery(
                embeddings_pb2.EmbedRequest(text=text),
                timeout=EMBEDDING_GRPC_DEADLINE_SECONDS,
                metadata=(("x-correlation-id", self.get_correlation_id()),),
            )
            embedding = [float(value) for value in response.embedding]
            if len(embedding) != EMBEDDING_DIMENSIONS or not all(math.isfinite(value) for value in embedding):
                raise ValueError
            self.log("embedding_query", "completed", started=started, Dimensions=len(embedding))
            return embedding
        except (grpc.RpcError, ValueError) as exception:
            raise EmbeddingUnavailable("Embedding service must return 768 finite dimensions.") from exception

    async def ready(self) -> None:
        await self.start()
        try:
            await asyncio.wait_for(self.channel.channel_ready(), timeout=5)
        except (asyncio.TimeoutError, grpc.RpcError) as exception:
            raise EmbeddingUnavailable() from exception

    async def close(self) -> None:
        channel = self.channel
        self.channel = None
        self.stub = None
        if channel is not None:
            await channel.close()
