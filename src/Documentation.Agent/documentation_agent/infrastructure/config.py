import os
from dataclasses import dataclass


@dataclass(frozen=True)
class Settings:
    embedding_grpc_address: str = "localhost:8080"
    database_dsn: str = "host=localhost port=5432 dbname=documentation_portal user=postgres password=postgres"
    agent_model: str = "nvidia/nemotron-3-ultra-550b-a55b"
    agent_fallback_model: str = "nvidia/nemotron-3-super-120b-a12b"
    llm_max_tokens: int = 512
    llm_base_url: str = "https://integrate.api.nvidia.com/v1"
    llm_api_key: str = ""
    portal_origin: str = "http://localhost:3000"

    @classmethod
    def from_environment(cls) -> "Settings":
        return cls(
            embedding_grpc_address=os.getenv("EMBEDDING_GRPC_ADDRESS", cls.embedding_grpc_address),
            database_dsn=os.getenv("DATABASE_DSN", cls.database_dsn),
            agent_model=os.getenv("AGENT_MODEL", cls.agent_model),
            agent_fallback_model=os.getenv("AGENT_FALLBACK_MODEL", cls.agent_fallback_model),
            llm_max_tokens=int(os.getenv("LLM_MAX_TOKENS", str(cls.llm_max_tokens))),
            llm_base_url=os.getenv("LLM_BASE_URL", cls.llm_base_url),
            llm_api_key=os.getenv("LLM_API_KEY", ""),
            portal_origin=os.getenv("PORTAL_ORIGIN", cls.portal_origin),
        )
