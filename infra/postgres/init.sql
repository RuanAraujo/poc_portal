-- Inicializacao executada apenas na criacao de um novo volume PostgreSQL.
-- Tabelas e migrations pertencem aos servicos .NET.
CREATE EXTENSION IF NOT EXISTS vector;

CREATE SCHEMA IF NOT EXISTS documentation;
CREATE SCHEMA IF NOT EXISTS ingestion;
