using System.Data;
using System.Data.Common;
using EFactura.Application.Fiscal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence.V1.Write.Repositories;

/// <summary>
/// Append-only persistence for DGI Reporte Diario later-state observations.
/// This evidence table is migration-owned and intentionally kept outside the aggregate mappings so
/// observing DR/ER/FR cannot accidentally create an EF state transition on the root/revision rows.
/// </summary>
public sealed class EfFiscalDailyReportLaterStateObservationRepository :
    IFiscalDailyReportLaterStateObservationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfFiscalDailyReportLaterStateObservationRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoredFiscalDailyReportLaterStateObservation?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var q = IdentifierQuote();
        var table = Q(q, "v1_fdr_later_state_observations");
        var sql = $"""
            SELECT
                {Q(q, "Id")}, {Q(q, "RootSubmissionId")}, {Q(q, "BrCorrectionRevisionId")},
                {Q(q, "OrganizationId")}, {Q(q, "IssuerRuc")}, {Q(q, "SummaryDate")},
                {Q(q, "Sequence")}, {Q(q, "LocalRevision")}, {Q(q, "OperationId")},
                {Q(q, "DgiEmitterId")}, {Q(q, "DgiReceiverId")}, {Q(q, "State")},
                {Q(q, "DgiStateCode")}, {Q(q, "DgiReceptionTimestampText")},
                {Q(q, "EvidenceXml")}, {Q(q, "EvidenceXmlHash")}, {Q(q, "ObservedAtUtc")}
            FROM {table}
            WHERE {Q(q, "OrganizationId")} = @organizationId
              AND {Q(q, "OperationId")} = @operationId
            """;

        return await ExecuteReaderSingleAsync(
            sql,
            command =>
            {
                Add(command, "@organizationId", organizationId);
                Add(command, "@operationId", operationId);
            },
            cancellationToken);
    }

    public async Task AddAsync(
        StoredFiscalDailyReportLaterStateObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (observation.RootSubmissionId.HasValue == observation.BrCorrectionRevisionId.HasValue)
            throw new InvalidOperationException("Later-state observation must reference exactly one Reporte Diario target.");

        var q = IdentifierQuote();
        var table = Q(q, "v1_fdr_later_state_observations");
        var sql = $"""
            INSERT INTO {table} (
                {Q(q, "Id")}, {Q(q, "RootSubmissionId")}, {Q(q, "BrCorrectionRevisionId")},
                {Q(q, "OrganizationId")}, {Q(q, "IssuerRuc")}, {Q(q, "SummaryDate")},
                {Q(q, "Sequence")}, {Q(q, "LocalRevision")}, {Q(q, "OperationId")},
                {Q(q, "DgiEmitterId")}, {Q(q, "DgiReceiverId")}, {Q(q, "State")},
                {Q(q, "DgiStateCode")}, {Q(q, "DgiReceptionTimestampText")},
                {Q(q, "EvidenceXml")}, {Q(q, "EvidenceXmlHash")}, {Q(q, "ObservedAtUtc")})
            VALUES (
                @id, @rootSubmissionId, @brCorrectionRevisionId,
                @organizationId, @issuerRuc, @summaryDate,
                @sequence, @localRevision, @operationId,
                @dgiEmitterId, @dgiReceiverId, @state,
                @dgiStateCode, @dgiReceptionTimestampText,
                @evidenceXml, @evidenceXmlHash, @observedAtUtc)
            """;

        await ExecuteNonQueryAsync(
            sql,
            command =>
            {
                Add(command, "@id", observation.Id);
                Add(command, "@rootSubmissionId", observation.RootSubmissionId);
                Add(command, "@brCorrectionRevisionId", observation.BrCorrectionRevisionId);
                Add(command, "@organizationId", observation.OrganizationId);
                Add(command, "@issuerRuc", observation.IssuerRuc);
                Add(command, "@summaryDate", observation.SummaryDate.ToDateTime(TimeOnly.MinValue));
                Add(command, "@sequence", observation.Sequence);
                Add(command, "@localRevision", observation.LocalRevision);
                Add(command, "@operationId", observation.OperationId);
                Add(command, "@dgiEmitterId", observation.DgiEmitterId);
                Add(command, "@dgiReceiverId", observation.DgiReceiverId);
                Add(command, "@state", (int)observation.State);
                Add(command, "@dgiStateCode", observation.DgiStateCode);
                Add(command, "@dgiReceptionTimestampText", observation.DgiReceptionTimestampText);
                Add(command, "@evidenceXml", observation.EvidenceXml);
                Add(command, "@evidenceXmlHash", observation.EvidenceXmlHash);
                Add(command, "@observedAtUtc", observation.ObservedAtUtc);
            },
            cancellationToken);
    }

    private async Task<StoredFiscalDailyReportLaterStateObservation?> ExecuteReaderSingleAsync(
        string sql,
        Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AttachTransaction(command);
            configure(command);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var value = Map(reader);
            if (await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("Later-state observation operation id is not unique in persistence.");
            return value;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task ExecuteNonQueryAsync(
        string sql,
        Action<DbCommand> configure,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AttachTransaction(command);
            configure(command);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private void AttachTransaction(DbCommand command)
    {
        var transaction = _dbContext.Database.CurrentTransaction;
        if (transaction is not null)
            command.Transaction = transaction.GetDbTransaction();
    }

    private string IdentifierQuote()
    {
        var provider = _dbContext.Database.ProviderName ?? string.Empty;
        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            return "\"";
        if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            return "`";
        throw new NotSupportedException($"Unsupported persistence provider '{provider}'.");
    }

    private static string Q(string quote, string identifier) => quote + identifier + quote;

    private static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static StoredFiscalDailyReportLaterStateObservation Map(DbDataReader reader) =>
        new(
            ReadGuid(reader.GetValue(0)),
            reader.IsDBNull(1) ? null : ReadGuid(reader.GetValue(1)),
            reader.IsDBNull(2) ? null : ReadGuid(reader.GetValue(2)),
            reader.GetString(3),
            reader.GetString(4),
            DateOnly.FromDateTime(Convert.ToDateTime(reader.GetValue(5), System.Globalization.CultureInfo.InvariantCulture)),
            Convert.ToInt32(reader.GetValue(6), System.Globalization.CultureInfo.InvariantCulture),
            reader.IsDBNull(7) ? null : Convert.ToInt32(reader.GetValue(7), System.Globalization.CultureInfo.InvariantCulture),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            (FiscalDailyReportLaterState)Convert.ToInt32(reader.GetValue(11), System.Globalization.CultureInfo.InvariantCulture),
            reader.GetString(12),
            reader.GetString(13),
            reader.GetString(14),
            reader.GetString(15),
            ReadOffset(reader.GetValue(16)));

    private static Guid ReadGuid(object value) => value switch
    {
        Guid guid => guid,
        byte[] bytes when bytes.Length == 16 => new Guid(bytes),
        _ => Guid.Parse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("Persisted GUID is null."))
    };

    private static DateTimeOffset ReadOffset(object value) => value switch
    {
        DateTimeOffset offset => offset.ToUniversalTime(),
        DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
        _ => DateTimeOffset.Parse(
            Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException("Persisted timestamp is null."),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime()
    };
}
