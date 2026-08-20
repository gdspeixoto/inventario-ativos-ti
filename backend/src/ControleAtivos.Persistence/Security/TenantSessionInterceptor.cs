using System.Data.Common;
using ControleAtivos.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace ControleAtivos.Persistence.Security;

/// <summary>
/// Informa ao PostgreSQL qual e o tenant da conexao, para que as politicas de
/// RLS possam agir.
///
/// O isolamento ja existe no EF, por filtro global de consulta. Esta e a
/// segunda camada, e ela existe porque a primeira tem um ponto cego: uma
/// consulta escrita em SQL cru, uma migration mal revisada ou um
/// <c>IgnoreQueryFilters</c> esquecido passariam direto pelo filtro do EF e
/// enxergariam dados de todos os tenants. O banco nao tem esse ponto cego —
/// ele aplica a politica independentemente de como a consulta chegou.
///
/// O valor vai por <c>set_config</c> com parametro, nunca por interpolacao:
/// concatenar o identificador na string transformaria o mecanismo de
/// isolamento em vetor de injecao.
/// </summary>
public sealed class TenantSessionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public const string TenantSettingName = "app.current_tenant";

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ApplyAsync(connection, cancellationToken);

        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyAsync(connection, CancellationToken.None).GetAwaiter().GetResult();

        base.ConnectionOpened(connection, eventData);
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not NpgsqlConnection npgsql)
        {
            return;
        }

        /*
         * Sem tenant resolvido — health check, login, migrations — a variavel
         * fica vazia. As politicas tratam vazio como "nenhuma linha visivel",
         * de modo que a falta de contexto nega acesso em vez de liberar tudo.
         */
        var tenantId = tenantContext.HasTenant
            ? tenantContext.TenantId.ToString()
            : string.Empty;

        await using var command = npgsql.CreateCommand();
        command.CommandText = "SELECT set_config(@name, @value, false)";
        command.Parameters.AddWithValue("name", TenantSettingName);
        command.Parameters.AddWithValue("value", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
