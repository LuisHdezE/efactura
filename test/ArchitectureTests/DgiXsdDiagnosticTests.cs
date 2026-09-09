using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace ArchitectureTests;

public sealed class DgiXsdDiagnosticTests
{
    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";

    [Fact]
    public async Task Emit_official_DGI_CFE_root_structure()
    {
        var portal = new Uri("https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=");
        var cookies = new CookieContainer();
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = cookies,
            AutomaticDecompression = DecompressionMethods.All
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 efactura-xsd-evidence/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/zip,application/octet-stream,*/*");

        using var portalResponse = await client.GetAsync(portal);
        var html = await portalResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "href=[\\\"'](?<href>[^\\\"']*xsds_fe_1_44_2[^\\\"']*)[\\\"']", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Official XSD 1.44.2 link not found on DGI portal.");

        var xsdUri = new Uri(portal, WebUtility.HtmlDecode(match.Groups["href"].Value));
        using var request = new HttpRequestMessage(HttpMethod.Get, xsdUri);
        request.Headers.Referrer = portal;
        using var response = await client.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K',
            $"Official DGI XSD response was not a ZIP. Status={(int)response.StatusCode}; bytes={bytes.Length}");

        var report = new StringBuilder();
        report.AppendLine($"DGI_XSD_VERSION=1.44.2");
        report.AppendLine($"XSD_URI={xsdUri}");
        report.AppendLine($"ZIP_BYTES={bytes.Length}");
        report.AppendLine($"ZIP_SHA256={Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}");

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read, leaveOpen: false);
        var wanted = new[] { "CFEDGI.xsd", "CFEType.xsd", "DGITypes.xsd" };
        var docs = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in wanted)
        {
            var entry = archive.Entries.Single(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            using var stream = entry.Open();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            var fileBytes = memory.ToArray();
            report.AppendLine($"FILE={name}|BYTES={fileBytes.Length}|SHA256={Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant()}");
            using var xml = new MemoryStream(fileBytes, writable: false);
            docs[name] = XDocument.Load(xml, LoadOptions.None);
        }

        var schemas = docs.Values.Select(d => d.Root!).ToArray();
        var complexTypes = schemas
            .SelectMany(s => s.Elements(Xs + "complexType"))
            .Where(x => x.Attribute("name") is not null)
            .ToDictionary(x => x.Attribute("name")!.Value, StringComparer.Ordinal);
        var simpleTypes = schemas
            .SelectMany(s => s.Elements(Xs + "simpleType"))
            .Where(x => x.Attribute("name") is not null)
            .ToDictionary(x => x.Attribute("name")!.Value, StringComparer.Ordinal);

        var cfeSchema = docs["CFEDGI.xsd"].Root!;
        report.AppendLine($"TARGET_NS={cfeSchema.Attribute("targetNamespace")?.Value}");
        var cfeRoot = cfeSchema.Elements(Xs + "element").Single(x => x.Attribute("name")?.Value == "CFE");
        report.AppendLine($"ROOT_ELEMENT={DescribeElement(cfeRoot)}");
        report.AppendLine($"COMPLEX_TYPE_COUNT={complexTypes.Count}");
        report.AppendLine($"COMPLEX_TYPES={string.Join(',', complexTypes.Keys.OrderBy(x => x, StringComparer.Ordinal))}");

        var rootTypeName = LocalName(cfeRoot.Attribute("type")?.Value);
        XElement? rootType = null;
        if (rootTypeName is not null && complexTypes.TryGetValue(rootTypeName, out var namedRootType))
            rootType = namedRootType;
        rootType ??= cfeRoot.Element(Xs + "complexType");

        report.AppendLine($"ROOT_TYPE={rootTypeName ?? "<anonymous>"}");
        if (rootType is not null)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var budget = 450;
            DumpComplexType(report, rootType, rootTypeName ?? "<anonymous>", complexTypes, simpleTypes, visited, 0, ref budget);
        }

        Assert.Fail(report.ToString());
    }

    private static void DumpComplexType(
        StringBuilder report,
        XElement complexType,
        string typeName,
        IReadOnlyDictionary<string, XElement> complexTypes,
        IReadOnlyDictionary<string, XElement> simpleTypes,
        ISet<string> visited,
        int depth,
        ref int budget)
    {
        if (budget-- <= 0 || depth > 5) return;
        var key = $"{typeName}@{depth}";
        if (!visited.Add(key)) return;
        var indent = new string(' ', depth * 2);
        report.AppendLine($"{indent}TYPE {typeName}");

        foreach (var particle in complexType.Elements().Where(IsParticle))
            DumpParticle(report, particle, complexTypes, simpleTypes, visited, depth + 1, ref budget);
    }

    private static void DumpParticle(
        StringBuilder report,
        XElement particle,
        IReadOnlyDictionary<string, XElement> complexTypes,
        IReadOnlyDictionary<string, XElement> simpleTypes,
        ISet<string> visited,
        int depth,
        ref int budget)
    {
        if (budget-- <= 0 || depth > 6) return;
        var indent = new string(' ', depth * 2);
        report.AppendLine($"{indent}{particle.Name.LocalName.ToUpperInvariant()} min={particle.Attribute("minOccurs")?.Value ?? "1"} max={particle.Attribute("maxOccurs")?.Value ?? "1"}");

        foreach (var child in particle.Elements())
        {
            if (budget-- <= 0) return;
            if (child.Name == Xs + "element")
            {
                report.AppendLine($"{indent}  ELEMENT {DescribeElement(child)}");
                var localType = LocalName(child.Attribute("type")?.Value);
                if (localType is not null && complexTypes.TryGetValue(localType, out var nestedComplex))
                {
                    DumpComplexType(report, nestedComplex, localType, complexTypes, simpleTypes, visited, depth + 2, ref budget);
                }
                else if (localType is not null && simpleTypes.TryGetValue(localType, out var nestedSimple))
                {
                    var values = nestedSimple.Descendants(Xs + "enumeration")
                        .Select(x => x.Attribute("value")?.Value)
                        .Where(x => x is not null)
                        .Take(30);
                    report.AppendLine($"{indent}    ENUM {localType}=[{string.Join(',', values)}]");
                }
                else if (child.Element(Xs + "complexType") is { } anonymous)
                {
                    DumpComplexType(report, anonymous, $"{child.Attribute("name")?.Value ?? "anonymous"}#inline", complexTypes, simpleTypes, visited, depth + 2, ref budget);
                }
            }
            else if (IsParticle(child))
            {
                DumpParticle(report, child, complexTypes, simpleTypes, visited, depth + 1, ref budget);
            }
        }
    }

    private static bool IsParticle(XElement x) =>
        x.Name == Xs + "sequence" || x.Name == Xs + "choice" || x.Name == Xs + "all";

    private static string DescribeElement(XElement element) =>
        $"name={element.Attribute("name")?.Value ?? "-"}|ref={element.Attribute("ref")?.Value ?? "-"}|type={element.Attribute("type")?.Value ?? "-"}|min={element.Attribute("minOccurs")?.Value ?? "1"}|max={element.Attribute("maxOccurs")?.Value ?? "1"}";

    private static string? LocalName(string? qname)
    {
        if (string.IsNullOrWhiteSpace(qname)) return null;
        var index = qname.IndexOf(':');
        return index >= 0 ? qname[(index + 1)..] : qname;
    }
}
