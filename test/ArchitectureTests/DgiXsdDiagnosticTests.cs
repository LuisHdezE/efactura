using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace ArchitectureTests;

public sealed class DgiXsdDiagnosticTests
{
    [Fact]
    public async Task Emit_official_DGI_XSD_1_44_2_inventory()
    {
        const string url = "https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("efactura-xsd-evidence/1.0");

        var bytes = await client.GetByteArrayAsync(url);
        Assert.True(bytes.Length > 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K',
            $"DGI response is not a ZIP. Bytes={bytes.Length}");

        var report = new StringBuilder();
        report.AppendLine($"DGI_XSD_URL={url}");
        report.AppendLine($"ZIP_BYTES={bytes.Length}");
        report.AppendLine($"ZIP_SHA256={Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}");

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read, leaveOpen: false);
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
                var doc = XDocument.Parse(Encoding.UTF8.GetString(fileBytes));
                XNamespace xs = "http://www.w3.org/2001/XMLSchema";
                var schema = doc.Root;
                var target = schema?.Attribute("targetNamespace")?.Value ?? "<none>";
                var roots = schema?.Elements(xs + "element")
                    .Select(x => x.Attribute("name")?.Value ?? $"ref:{x.Attribute("ref")?.Value}")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray() ?? Array.Empty<string>();
                report.AppendLine($"  TARGET_NS={target}");
                report.AppendLine($"  TOP_ELEMENTS={string.Join(',', roots)}");
            }
            catch (Exception ex)
            {
                report.AppendLine($"  PARSE_ERROR={ex.GetType().Name}:{ex.Message}");
            }
        }

        Assert.True(false, report.ToString());
    }
}
