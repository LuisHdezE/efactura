using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using EFactura.Application.Fiscal;

namespace Infrastructure.Fiscal;

public sealed class DgiFeV1_44_2SignedCfeSchemaValidator : IFiscalSignedCfeSchemaValidator
{
    public const string SchemaSetId = "dgi-fe-xsd-v1.44.2";
    public const string SchemaVersion = "1.44.2";
    private const string RootSchema = "CFEDGI.xsd";
    private const string ResourcePrefix = "Infrastructure.Fiscal.Schemas.DgiFeV1_44_2.";

    private static readonly string[] RequiredFiles =
    [
        "CFEDGI.xsd",
        "CFEType.xsd",
        "DGITypes.xsd",
        "xmldsig-core-schema.xsd",
        "xenc-schema.xsd",
        "version.txt"
    ];

    private readonly SchemaState _state;

    public DgiFeV1_44_2SignedCfeSchemaValidator() => _state = BuildState();

    public FiscalSignedCfeSchemaValidationResult Validate(string signedXml)
    {
        if (!_state.Ready)
            return Result(FiscalSignedCfeSchemaValidationStatus.SchemaSetInvalid, _state.Errors);

        if (string.IsNullOrWhiteSpace(signedXml))
        {
            return Result(
                FiscalSignedCfeSchemaValidationStatus.DocumentInvalid,
                [new("fiscal.xsd.signed_xml_required", "Signed CFE XML is required.")]);
        }

        var errors = new List<FiscalSignedCfeSchemaValidationError>();
        try
        {
            var settings = SecureXmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = _state.SchemaSet!;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += (_, args) => errors.Add(new FiscalSignedCfeSchemaValidationError(
                args.Severity == XmlSeverityType.Warning ? "fiscal.xsd.validation_warning" : "fiscal.xsd.validation_error",
                args.Message,
                Positive(args.Exception?.LineNumber),
                Positive(args.Exception?.LinePosition)));

            using var textReader = new StringReader(signedXml);
            using var reader = XmlReader.Create(textReader, settings);
            while (reader.Read())
            {
            }
        }
        catch (XmlException ex)
        {
            errors.Add(new FiscalSignedCfeSchemaValidationError(
                "fiscal.xsd.malformed_xml",
                ex.Message,
                Positive(ex.LineNumber),
                Positive(ex.LinePosition)));
        }
        catch (XmlSchemaException ex)
        {
            errors.Add(new FiscalSignedCfeSchemaValidationError(
                "fiscal.xsd.validation_error",
                ex.Message,
                Positive(ex.LineNumber),
                Positive(ex.LinePosition)));
        }

        return errors.Count == 0
            ? Result(FiscalSignedCfeSchemaValidationStatus.Valid, Array.Empty<FiscalSignedCfeSchemaValidationError>())
            : Result(FiscalSignedCfeSchemaValidationStatus.DocumentInvalid, errors);
    }

    private FiscalSignedCfeSchemaValidationResult Result(
        FiscalSignedCfeSchemaValidationStatus status,
        IReadOnlyList<FiscalSignedCfeSchemaValidationError> errors) =>
        new(status, SchemaSetId, SchemaVersion, _state.Fingerprint, errors);

    private static SchemaState BuildState()
    {
        var errors = new List<FiscalSignedCfeSchemaValidationError>();
        try
        {
            var resources = LoadAndVerifyResources();
            var fingerprint = Fingerprint(resources.Hashes);
            var resolver = new EmbeddedSchemaResolver(resources.Bytes);
            var schemaSet = new XmlSchemaSet { XmlResolver = resolver };
            schemaSet.ValidationEventHandler += (_, args) => errors.Add(new FiscalSignedCfeSchemaValidationError(
                args.Severity == XmlSeverityType.Warning ? "fiscal.xsd.schema_warning" : "fiscal.xsd.schema_error",
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

            return new SchemaState(true, schemaSet, fingerprint, Array.Empty<FiscalSignedCfeSchemaValidationError>());
        }
        catch (Exception ex) when (ex is InvalidDataException
            or JsonException
            or XmlException
            or XmlSchemaException
            or KeyNotFoundException
            or InvalidOperationException)
        {
            errors.Add(new FiscalSignedCfeSchemaValidationError(
                "fiscal.xsd.schema_set_invalid",
                ex.Message));
            return new SchemaState(false, null, string.Empty, errors);
        }
    }

    private static VerifiedResources LoadAndVerifyResources()
    {
        var assembly = typeof(DgiFeV1_44_2SignedCfeSchemaValidator).Assembly;
        var manifestBytes = ReadResource(assembly, "schema-manifest.json");
        using var manifest = JsonDocument.Parse(manifestBytes);
        var root = manifest.RootElement;

        if (!string.Equals(root.GetProperty("schemaSetId").GetString(), SchemaSetId, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("version").GetString(), SchemaVersion, StringComparison.Ordinal)
            || !string.Equals(root.GetProperty("rootSchema").GetString(), RootSchema, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Embedded DGI schema manifest identity does not match the supported v1.44.2 profile.");
        }

        var expected = root.GetProperty("files")
            .EnumerateArray()
            .ToDictionary(
                item => item.GetProperty("name").GetString()
                    ?? throw new InvalidDataException("Schema manifest contains a file without a name."),
                item => item.GetProperty("sha256").GetString()
                    ?? throw new InvalidDataException("Schema manifest contains a file without a SHA-256."),
                StringComparer.Ordinal);

        if (expected.Count != RequiredFiles.Length
            || RequiredFiles.Any(file => !expected.ContainsKey(file)))
        {
            throw new InvalidDataException("Embedded DGI schema manifest does not contain the exact required signed-CFE schema closure.");
        }

        var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in RequiredFiles)
        {
            var data = ReadResource(assembly, file);
            var actual = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            if (!string.Equals(actual, expected[file], StringComparison.Ordinal))
                throw new InvalidDataException($"Embedded DGI schema integrity check failed for {file}.");

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
            throw new InvalidDataException($"Required embedded DGI schema resource is missing: {fileName}.");

        using var stream = assembly.GetManifestResourceStream(exact)
            ?? throw new InvalidDataException($"Required embedded DGI schema resource cannot be opened: {fileName}.");
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
        IReadOnlyList<FiscalSignedCfeSchemaValidationError> Errors);

    private sealed class EmbeddedSchemaResolver(IReadOnlyDictionary<string, byte[]> resources) : XmlResolver
    {
        private static readonly Uri BaseUri = new("https://schemas.local/dgi/fe/1.44.2/");

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
            if (!resources.TryGetValue(fileName, out var data) || !fileName.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
                throw new XmlException($"Schema dependency is not part of the pinned DGI closure: {fileName}.");

            return new MemoryStream(data, writable: false);
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
