namespace Infrastructure.Persistence.V1;

public enum V1DatabaseProvider
{
    PostgreSql,
    MySql
}

public static class V1DatabaseProviderParser
{
    public static V1DatabaseProvider Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return V1DatabaseProvider.PostgreSql;
        }

        if (value.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return V1DatabaseProvider.PostgreSql;
        }

        if (value.Equals("MySql", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
        {
            return V1DatabaseProvider.MySql;
        }

        throw new InvalidOperationException($"Unsupported v1 database provider '{value}'. Expected PostgreSql or MySql.");
    }
}
