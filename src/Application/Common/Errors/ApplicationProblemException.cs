namespace EFactura.Application.Common.Errors;

public enum ApplicationProblemKind
{
    BadRequest,
    AuthenticationRequired,
    Forbidden,
    NotFound,
    Conflict,
    Validation,
    BusinessRule,
    RequestTooLarge,
    UnsupportedMediaType,
    RateLimited,
    DependencyUnavailable
}

public sealed record ApplicationFieldError(string Path, string Code, string Message);

public sealed record ApplicationRuleReference(string RuleId, string? Version = null, string? Source = null);

public sealed class ApplicationProblemException : Exception
{
    public ApplicationProblemException(
        ApplicationProblemKind kind,
        string code,
        string safeDetail,
        IReadOnlyList<ApplicationFieldError>? errors = null,
        string? conflictType = null,
        string? currentVersion = null,
        IReadOnlyList<ApplicationRuleReference>? ruleReferences = null,
        int? retryAfterSeconds = null)
        : base(safeDetail)
    {
        Kind = kind;
        Code = code;
        Errors = errors ?? Array.Empty<ApplicationFieldError>();
        ConflictType = conflictType;
        CurrentVersion = currentVersion;
        RuleReferences = ruleReferences ?? Array.Empty<ApplicationRuleReference>();
        RetryAfterSeconds = retryAfterSeconds;
    }

    public ApplicationProblemKind Kind { get; }
    public string Code { get; }
    public IReadOnlyList<ApplicationFieldError> Errors { get; }
    public string? ConflictType { get; }
    public string? CurrentVersion { get; }
    public IReadOnlyList<ApplicationRuleReference> RuleReferences { get; }
    public int? RetryAfterSeconds { get; }
}
