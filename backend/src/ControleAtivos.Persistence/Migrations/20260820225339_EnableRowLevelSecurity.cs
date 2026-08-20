using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleAtivos.Persistence.Migrations;

/// <summary>
/// Isolamento por tenant no proprio banco (Row Level Security).
///
/// O EF ja filtra por tenant em toda consulta, e essa continua sendo a
/// primeira linha de defesa. Esta e a segunda, e existe porque a primeira tem
/// um ponto cego conhecido: SQL cru, uma migration mal revisada ou um
/// <c>IgnoreQueryFilters</c> esquecido passam direto pelo filtro do EF. O banco
/// nao tem esse ponto cego — ele aplica a politica independentemente de como a
/// consulta chegou.
///
/// Duas decisoes merecem registro:
///
/// <c>FORCE ROW LEVEL SECURITY</c> e obrigatorio porque a aplicacao e dona das
/// tabelas, e o dono ignora RLS por padrao. Sem o FORCE, as politicas ficariam
/// decorativas.
///
/// <c>current_setting(..., true)</c> devolve NULL quando a variavel nao foi
/// definida, e a comparacao com NULL nao e verdadeira — de modo que a ausencia
/// de contexto de tenant nega acesso em vez de liberar tudo. Uma conexao que
/// esqueca de informar o tenant enxerga zero linhas, nao todas.
///
/// IMPORTANTE: RLS nao se aplica a superusuarios. O usuario da aplicacao
/// precisa ser um role comum; ver docs/seguranca-multi-tenant.md.
/// </summary>
public partial class EnableRowLevelSecurity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.alerts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.alerts FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.alerts;

                CREATE POLICY tenant_isolation ON controle_ativos.alerts
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.assets ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.assets FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.assets;

                CREATE POLICY tenant_isolation ON controle_ativos.assets
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.audit_logs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.audit_logs FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.audit_logs;

                CREATE POLICY tenant_isolation ON controle_ativos.audit_logs
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.contracts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.contracts FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.contracts;

                CREATE POLICY tenant_isolation ON controle_ativos.contracts
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.cost_centers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.cost_centers FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.cost_centers;

                CREATE POLICY tenant_isolation ON controle_ativos.cost_centers
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.cost_entries ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.cost_entries FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.cost_entries;

                CREATE POLICY tenant_isolation ON controle_ativos.cost_entries
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.documents ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.documents FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.documents;

                CREATE POLICY tenant_isolation ON controle_ativos.documents
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.local_credentials ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.local_credentials FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.local_credentials;

                CREATE POLICY tenant_isolation ON controle_ativos.local_credentials
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.management_assignments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.management_assignments FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.management_assignments;

                CREATE POLICY tenant_isolation ON controle_ativos.management_assignments
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.organizational_units ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.organizational_units FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.organizational_units;

                CREATE POLICY tenant_isolation ON controle_ativos.organizational_units
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.price_adjustments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.price_adjustments FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.price_adjustments;

                CREATE POLICY tenant_isolation ON controle_ativos.price_adjustments
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.suppliers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.suppliers FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.suppliers;

                CREATE POLICY tenant_isolation ON controle_ativos.suppliers
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.timeline_events ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.timeline_events FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.timeline_events;

                CREATE POLICY tenant_isolation ON controle_ativos.timeline_events
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.user_role_assignments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.user_role_assignments FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.user_role_assignments;

                CREATE POLICY tenant_isolation ON controle_ativos.user_role_assignments
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE controle_ativos.users ENABLE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.users FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.users;

                CREATE POLICY tenant_isolation ON controle_ativos.users
                    USING (tenant_id::text = current_setting('app.current_tenant', true))
                    WITH CHECK (tenant_id::text = current_setting('app.current_tenant', true));
                """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.alerts;
                ALTER TABLE controle_ativos.alerts NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.alerts DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.assets;
                ALTER TABLE controle_ativos.assets NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.assets DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.audit_logs;
                ALTER TABLE controle_ativos.audit_logs NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.audit_logs DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.contracts;
                ALTER TABLE controle_ativos.contracts NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.contracts DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.cost_centers;
                ALTER TABLE controle_ativos.cost_centers NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.cost_centers DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.cost_entries;
                ALTER TABLE controle_ativos.cost_entries NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.cost_entries DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.documents;
                ALTER TABLE controle_ativos.documents NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.documents DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.local_credentials;
                ALTER TABLE controle_ativos.local_credentials NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.local_credentials DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.management_assignments;
                ALTER TABLE controle_ativos.management_assignments NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.management_assignments DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.organizational_units;
                ALTER TABLE controle_ativos.organizational_units NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.organizational_units DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.price_adjustments;
                ALTER TABLE controle_ativos.price_adjustments NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.price_adjustments DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.suppliers;
                ALTER TABLE controle_ativos.suppliers NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.suppliers DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.timeline_events;
                ALTER TABLE controle_ativos.timeline_events NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.timeline_events DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.user_role_assignments;
                ALTER TABLE controle_ativos.user_role_assignments NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.user_role_assignments DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS tenant_isolation ON controle_ativos.users;
                ALTER TABLE controle_ativos.users NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE controle_ativos.users DISABLE ROW LEVEL SECURITY;
                """);
    }
}
