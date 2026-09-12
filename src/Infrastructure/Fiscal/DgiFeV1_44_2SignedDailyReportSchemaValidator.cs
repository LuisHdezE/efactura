using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;

namespace Infrastructure.Fiscal;

/// <summary>
/// Validates the complete signed Reporte Diario against the untouched byte-pinned v1.44.2 schema
/// closure. Unlike the pre-signature validator, this adapter does not alter ds:Signature cardinality.
/// </summary>
public sealed class DgiFeV1_44_2SignedDailyReportSchemaValidator : IFiscalDailyReportSignedSchemaValidator
{
    public const string SchemaSetId = "dgi-daily-report-v13.2-xsd-v1.44.2-signed";
    private const string RootSchema = "ReporteDiarioCFE.xsd";
    private const string ManifestFile = "daily-report-schema-manifest.json";
    private const string ResourcePrefix = "Infrastructure.Fiscal.Schemas.DgiFeV1_44_2.";
    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    private static readonly string[] RequiredFiles =
    [
        RootSchema,
        "DGITypes.xsd",
        "xmldsig-core-schema.xsd"
    ];

    private readonly SchemaState _state;

    public DgiFeV1_44_2SignedDailyReportSchemaValidator() => _state = BuildState();

    public FiscalDailyReportSignedSchemaValidationResult Validate(string signedXml)
    {
        if (!_state.Ready)
            return Result(FiscalDailyReportSignedSchemaValidationStatus.SchemaSetInvalid, _state.Errors);

        if (string.IsNullOrWhiteSpace(signedXml))
        {
            return Result(
                FiscalDailyReportSignedSchemaValidationStatus.DocumentInvalid,
                [new("fiscal.daily_report.xsd.signed_xml_required", "Signed Reporte Diario XML is required.")]);
        }

        var errors = new List<FiscalDailyReportSignedSchemaValidationError>();
        try
        {
            EnsureSignedShape(signedXml);
            var settings = SecureXmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = _state.SchemaSet!;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += (_, args) => errors.Add(new FiscalDailyReportSignedSchemaValidationError(
                args.Severity == XmlSeverityType.Warning
                    ? "fiscal.daily_report.xsd.validation_warning"
                    : "fiscal.daily_report.xsd.validation_error",
                args.Message,
                Positive(args.Exception?.LineNumber),
                Positive(args.Exception?.LinePosition)));

            using var textReader = new StringReader(signedXml);
            using var reader = XmlReader.Create(textReader, settings);
            while (reader.Read())
            {
            }
        }
        catch (SignedDailyReportValidationException ex)
        {
            errors.Add(new FiscalDailyReportSignedSchemaValidationError(ex.Code, ex.Message));
        }
        catch (XmlException ex)
        {
            errors.Add(new FiscalDailyReportSignedSchemaValidationError(
                "fiscal.daily_report.xsd.malformed_xml",
                ex.Message,
                Positive(ex.LineNumber),
                Positive(ex.LinePosition)));
        }
        catch (XmlSchemaException ex)
        {
            errors.Add(new FiscalDailyReportSignedSchemaValidationError(
                "fiscal.daily_report.xsd.validation_error",
                ex.Message,
                Positive(ex.LineNumber),
                Positive(ex.LinePosition)));
        }

        return errors.Count == 0
            ? Result(FiscalDailyReportSignedSchemaValidationStatus.Valid, Array.Empty<FiscalDailyReportSignedSchemaValidationError>())
            : Result(FiscalDailyReportSignedSchemaValidationStatus.DocumentInvalid, errors);
    }

    private FiscalDailyReportSignedSchemaValidationResult Result(
        FiscalDailyReportSignedSchemaValidationStatus status,
        IReadOnlyList<FiscalDailyReportSignedSchemaValidationError> errors) =>
        new(
            status,
            SchemaSetId,
            FiscalDailyReportV13_2WireContract.Version,
            FiscalDailyReportV13_2WireContract.SchemaArchiveVersion,
            _state.Fingerprint,
            errors);

