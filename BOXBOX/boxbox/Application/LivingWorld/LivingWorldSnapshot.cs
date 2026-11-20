using Boxbox.Domain.LivingWorld;

namespace Boxbox.Application.LivingWorld;

public sealed record LivingWorldSnapshot(
    string RulesetVersion,
    ulong Seed,
    long CurrentMinute,
    LivingWorldState State,
    IReadOnlyList<LivingWorldScheduledEvent> Schedule,
    IReadOnlyList<string> ProcessedEventIds,
    IReadOnlyList<LivingWorldEvent> EventLog);

public interface ILivingWorldSnapshotStore
{
    void Save(LivingWorldSnapshot snapshot);
    LivingWorldSnapshot Load();
}
