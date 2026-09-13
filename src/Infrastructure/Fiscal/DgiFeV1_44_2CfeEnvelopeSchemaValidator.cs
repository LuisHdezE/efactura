using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using EFactura.Application.Fiscal;

namespace Infrastructure.Fiscal;

public sealed class DgiFeV1_44_2CfeEnvelopeSchemaValidator : IFiscalCfeEnvelopeSchemaValidator
{
    public const string SchemaSetId = "dgi-fe-envelope-xsd-v1.44.2";
    public const string SchemaVersion = "1.44.2";
    public const string FunctionalFormatVersion = "05";
    private const string RootSchema = "EnvioCFE.xsd";
    private const string ResourcePrefix = "Infrastructure.Fiscal.Schemas.DgiFeV1_44_2.";

    private static readonly string[] RequiredFiles =
    [
        "EnvioCFE.xsd",
        "CFEType.xsd",
        "DGITypes.xsd",
        "xmldsig-core-schema.xsd",
        "xenc-schema.xsd",
        "version.txt"
    ];

    private static readonly HashSet<string> LegacyW3cDoctypeSchemas =
        new(StringComparer.Ordinal)
        {
            "xmldsig-core-schema.xsd",
            "xenc-schema.xsd"
        };

    private readonly SchemaState _state;

    public DgiFeV1_44_2CfeEnvelopeSchemaValidator() => _state = BuildState();

    public FiscalCfeEnvelopeSchemaValidationResult Validate(string envelopeXml)
    {
        if (!_state.Ready)
            return Result(FiscalCfeEnvelopeSchemaValidationStatus.SchemaSetInvalid, _state.Errors);

        if (string.IsNullOrWhiteSpace(envelopeXml))
        {
            return Result(
                FiscalCfeEnvelopeSchemaValidationStatus.DocumentInvalid,
                [new("fiscal.envelope.xsd.xml_required", "Sobre XML is required.")]);
        }

        var errors = new List<FiscalCfeEnvelopeSchemaValidationError>();
        try
        {
            var settings = SecureXmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = _state.SchemaSet!;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += (_, args) => errors.Add(new FiscalCfeEnvelopeSchemaValidationError(
                args.Severity == XmlSeverityType.Warning
                    ? "fiscal.envelope.xsd.validation_warning"
                    : "fiscal.envelope.xsd.validation_error",
                args.Message,
                Positive(args.Exception?.LineNumber),
                Positive(args.Exception?.LinePosition)));

            using var textReader = new StringReader(envelopeXml);
            using var reader = XmlReader.Create(textReader, settings);
            while (reader.Read())
            {
            }
        }
        catch (XmlException ex)
        {
            errors.Add(new FiscalCfeEnvelopeSchemaValidationError(
                "fiscal.envelope.xsd.malformed_xml",
                ex.Message,
                Positive(ex.LineNumber),
                Positive(ex.LinePosition)));
        }
        catch (XmlSchemaException ex)
        {
            errors.Add(new FiscalCfeEnvelopeSchemaValidationError(
                "fiscal.envelope.xsd.validation_error",
                ex.Message,
                Positive(ex.LineNumber),
                Positive(ex.LinePosition)));
        }

        return errors.Count == 0
            ? Result(FiscalCfeEnvelopeSchemaValidationStatus.Valid, Array.Empty<FiscalCfeEnvelopeSchemaValidationError>())
            : Result(FiscalCfeEnvelopeSchemaValidationStatus.DocumentInvalid, errors);
    }

    private FiscalCfeEnvelopeSchemaValidationResult Result(
        FiscalCfeEnvelopeSchemaValidationStatus status,
        IReadOnlyList<FiscalCfeEnvelopeSchemaValidationError> errors) =>
        new(status, SchemaSetId, SchemaVersion, _state.Fingerprint, errors);

    private static SchemaState BuildState()
    {
        var errors = new List<FiscalCfeEnvelopeSchemaValidationError>();
        try
        {
            var resources = LoadAndVerifyResources();
            var fingerprint = Fingerprint(resources.Hashes);
            var resolver = new EmbeddedSchemaResolver(resources.Bytes);
            var schemaSet = new XmlSchemaSet { XmlResolver = resolver };
            schemaSet.ValidationEventHandler += (_, args) => errors.Add(new FiscalCfeEnvelopeSchemaValidationError(
                args.Severity == XmlSeverityType.Warning
                    ? "fiscal.envelope.xsd.schema_warning"
                    : "fiscal.envelope.xsd.schema_error",
                args.Message,
                Positive(args.Exception?.LineNumber),
                Positive(args.Exception?.LinePosition)));

            var readerSettings = SecureXmlReaderSettings();
            readerSettings.XmlResolver = resolver;
            using var rootStream = new MemoryStream(resources.Bytes[RootSchema], writable: false);
            using var rootReader = XmlReader.Create(rootStream, readerSettings, resolver.UriFor(RootSchema).AbsoluteUri);
            schemaSet.Add(null, rootReader);
            schemaSet.Compile();

            if (errors.Count != 0)
                return new SchemaState(false, null, fingerprint, errors);

            return new SchemaState(true, schemaSet, fingerprint, Array.Empty<FiscalCfeEnvelopeSchemaValidationError>());
        }
        catch (Exception ex) when (ex is InvalidDataException
            or JsonException
            or XmlException
            or XmlSchemaException
            or KeyNotFoundException
            or InvalidOperationException)
        {
            errors.Add(new FiscalCfeEnvelopeSchemaValidationError(
                "fiscal.envelope.xsd.schema_set_invalid",
                ex.Message));
            return new SchemaState(false, null, string.Empty, errors);
        }
    }