    private static void EnsureSignedShape(string xml)
    {
        var settings = SecureXmlReaderSettings();
        using var textReader = new StringReader(xml);
        using var reader = XmlReader.Create(textReader, settings);
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.Load(reader);

        var root = document.DocumentElement;
        if (root is null
            || !string.Equals(root.LocalName, FiscalDailyReportV13_2WireContract.XmlRootElementName, StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, FiscalDailyReportV13_2WireContract.XmlNamespace, StringComparison.Ordinal))
        {
            throw new SignedDailyReportValidationException(
                "fiscal.daily_report.xsd.root_invalid",
                "Signed Reporte Diario must use the pinned DGI Reporte root and namespace.");
        }

        var signatures = document.GetElementsByTagName(
            FiscalDailyReportV13_2WireContract.XmlDigitalSignatureElementName,
            XmlDsigNamespace);
        if (signatures.Count != 1 || signatures[0] is not XmlElement signature)
        {
            throw new SignedDailyReportValidationException(
                "fiscal.daily_report.xsd.signature_required",
                "Signed Reporte Diario requires exactly one ds:Signature.");
        }

        if (!ReferenceEquals(signature, root.LastChild))
        {
            throw new SignedDailyReportValidationException(
                "fiscal.daily_report.xsd.signature_position_invalid",
                "Signed Reporte Diario requires ds:Signature as the final child of Reporte.");
        }
    }

    private static SchemaState BuildState()
    {
        var errors = new List<FiscalDailyReportSignedSchemaValidationError>();
        try
        {
            var resources = LoadAndVerifyResources();
            var fingerprint = Fingerprint(resources.Hashes);
            var resolver = new EmbeddedSchemaResolver(resources.Bytes);
            var schemaSet = new XmlSchemaSet { XmlResolver = resolver };
            schemaSet.ValidationEventHandler += (_, args) => errors.Add(new FiscalDailyReportSignedSchemaValidationError(
                args.Severity == XmlSeverityType.Warning
                    ? "fiscal.daily_report.xsd.schema_warning"
                    : "fiscal.daily_report.xsd.schema_error",
                args.Message,
                Positive(args.Exception?.LineNumber),
                Positive(args.Exception?.LinePosition)));

            var readerSettings = SecureXmlReaderSettings();
            readerSettings.XmlResolver = resolver;
            using var rootStream = new MemoryStream(resources.Bytes[RootSchema], writable: false);
            using var rootReader = XmlReader.Create(rootStream, readerSettings, resolver.UriFor(RootSchema).AbsoluteUri);
            schemaSet.Add(null, rootReader);
            schemaSet.Compile();

            return errors.Count == 0
                ? new SchemaState(true, schemaSet, fingerprint, Array.Empty<FiscalDailyReportSignedSchemaValidationError>())
                : new SchemaState(false, null, fingerprint, errors);
        }
        catch (Exception ex) when (ex is InvalidDataException
            or JsonException
            or XmlException
            or XmlSchemaException
            or KeyNotFoundException
            or InvalidOperationException)
        {
            errors.Add(new FiscalDailyReportSignedSchemaValidationError(
                "fiscal.daily_report.xsd.schema_set_invalid",
                ex.Message));
            return new SchemaState(false, null, string.Empty, errors);
        }
    }

