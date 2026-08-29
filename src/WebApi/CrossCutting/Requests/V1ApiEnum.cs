using EFactura.Application.Common.Errors;

namespace WebApi.CrossCutting.Requests;

public static class V1ApiEnum
{
    public static TEnum Parse<TEnum>(string? value, string code, string safeDetail)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                code,
                safeDetail);
        }

        var trimmed = value.Trim();
        if (trimmed.Any(ch => !IsAsciiLetter(ch) && ch != '_'))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                code,
                safeDetail);
        }

        var normalized = trimmed.Replace("_", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                code,
                safeDetail);
        }

        return parsed;
    }

    private static bool IsAsciiLetter(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}
