-- Extensoes e configuracoes base do banco do Controle de Ativos.
-- Executado apenas na primeira inicializacao do volume de dados.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "citext";
CREATE EXTENSION IF NOT EXISTS "unaccent";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Datas sempre em UTC no armazenamento; a aplicacao converte para exibicao.
DO $$
BEGIN
    EXECUTE format('ALTER DATABASE %I SET timezone TO %L', current_database(), 'UTC');
END
$$;
