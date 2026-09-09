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
    [Fact]
    public async Task Emit_official_DGI_XSD_1_44_2_inventory()
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
        var report = new StringBuilder();
        report.AppendLine($"PORTAL_STATUS={(int)portalResponse.StatusCode} {portalResponse.StatusCode}");
        report.AppendLine($"PORTAL_FINAL_URI={portalResponse.RequestMessage?.RequestUri}");
        report.AppendLine($"PORTAL_BYTES={Encoding.UTF8.GetByteCount(html)}");
        report.AppendLine($"COOKIE_COUNT={cookies.GetCookies(portal).Count}");

        if (!match.Success)
        {
            report.AppendLine("XSD_LINK_NOT_FOUND");
            Assert.Fail(report.ToString());
        }

        var href = WebUtility.HtmlDecode(match.Groups["href"].Value);
        var xsdUri = new Uri(portal, href);
        report.AppendLine($"XSD_HREF={href}");
        report.AppendLine($"XSD_REQUEST_URI={xsdUri}");

        using var request = new HttpRequestMessage(HttpMethod.Get, xsdUri);
        request.Headers.Referrer = portal;
        using var response = await client.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        report.AppendLine($"XSD_STATUS={(int)response.StatusCode} {response.StatusCode}");
        report.AppendLine($"XSD_FINAL_URI={response.RequestMessage?.RequestUri}");
        report.AppendLine($"XSD_CONTENT_TYPE={response.Content.Headers.ContentType}");
        report.AppendLine($"XSD_CONTENT_LENGTH_HEADER={response.Content.Headers.ContentLength?.ToString() ?? "<none>"}");
        report.AppendLine($"XSD_BYTES={bytes.Length}");

        if (bytes.Length <= 4 || bytes[0] != (byte)'P' || bytes[1] != (byte)'K')
        {
            var preview = Encoding.UTF8.GetString(bytes.Take(Math.Min(bytes.Length, 500)).ToArray());
            report.AppendLine($"XSD_BODY_PREVIEW={preview.Replace('\r', ' ').Replace('\n', ' ')}");
            Assert.Fail(report.ToString());
        }

        report.AppendLine($"ZIP_SHA256={Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}");
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read, leaveOpen: false);
        report.AppendLine($"ZIP_ENTRY_COUNT={archive.Entries.Count}");

        foreach (var entry in archive.Entries.OrderBy(e => e.FullName, StringComparer.Ordinal))
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            using var stream = entry.Open();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            var fileBytes = memory.ToArray();
            report.AppendLine($"FILE={entry.FullName}|BYTES={fileBytes.Length}|SHA256={Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant()}");

            if (!entry.Name.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                using var xmlStream = new MemoryStream(fileBytes, writable: false);
                var doc = XDocument.Load(xmlStream, LoadOptions.None);
                XNamespace xs = "http://www.w3.org/2001/XMLSchema";
                var schema = doc.Root;
                var target = schema?.Attribute("targetNamespace")?.Value ?? "<none>";
                var roots = schema?.Elements(xs + "element")
                    .Select(x => x.Attribute("name")?.Value ?? $"ref:{x.Attribute("ref")?.Value}")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Take(40)
                    .ToArray() ?? Array.Empty<string>();
                var imports = schema?.Elements(xs + "import")
                    .Select(x => $"{x.Attribute("namespace")?.Value}|{x.Attribute("schemaLocation")?.Value}")
                    .Take(40)
                    .ToArray() ?? Array.Empty<string>();
                var includes = schema?.Elements(xs + "include")
                    .Select(x => x.Attribute("schemaLocation")?.Value)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Take(40)
                    .ToArray() ?? Array.Empty<string>();
                report.AppendLine($"  TARGET_NS={target}");
                report.AppendLine($"  TOP_ELEMENTS={string.Join(',', roots)}");
                report.AppendLine($"  IMPORTS={string.Join(',', imports)}");
                report.AppendLine($"  INCLUDES={string.Join(',', includes)}");
            }
            catch (Exception ex)
            {
                report.AppendLine($"  PARSE_ERROR={ex.GetType().Name}:{ex.Message}");
                report.AppendLine($"  FIRST_BYTES={Convert.ToHexString(fileBytes.Take(32).ToArray())}");
            }
        }

        Assert.Fail(report.ToString());
    }
}
