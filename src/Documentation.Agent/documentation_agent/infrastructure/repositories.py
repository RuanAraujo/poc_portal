import psycopg
from psycopg.rows import dict_row

from documentation_agent.application.errors import KnowledgeBaseUnavailable
from documentation_agent.domain import KnowledgeChunk


def vector_literal(embedding: list[float]) -> str:
    return "[" + ",".join(format(value, ".9g") for value in embedding) + "]"


class PostgresKnowledgeRepository:
    def __init__(self, dsn: str):
        self.dsn = dsn

    def check(self) -> None:
        try:
            with psycopg.connect(self.dsn, connect_timeout=5) as connection:
                connection.execute("SELECT 1")
        except psycopg.Error as exception:
            raise KnowledgeBaseUnavailable() from exception

    def search(self, embedding: list[float]) -> list[KnowledgeChunk]:
        sql = '''
            SELECT
                documentation."ApiId" AS api_id,
                documentation."Name" AS api_name,
                version."Version" AS version,
                version."Environment" AS environment,
                chunk.chunk_type,
                chunk.metadata,
                chunk.content,
                chunk.document_id::text AS document_id,
                chunk.version_id::text AS version_id,
                1 - (chunk.embedding <=> %s::vector) AS score
            FROM ingestion.document_chunks AS chunk
            INNER JOIN documentation.api_documentations AS documentation
                ON documentation."Id" = chunk.document_id
            INNER JOIN documentation.documentation_versions AS version
                ON version."Id" = chunk.version_id
            WHERE version."Status" = 'Available'
            ORDER BY chunk.embedding <=> %s::vector
            LIMIT 3;
        '''
        try:
            vector = vector_literal(embedding)
            with psycopg.connect(self.dsn, row_factory=dict_row, connect_timeout=5) as connection:
                rows = connection.execute(sql, (vector, vector)).fetchall()
            return [
                KnowledgeChunk(**{**row, "score": float(row["score"]), "content": row["content"][:4_000]})
                for row in rows
            ]
        except psycopg.Error as exception:
            raise KnowledgeBaseUnavailable() from exception
