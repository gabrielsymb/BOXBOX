using Boxbox.AI.Contracts;

namespace Boxbox.AI.LivingWorld;

public enum NpcActionKind
{
    PurchaseMeal,
    BorrowForEssentials,
    SeekEmployment,
    Wait
}

public sealed record NpcActionCandidate(
    string ActionId,
    NpcActionKind Kind,
    int Weight,
    string ReasonKey,
    long AmountCents = 0);

public sealed record NpcDecisionRequest(
    string DecisionId,
    string ActorId,
    IReadOnlyList<NpcActionCandidate> ValidActions);

public sealed record NpcDecisionProposal(
    string DecisionId,
    string ActorId,
    NpcActionCandidate SelectedAction,
    double Draw);

public interface INpcDecisionPolicy
{
    NpcDecisionProposal Propose(NpcDecisionRequest request, IChoiceRandomSource random);
}

/// <summary>
/// Lightweight local fallback. It can only choose from the immutable valid set
/// supplied by the application and never mutates world state.
/// </summary>
public sealed class WeightedNpcDecisionPolicy : INpcDecisionPolicy
{
    public NpcDecisionProposal Propose(NpcDecisionRequest request, IChoiceRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(random);
        if (request.ValidActions.Count == 0)
        {
            throw new ArgumentException("A decision requires at least one valid action.", nameof(request));
        }

        var ordered = request.ValidActions.OrderBy(item => item.ActionId, StringComparer.Ordinal).ToArray();
        if (ordered.Any(item => item.Weight < 0))
        {
            throw new ArgumentException("Action weights cannot be negative.", nameof(request));
        }

        var total = ordered.Sum(item => (long)item.Weight);
        if (total <= 0)
        {
            throw new ArgumentException("A decision requires positive total weight.", nameof(request));
        }

        var draw = random.NextUnitDouble();
        var threshold = draw * total;
        long cumulative = 0;
        foreach (var candidate in ordered)
        {
            cumulative += candidate.Weight;
            if (threshold < cumulative)
            {
                return new NpcDecisionProposal(request.DecisionId, request.ActorId, candidate, draw);
            }
        }

        return new NpcDecisionProposal(request.DecisionId, request.ActorId, ordered[^1], draw);
    }
}
