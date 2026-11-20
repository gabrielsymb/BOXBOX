using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Boxbox.Application.LivingWorld;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.LivingWorld;
using Boxbox.Domain.Market;

namespace Boxbox.Persistence;

public sealed class JsonLivingWorldSnapshotStore : ILivingWorldSnapshotStore
{
    private static readonly JsonSerializerOptions Options = CreateOptions();
    private readonly string _path;

    public JsonLivingWorldSnapshotStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public void Save(LivingWorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var directory = Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("Snapshot path requires a parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";
        var json = JsonSerializer.Serialize(ToDocument(snapshot), Options);
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temporaryPath, _path, overwrite: true);
    }

    public LivingWorldSnapshot Load()
    {
        var json = File.ReadAllText(_path, Encoding.UTF8);
        var document = JsonSerializer.Deserialize<SnapshotDocument>(json, Options)
            ?? throw new InvalidDataException("Snapshot payload was empty.");
        var actors = document.State.Actors.Select(item => new SimulatedActorState(
            item.ActorId,
            item.CashCents,
            item.FoodNeed,
            item.Employment,
            item.WageCents,
            item.DebtCents,
            item.MaximumDebtCents,
            item.KnownFactIds));
        var store = StoreState.Restore(
            document.State.Store.ShopId,
            document.State.Store.IsOpen,
            document.State.Store.StockByProduct,
            document.State.Store.UnitPriceCentsByProduct,
            document.State.Store.CashBalanceCents,
            document.State.Store.CompletedDecisionIds);
        var competitors = document.State.Competitors.Select(item => CompetitorMarketState.Restore(
            item.CompetitorId,
            item.IsOpen,
            item.AvailableServiceCapacity,
            item.Offers.Select(offer => new CompetitorOfferState(
                offer.OfferId,
                offer.CategoryId,
                offer.ProductId,
                offer.UnitPriceCents,
                offer.AvailableQuantity,
                offer.IsActive)),
            item.CompletedDecisionIds));
        var state = new LivingWorldState(
            actors,
            store,
            document.State.EmployerCashCents,
            document.State.BankCashCents,
            document.State.CompletedCommandIds,
            competitors,
            document.State.ExternalMarketCashCents);
        return new LivingWorldSnapshot(
            document.RulesetVersion,
            document.Seed,
            document.CurrentMinute,
            state,
            document.Schedule,
            document.ProcessedEventIds,
            document.EventLog);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static SnapshotDocument ToDocument(LivingWorldSnapshot snapshot)
    {
        var actors = snapshot.State.Actors.Values
            .OrderBy(item => item.ActorId, StringComparer.Ordinal)
            .Select(item => new ActorDocument(
                item.ActorId,
                item.CashCents,
                item.FoodNeed,
                item.Employment,
                item.WageCents,
                item.DebtCents,
                item.MaximumDebtCents,
                item.KnownFactIds.ToArray()))
            .ToArray();
        var sourceStore = snapshot.State.Store;
        var store = new StoreDocument(
            sourceStore.ShopId,
            sourceStore.IsOpen,
            sourceStore.CashBalanceCents,
            sourceStore.StockByProduct.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            sourceStore.UnitPriceCentsByProduct.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            sourceStore.CompletedDecisionIds.Order(StringComparer.Ordinal).ToArray());
        var competitors = snapshot.State.Competitors.Values
            .OrderBy(item => item.CompetitorId, StringComparer.Ordinal)
            .Select(item => new CompetitorDocument(
                item.CompetitorId,
                item.IsOpen,
                item.AvailableServiceCapacity,
                item.Offers.Select(offer => new CompetitorOfferDocument(
                    offer.OfferId,
                    offer.CategoryId,
                    offer.ProductId,
                    offer.UnitPriceCents,
                    offer.AvailableQuantity,
                    offer.IsActive)).ToArray(),
                item.CompletedDecisionIds.Order(StringComparer.Ordinal).ToArray()))
            .ToArray();
        var state = new StateDocument(
            actors,
            store,
            snapshot.State.EmployerCashCents,
            snapshot.State.BankCashCents,
            snapshot.State.ExternalMarketCashCents,
            competitors,
            snapshot.State.CompletedCommandIds.Order(StringComparer.Ordinal).ToArray());
        return new SnapshotDocument(
            snapshot.RulesetVersion,
            snapshot.Seed,
            snapshot.CurrentMinute,
            state,
            snapshot.Schedule.ToArray(),
            snapshot.ProcessedEventIds.ToArray(),
            snapshot.EventLog.ToArray());
    }

    private sealed record SnapshotDocument(
        string RulesetVersion,
        ulong Seed,
        long CurrentMinute,
        StateDocument State,
        LivingWorldScheduledEvent[] Schedule,
        string[] ProcessedEventIds,
        LivingWorldEvent[] EventLog);

    private sealed record StateDocument(
        ActorDocument[] Actors,
        StoreDocument Store,
        long EmployerCashCents,
        long BankCashCents,
        long ExternalMarketCashCents,
        CompetitorDocument[] Competitors,
        string[] CompletedCommandIds);

    private sealed record ActorDocument(
        string ActorId,
        long CashCents,
        int FoodNeed,
        EmploymentStatus Employment,
        long WageCents,
        long DebtCents,
        long MaximumDebtCents,
        string[] KnownFactIds);

    private sealed record StoreDocument(
        string ShopId,
        bool IsOpen,
        long CashBalanceCents,
        Dictionary<string, int> StockByProduct,
        Dictionary<string, long> UnitPriceCentsByProduct,
        string[] CompletedDecisionIds);

    private sealed record CompetitorDocument(
        string CompetitorId,
        bool IsOpen,
        int AvailableServiceCapacity,
        CompetitorOfferDocument[] Offers,
        string[] CompletedDecisionIds);

    private sealed record CompetitorOfferDocument(
        string OfferId,
        string CategoryId,
        string? ProductId,
        long UnitPriceCents,
        int AvailableQuantity,
        bool IsActive);
}
