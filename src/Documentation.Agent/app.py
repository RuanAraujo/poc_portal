from contextlib import asynccontextmanager
from fastapi import FastAPI

from documentation_agent.application.services import ChatUseCase, HealthUseCase, KnowledgeSearchUseCase
from documentation_agent.infrastructure.agents import LangChainAgentGateway
from documentation_agent.infrastructure.config import Settings
from documentation_agent.infrastructure.embeddings import GrpcEmbeddingGateway
from documentation_agent.infrastructure.repositories import PostgresKnowledgeRepository
from documentation_agent.interface_adapters.http import create_router
from documentation_agent.interface_adapters.observability import correlation_id, configure_logging, log_event
from documentation_agent.interface_adapters.web import add_web_concerns


def create_app(settings: Settings | None = None) -> FastAPI:
    settings = settings or Settings.from_environment()
    configure_logging()
    embeddings = GrpcEmbeddingGateway(settings.embedding_grpc_address, log_event, correlation_id.get)
    repository = PostgresKnowledgeRepository(settings.database_dsn)
    search = KnowledgeSearchUseCase(embeddings, repository)
    chat = ChatUseCase(LangChainAgentGateway(settings, search, log_event))
    health = HealthUseCase(embeddings, repository)

    @asynccontextmanager
    async def lifespan(_: FastAPI):
        await embeddings.start()
        log_event("service_lifecycle", "started")
        yield
        await embeddings.close()
        log_event("service_lifecycle", "stopped")

    application = FastAPI(title="Documentation Agent", version="1.0.0", lifespan=lifespan)
    add_web_concerns(application, settings.portal_origin)
    application.include_router(create_router(chat, health))
    return application


app = create_app()
