from dataclasses import asdict, dataclass
from typing import Any


@dataclass(frozen=True)
class KnowledgeChunk:
    api_id: str
    api_name: str
    version: str
    environment: str
    chunk_type: str
    metadata: Any
    content: str
    document_id: str
    version_id: str
    score: float

    def as_dict(self) -> dict[str, Any]:
        return asdict(self)
