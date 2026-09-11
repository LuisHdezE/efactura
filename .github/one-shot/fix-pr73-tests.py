from pathlib import Path

path = Path("test/CrossCuttingTests/FiscalDailyReportUnsignedXmlTests.cs")
text = path.read_text()
replacements = {
    "        var root = Assert.NotNull(document.Root);\n": "        var root = document.Root;\n        Assert.NotNull(root);\n",
    "        var caratula = Assert.NotNull(root.Element(Cfe + \"Caratula\"));\n": "        var caratula = root!.Element(Cfe + \"Caratula\");\n        Assert.NotNull(caratula);\n",
    "        var summary = Assert.NotNull(root.Element(Cfe + summaryElement));\n": "        var summary = root.Element(Cfe + summaryElement);\n        Assert.NotNull(summary);\n",
    "        var data = Assert.NotNull(summary.Element(Cfe + \"RsmnData\"));\n": "        var data = summary!.Element(Cfe + \"RsmnData\");\n        Assert.NotNull(data);\n",
    "        var amount = Assert.NotNull(data.Descendants(Cfe + \"Mnts_FyT_Item\").SingleOrDefault());\n": "        var amount = data!.Descendants(Cfe + \"Mnts_FyT_Item\").SingleOrDefault();\n        Assert.NotNull(amount);\n",
    "        Assert.Equal(1, document.Root!.Elements().Count());\n": "        Assert.Single(document.Root!.Elements());\n",
}
for old, new in replacements.items():
    if text.count(old) != 1:
        raise SystemExit(f"expected one match for: {old!r}; got {text.count(old)}")
    text = text.replace(old, new, 1)
path.write_text(text)