    private static VerifiedResources LoadAndVerifyResources()
    {
        var assembly = typeof(DgiFeV1_44_2CfeEnvelopeSchemaValidator).Assembly;
        var manifestBytes = ReadResource(assembly, "envelope-schema-manifest.json");
        using var manifest = JsonDocument.Parse(manifestBytes);
        var root = manifest.RootElement;

        if (!string.Equals(root.GetProperty("schemaSetId").GetString(), SchemaSetId, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("version").GetString(), SchemaVersion, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("functionalFormatVersion").GetString(), FunctionalFormatVersion, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("rootSchema").GetString(), RootSchema, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Embedded DGI Sobre schema manifest identity does not match the governed v05/v1.44.2 profile.");
        }

        var expected = root.GetProperty("files")
            .EnumerateArray()
            .ToDictionary(
                item => item.GetProperty("name").GetString()
                    ?? throw new InvalidDataException("Sobre schema manifest contains a file without a name."),
                item => item.GetProperty("sha256").GetString()
                    ?? throw new InvalidDataException("Sobre schema manifest contains a file without a SHA-256."),
                StringComparer.Ordinal);

        if (expected.Count != RequiredFiles.Length
            || RequiredFiles.Any(file => !expected.ContainsKey(file)))
        {
            throw new InvalidDataException("Embedded DGI Sobre schema manifest does not contain the exact required schema closure.");
        }

        var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in RequiredFiles)
        {
            var data = ReadResource(assembly, file);
            var actual = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            if (!string.Equals(actual, expected[file], StringComparison.Ordinal))
                throw new InvalidDataException($"Embedded DGI Sobre schema integrity check failed for {file}.");

            bytes[file] = data;
            hashes[file] = actual;
        }

        var versionText = Encoding.UTF8.GetString(bytes["version.txt"]);
        if (!string.Equals(versionText, "version: 1.44.2", StringComparison.Ordinal))
            throw new InvalidDataException("Embedded DGI schema version marker is not exactly version: 1.44.2.");

        return new VerifiedResources(bytes, hashes);
    }

    private static byte[] ReadResource(System.Reflection.Assembly assembly, string fileName)
    {
        var exact = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => string.Equals(name, ResourcePrefix + fileName, StringComparison.Ordinal))
            ?? assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("." + fileName, StringComparison.Ordinal));

        if (exact is null)
            throw new InvalidDataException($"Required embedded DGI Sobre schema resource is missing: {fileName}.");

        using var stream = assembly.GetManifestResourceStream(exact)
            ?? throw new InvalidDataException($"Required embedded DGI Sobre schema resource cannot be opened: {fileName}.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string Fingerprint(IReadOnlyDictionary<string, string> hashes)
    {
        var canonical = string.Join(
            "\n",
            hashes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}:{pair.Value}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static byte[] PrepareSchemaForCompilation(string fileName, byte[] verifiedBytes)
    {
        if (!LegacyW3cDoctypeSchemas.Contains(fileName))
            return verifiedBytes;

        var text = Encoding.UTF8.GetString(verifiedBytes);
        var start = text.IndexOf("<!DOCTYPE schema", StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidDataException($"Expected legacy W3C schema DOCTYPE is missing from {fileName}.");

        var end = text.IndexOf("]>", start, StringComparison.Ordinal);
        if (end < 0)
            throw new InvalidDataException($"Legacy W3C schema DOCTYPE is malformed in {fileName}.");

        var doctype = text.Substring(start, end + 2 - start);
        if (!doctype.Contains("http://www.w3.org/2001/XMLSchema.dtd", StringComparison.Ordinal))
            throw new InvalidDataException($"Unexpected legacy schema DOCTYPE target in {fileName}.");

        var normalized = text.Remove(start, end + 2 - start);
        if (normalized.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("&dsig;", StringComparison.Ordinal)
            || normalized.Contains("&xenc;", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsafe or semantically required DTD content remains in {fileName} after local normalization.");
        }

        return Encoding.UTF8.GetBytes(normalized);
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
        IReadOnlyList<FiscalCfeEnvelopeSchemaValidationError> Errors);

    private sealed class EmbeddedSchemaResolver(IReadOnlyDictionary<string, byte[]> resources) : XmlResolver
    {
        private static readonly Uri BaseUri = new("https://schemas.local/dgi/fe/1.44.2/envelope/");

        public override ICredentials? Credentials
        {
            set { }
        }

        public Uri UriFor(string fileName) => new(BaseUri, fileName);

        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (!string.Equals(absoluteUri.Host, BaseUri.Host, StringComparison.Ordinal)
                || !absoluteUri.AbsolutePath.StartsWith(BaseUri.AbsolutePath, StringComparison.Ordinal))
            {
                throw new XmlException($"External schema resolution is forbidden: {absoluteUri}.");
            }

            var fileName = Path.GetFileName(absoluteUri.AbsolutePath);
            if (!resources.TryGetValue(fileName, out var data)
                || !fileName.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
            {
                throw new XmlException($"Schema dependency is not part of the pinned DGI Sobre closure: {fileName}.");
            }

            return new MemoryStream(PrepareSchemaForCompilation(fileName, data), writable: false);
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
}
