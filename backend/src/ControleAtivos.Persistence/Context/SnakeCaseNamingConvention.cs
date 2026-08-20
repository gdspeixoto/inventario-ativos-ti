using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ControleAtivos.Persistence.Context;

/// <summary>
/// Converte nomes de tabelas, colunas, chaves e indices para snake_case.
///
/// Sem isso, o EF Core geraria identificadores em PascalCase, que no PostgreSQL
/// exigem aspas duplas em toda consulta manual — atrito desnecessario para quem
/// precisa investigar dados direto no banco.
/// </summary>
public static class SnakeCaseNamingConvention
{
    public static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entity.GetKeys())
            {
                var name = key.GetName();
                if (name is not null)
                {
                    key.SetName(ToSnakeCase(name));
                }
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var name = foreignKey.GetConstraintName();
                if (name is not null)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(name));
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                var name = index.GetDatabaseName();
                if (name is not null)
                {
                    index.SetDatabaseName(ToSnakeCase(name));
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var builder = new StringBuilder(input.Length + 8);

        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];

            if (char.IsUpper(current))
            {
                var previousIsLower = i > 0 && (char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1]));
                var nextIsLower = i + 1 < input.Length && char.IsLower(input[i + 1]);
                var previousIsUpper = i > 0 && char.IsUpper(input[i - 1]);

                if (i > 0 && input[i - 1] != '_' && (previousIsLower || (previousIsUpper && nextIsLower)))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