    private static VerifiedResources LoadAndVerifyResources()
    {
        var assembly = typeof(DgiFeV1_44_2SignedDailyReportSchemaValidator).Assembly;
        using var manifest = JsonDocument.Parse(ReadResource(assembly, ManifestFile));
        var root = manifest.RootElement;

        if (!string.Equals(root.GetProperty("artifactSetId").GetString(), "dgi-daily-report-v13.2-byte-pin", StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("functionalFormatVersion").GetString(), FiscalDailyReportV13_2WireContract.Version, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("schemaArchiveVersion").GetString(), FiscalDailyReportV13_2WireContract.SchemaArchiveVersion, StringComparison.Ordinal)
            || !root.GetProperty("schemaFacts").GetProperty("signatureIsRequiredFinalChild").GetBoolean())
        {
            throw new InvalidDataException("Embedded Daily Report schema manifest identity/signature facts do not match the supported signed v13.2 / FE v1.44.2 profile.");
        }

        var artifacts = root.GetProperty("artifacts");
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Path.GetFileName(artifacts.GetProperty("reportSchema").GetProperty("path").GetString()!)] =
                artifacts.GetProperty("reportSchema").GetProperty("sha256").GetString()!
        };
        foreach (var dependency in artifacts.GetProperty("schemaClosure").EnumerateArray())
        {
            expected[Path.GetFileName(dependency.GetProperty("path").GetString()!)] =
                dependency.GetProperty("sha256").GetString()!;
        }

        if (expected.Count != RequiredFiles.Length || RequiredFiles.Any(file => !expected.ContainsKey(file)))
            throw new InvalidDataException("Daily Report manifest does not describe the exact required signed schema closure.");

        var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in RequiredFiles)
        {
            var data = ReadResource(assembly, file);
            var actual = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            if (!string.Equals(actual, expected[file], StringComparison.Ordinal))
                throw new InvalidDataException($"Embedded Daily Report schema integrity check failed for {file}.");
            bytes[file] = data;
            hashes[file] = actual;
        }

        return new VerifiedResources(bytes, hashes);
    }

    private static byte[] PrepareDependencyForCompilation(string fileName, byte[] verifiedBytes)
    {
        if (!string.Equals(fileName, "xmldsig-core-schema.xsd", StringComparison.Ordinal))
            return verifiedBytes;

        var text = Encoding.UTF8.GetString(verifiedBytes);
        var start = text.IndexOf("<!DOCTYPE schema", StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidDataException("Expected legacy W3C schema DOCTYPE is missing from xmldsig-core-schema.xsd.");
        var end = text.IndexOf("]>", start, StringComparison.Ordinal);
        if (end < 0)
            throw new InvalidDataException("Legacy W3C XMLDSig schema DOCTYPE is malformed.");
        var normalized = text.Remove(start, end + 2 - start);
        if (normalized.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("&dsig;", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Unsafe or semantically required DTD content remains in XMLDSig schema after local normalization.");
        }
        return Encoding.UTF8.GetBytes(normalized);
    }

    private static byte[] ReadResource(System.Reflection.Assembly assembly, string fileName)
    {
        var exact = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => string.Equals(name, ResourcePrefix + fileName, StringComparison.Ordinal))
            ?? assembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith("." + fileName, StringComparison.Ordinal));
        if (exact is null)
            throw new InvalidDataException($"Required embedded Daily Report schema resource is missing: {fileName}.");
        using var stream = assembly.GetManifestResourceStream(exact)
            ?? throw new InvalidDataException($"Required embedded Daily Report schema resource cannot be opened: {fileName}.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string Fingerprint(IReadOnlyDictionary<string, string> hashes)
    {
        var canonical = string.Join(
            "\n",
            hashes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}:{pair.Value}"))
            + "\nmode:signed-untouched-schema-v1";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static XmlReaderSettings SecureXmlReaderSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = false,
        IgnoreWhitespace = false,
        CloseInput = true
    };

    private static int? Positive(int? value) => value is > 0 ? value : null;

    private sealed record VerifiedResources(
        IReadOnlyDictionary<string, byte[]> Bytes,
        IReadOnlyDictionary<string, string> Hashes);

    private sealed record SchemaState(
        bool Ready,
        XmlSchemaSet? SchemaSet,
        string Fingerprint,
        IReadOnlyList<FiscalDailyReportSignedSchemaValidationError> Errors);

    private sealed class EmbeddedSchemaResolver(IReadOnlyDictionary<string, byte[]> resources) : XmlResolver
    {
        private static readonly Uri BaseUri = new("https://schemas.local/dgi/fe/1.44.2/daily-report/signed/");

        public override ICredentials? Credentials { set { } }
        public Uri UriFor(string fileName) => new(BaseUri, fileName);

        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (!string.Equals(absoluteUri.Host, BaseUri.Host, StringComparison.Ordinal)
                || !absoluteUri.AbsolutePath.StartsWith(BaseUri.AbsolutePath, StringComparison.Ordinal))
            {
                throw new XmlException($"External schema resolution is forbidden: {absoluteUri}.");
            }

            var fileName = Path.GetFileName(absoluteUri.AbsolutePath);
            if (!resources.TryGetValue(fileName, out var data) || !fileName.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
                throw new XmlException($"Schema dependency is not part of the pinned signed Daily Report closure: {fileName}.");
            return new MemoryStream(PrepareDependencyForCompilation(fileName, data), writable: false);
        }

        public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
        {
            if (string.IsNullOrWhiteSpace(relativeUri))
                return base.ResolveUri(baseUri ?? BaseUri, relativeUri);
            var resolved = new Uri(baseUri ?? BaseUri, relativeUri);
            if (!string.Equals(resolved.Host, BaseUri.Host, StringComparison.Ordinal)
                || !resolved.AbsolutePath.StartsWith(BaseUri.AbsolutePath, StringComparison.Ordinal))
            {
                throw new XmlException($"External schema resolution is forbidden: {resolved}.");
            }
            return resolved;
        }
    }

    private sealed class SignedDailyReportValidationException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}
