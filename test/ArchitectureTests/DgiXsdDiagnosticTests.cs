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
    public async Task Emit_official_DGI_domestic_CFE_build_contract()
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
        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(zipBytes.Length > 4 && zipBytes[0] == (byte)'P' && zipBytes[1] == (byte)'K',
            $"Official DGI XSD response was not a ZIP. Status={(int)response.StatusCode}; bytes={zipBytes.Length}");

        var report = new StringBuilder();
        report.AppendLine("DGI_XSD_VERSION=1.44.2");
        report.AppendLine($"ZIP_BYTES={zipBytes.Length}");
        report.AppendLine($"ZIP_SHA256={Convert.ToHexString(SHA256.HashData(zipBytes)).ToLowerInvariant()}");

        using var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read, leaveOpen: false);
        var docs = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in new[] { "CFEDGI.xsd", "CFEType.xsd", "DGITypes.xsd" })
        {
            var entry = archive.Entries.Single(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            using var entryStream = entry.Open();
            using var memory = new MemoryStream();
            await entryStream.CopyToAsync(memory);
            var bytes = memory.ToArray();
            using var xml = new MemoryStream(bytes, writable: false);
            docs[name] = XDocument.Load(xml, LoadOptions.None);
            report.AppendLine($"FILE={name}|BYTES={bytes.Length}|SHA256={Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}");
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

        report.AppendLine("TARGET_NS=http://cfe.dgi.gub.uy");
        var cfeDef = complexTypes["CFEDefType"];
        var familyChoice = cfeDef.Descendants(Xs + "choice").First();

        foreach (var family in new[] { "eTck", "eFact" })
        {
            var familyElement = familyChoice.Elements(Xs + "element")
                .Single(e => e.Attribute("name")?.Value == family);
            report.AppendLine($"=== FAMILY {family} ===");
            DumpAnonymousElement(report, familyElement, complexTypes, simpleTypes, depth: 0, maxDepth: 12);
        }

        foreach (var typeName in new[]
        {
            "IdDoc_Tck", "IdDoc_Fact", "Emisor", "Receptor_Tck", "Receptor_Fact",
            "Totales", "Totales_Boleta", "Item_Det_Fact", "Item_Det_Boleta",
            "CAEDataType", "MediosPago"
        })
        {
            report.AppendLine($"=== NAMED TYPE {typeName} ===");
            if (complexTypes.TryGetValue(typeName, out var complexType))
                DumpComplexType(report, complexType, complexTypes, simpleTypes, depth: 0, maxDepth: 5, new HashSet<string>(StringComparer.Ordinal));
            else if (simpleTypes.TryGetValue(typeName, out var simpleType))
                DumpSimpleType(report, typeName, simpleType, depth: 0);
            else
                report.AppendLine("NOT_FOUND");
        }

        foreach (var simpleName in new[] { "FechaHoraType", "FormaPagoType", "TipoCFEType", "IndFactType" })
        {
            report.AppendLine($"=== SIMPLE TYPE {simpleName} ===");
            if (simpleTypes.TryGetValue(simpleName, out var simpleType))
                DumpSimpleType(report, simpleName, simpleType, depth: 0);
            else
                report.AppendLine("NOT_FOUND");
        }

        Assert.Fail(report.ToString());
    }

    private static void DumpAnonymousElement(
        StringBuilder report,
        XElement element,
        IReadOnlyDictionary<string, XElement> complexTypes,
        IReadOnlyDictionary<string, XElement> simpleTypes,
        int depth,
        int maxDepth)
    {
        var indent = new string(' ', depth * 2);
        report.AppendLine($"{indent}ELEMENT {DescribeElement(element)}");
        var namedType = LocalName(element.Attribute("type")?.Value);
        if (namedType is not null && complexTypes.TryGetValue(namedType, out var namedComplex))
        {
            DumpComplexType(report, namedComplex, complexTypes, simpleTypes, depth + 1, maxDepth, new HashSet<string>(StringComparer.Ordinal));
            return;
        }
        if (namedType is not null && simpleTypes.TryGetValue(namedType, out var namedSimple))
        {
            DumpSimpleType(report, namedType, namedSimple, depth + 1);
            return;
        }
        if (element.Element(Xs + "complexType") is { } anonymous)
            DumpComplexType(report, anonymous, complexTypes, simpleTypes, depth + 1, maxDepth, new HashSet<string>(StringComparer.Ordinal));
    }

    private static void DumpComplexType(
        StringBuilder report,
        XElement complexType,
        IReadOnlyDictionary<string, XElement> complexTypes,
        IReadOnlyDictionary<string, XElement> simpleTypes,
        int depth,
        int maxDepth,
        ISet<string> path)
    {
        if (depth > maxDepth) return;
        foreach (var particle in complexType.Elements().Where(IsParticle))
            DumpParticle(report, particle, complexTypes, simpleTypes, depth, maxDepth, path);
    }

    private static void DumpParticle(
        StringBuilder report,
        XElement particle,
        IReadOnlyDictionary<string, XElement> complexTypes,
        IReadOnlyDictionary<string, XElement> simpleTypes,
        int depth,
        int maxDepth,
        ISet<string> path)
    {
        if (depth > maxDepth) return;
        var indent = new string(' ', depth * 2);
        report.AppendLine($"{indent}{particle.Name.LocalName.ToUpperInvariant()} min={particle.Attribute("minOccurs")?.Value ?? "1"} max={particle.Attribute("maxOccurs")?.Value ?? "1"}");

        foreach (var child in particle.Elements())
        {
            if (child.Name == Xs + "element")
            {
                report.AppendLine($"{indent}  ELEMENT {DescribeElement(child)}");
                if (depth >= maxDepth) continue;

                var localType = LocalName(child.Attribute("type")?.Value);
                if (localType is not null && complexTypes.TryGetValue(localType, out var nestedComplex))
                {
                    var key = $"complex:{localType}";
                    if (path.Add(key))
                    {
                        DumpComplexType(report, nestedComplex, complexTypes, simpleTypes, depth + 2, maxDepth, path);
                        path.Remove(key);
                    }
                }
                else if (localType is not null && simpleTypes.TryGetValue(localType, out var nestedSimple))
                {
                    DumpSimpleType(report, localType, nestedSimple, depth + 2);
                }
                else if (child.Element(Xs + "complexType") is { } anonymous)
                {
                    DumpComplexType(report, anonymous, complexTypes, simpleTypes, depth + 2, maxDepth, path);
                }
            }
            else if (IsParticle(child))
            {
                DumpParticle(report, child, complexTypes, simpleTypes, depth + 1, maxDepth, path);
            }
        }
    }

    private static void DumpSimpleType(StringBuilder report, string typeName, XElement simpleType, int depth)
    {
        var indent = new string(' ', depth * 2);
        var restriction = simpleType.Element(Xs + "restriction");
        var union = simpleType.Element(Xs + "union");
        report.AppendLine($"{indent}SIMPLE {typeName}|base={restriction?.Attribute("base")?.Value ?? "-"}|union={union?.Attribute("memberTypes")?.Value ?? "-"}");
        if (restriction is null) return;

        var facets = restriction.Elements()
            .Where(x => x.Name != Xs + "annotation")
            .Select(x => $"{x.Name.LocalName}={x.Attribute("value")?.Value}")
            .Take(80);
        report.AppendLine($"{indent}  FACETS {string.Join('|', facets)}");
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
