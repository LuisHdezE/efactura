using EFactura.Application.ReferenceData;

namespace Infrastructure.ReferenceData;

internal static class Release1CurrencyCatalog
{
    public static IReadOnlyList<CurrencyReference> Items { get; } =
        new CurrencyReference[]
        {
            new("USD", "US Dollar"),
            new("UYI", "Uruguay Peso en Unidades Indexadas (UI)"),
            new("UYU", "Peso Uruguayo")
        };
}
