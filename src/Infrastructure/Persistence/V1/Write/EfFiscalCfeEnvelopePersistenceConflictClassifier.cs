using EFactura.Application.Fiscal;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Infrastructure.Persistence.V1.Write;

public sealed class EfFiscalCfeEnvelopePersistenceConflictClassifier
    : IFiscalCfeEnvelopePersistenceConflictClassifier
{
    public bool IsUniqueConstraintConflict(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return true;
            }

            if (current is MySqlException mysql && mysql.Number == 1062)
            {
                return true;
            }
        }

        return false;
    }
}
