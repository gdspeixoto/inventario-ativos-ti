# Agent Backend

## Missao

Implementar o backend .NET do Controle de Ativos com DDD, PostgreSQL, multitenancy, seguranca forte e auditoria.

## Ordem De Trabalho Recomendada

1. Criar solution e projetos .NET.
2. Instalar pacotes: ASP.NET Core, EF Core, Npgsql, FluentValidation, Serilog, OpenTelemetry, Swagger/OpenAPI.
3. Modelar entidades do Domain.
4. Criar DbContext e mappings EF Core.
5. Implementar tenant context e filtros globais.
6. Implementar autenticacao/autorizacao.
7. Implementar use cases de licencas, servidores, contratos e reajustes.
8. Garantir timeline automatica e audit log.
9. Criar migrations.
10. Criar testes unitarios e integrados.

## Nao Negociavel

- Nenhuma query sem filtro de tenant.
- Nenhum endpoint mutavel sem autorizacao.
- Nenhum reajuste sem timeline.
- Nenhum DTO deve confiar em valores calculados enviados pelo cliente.
- Nenhum token ou secret em log.
