using System.Globalization;
using Boxbox.Application.BusinessDay;

var locale = args.Any(item => string.Equals(item, "--lang=en", StringComparison.OrdinalIgnoreCase))
    ? "en"
    : "pt-BR";
var seedArgument = args.FirstOrDefault(item => item.StartsWith("--seed=", StringComparison.OrdinalIgnoreCase));
var seed = seedArgument is null
    ? 20261119UL
    : ulong.Parse(seedArgument["--seed=".Length..], CultureInfo.InvariantCulture);
var decisions = ReadDecisions(args);
var result = GroceryDayOneScenario.Run(seed, decisions);
var text = new DayOneConsoleText(locale);

Console.WriteLine(text.Title(seed));
Console.WriteLine(text.Cash(result.Report.OpeningCashCents, result.Report.ClosingCashCents));
Console.WriteLine(text.Totals(
    result.Report.PurchaseSpendCents,
    result.Report.RevenueCents,
    result.Report.GrossResultCents,
    result.Report.InventoryAtCostCents));
Console.WriteLine();
Console.WriteLine(text.ProductsHeader);
foreach (var product in result.Report.Products)
{
    Console.WriteLine(text.Product(product));
}

Console.WriteLine();
Console.WriteLine(text.Market(
    result.Report.PlayerSales,
    result.Report.CompetitorSales,
    result.Report.NoPurchases,
    result.Report.UnservedDemand));

static IReadOnlyList<DayOneProductDecision> ReadDecisions(string[] arguments)
{
    return GroceryDayOneScenario.DefaultDecisions
        .Select(defaultDecision =>
        {
            var prefix = $"--{defaultDecision.ProductId}=";
            var argument = arguments.FirstOrDefault(item => item.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase));
            if (argument is null)
            {
                return defaultDecision;
            }

            var parts = argument[prefix.Length..].Split(':');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var quantity) ||
                !decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var priceReais) ||
                priceReais <= 0 ||
                priceReais * 100m != decimal.Truncate(priceReais * 100m))
            {
                throw new ArgumentException(
                    $"Invalid {defaultDecision.ProductId} decision. Use quantity:price:regular|emergency, for example 5:8.00:regular.");
            }

            var channel = parts[2].ToLowerInvariant();
            if (channel is not "regular" and not "emergency")
            {
                throw new ArgumentException(
                    $"Invalid supplier channel '{parts[2]}'. Use regular or emergency.");
            }

            return defaultDecision with
            {
                SupplierOfferId = $"{channel}-{defaultDecision.ProductId}",
                OrderQuantity = quantity,
                SalePriceCents = checked((long)(priceReais * 100m))
            };
        })
        .ToArray();
}

internal sealed class DayOneConsoleText
{
    private readonly bool _english;
    private readonly NumberFormatInfo _moneyFormat;

    internal DayOneConsoleText(string locale)
    {
        _english = string.Equals(locale, "en", StringComparison.Ordinal);
        _moneyFormat = (NumberFormatInfo)CultureInfo.GetCultureInfo(
            _english ? "en-US" : "pt-BR").NumberFormat.Clone();
        _moneyFormat.CurrencySymbol = "R$";
    }

    internal string ProductsHeader => _english ? "Products" : "Produtos";

    internal string Title(ulong seed) => _english
        ? $"BOXBOX — Grocery day closed (seed {seed})"
        : $"BOXBOX — Dia da mercearia encerrado (seed {seed})";

    internal string Cash(long opening, long closing) => _english
        ? $"Cash: {Money(opening)} → {Money(closing)}"
        : $"Caixa: {Money(opening)} → {Money(closing)}";

    internal string Totals(long purchases, long revenue, long grossResult, long inventory) => _english
        ? $"Purchases {Money(purchases)} | Revenue {Money(revenue)} | Gross result {Money(grossResult)} | Remaining inventory {Money(inventory)}"
        : $"Compras {Money(purchases)} | Receita {Money(revenue)} | Resultado bruto {Money(grossResult)} | Estoque restante {Money(inventory)}";

    internal string Product(ProductDayReport product)
    {
        var name = ProductName(product.ProductId);
        var signal = Signal(product.InventorySignalKey);
        return _english
            ? $"- {name}: bought {product.PurchasedUnits}, sold {product.SoldUnits}, remaining {product.RemainingUnits}; gross {Money(product.GrossResultCents)}; {signal}"
            : $"- {name}: comprou {product.PurchasedUnits}, vendeu {product.SoldUnits}, restaram {product.RemainingUnits}; bruto {Money(product.GrossResultCents)}; {signal}";
    }

    internal string Market(int player, int competitor, int noPurchase, int unserved) => _english
        ? $"Market outcomes: BOXBOX {player} | Rival {competitor} | No purchase {noPurchase} | Unserved {unserved}"
        : $"Resultados do mercado: BOXBOX {player} | Rival {competitor} | Não comprou {noPurchase} | Não atendida {unserved}";

    private string ProductName(string productId) => (productId, _english) switch
    {
        ("rice", false) => "Arroz",
        ("soda", false) => "Refrigerante",
        ("milk", false) => "Leite",
        ("rice", true) => "Rice",
        ("soda", true) => "Soda",
        ("milk", true) => "Milk",
        _ => productId
    };

    private string Signal(string key) => (key, _english) switch
    {
        ("report.inventory.sold_out", false) => "estoque esgotado",
        ("report.inventory.no_sales", false) => "nenhuma venda",
        ("report.inventory.high_turnover", false) => "giro alto",
        ("report.inventory.slow_moving", false) => "giro lento",
        ("report.inventory.balanced", false) => "giro equilibrado",
        ("report.inventory.sold_out", true) => "sold out",
        ("report.inventory.no_sales", true) => "no sales",
        ("report.inventory.high_turnover", true) => "high turnover",
        ("report.inventory.slow_moving", true) => "slow moving",
        ("report.inventory.balanced", true) => "balanced turnover",
        _ => key
    };

    private string Money(long cents) => (cents / 100m).ToString("C", _moneyFormat);
}
