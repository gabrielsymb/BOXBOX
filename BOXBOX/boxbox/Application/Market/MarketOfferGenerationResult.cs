using System.Collections.ObjectModel;
using Boxbox.Application.Customers;

namespace Boxbox.Application.Market;

public sealed class MarketOfferGenerationResult
{
    private readonly IReadOnlyList<AdmissibleMarketOffer> _admissibleOffers;
    private readonly IReadOnlyList<MarketOfferPassability> _passability;

    internal MarketOfferGenerationResult(
        IEnumerable<AdmissibleMarketOffer> admissibleOffers,
        IEnumerable<MarketOfferPassability> passability)
    {
        ArgumentNullException.ThrowIfNull(admissibleOffers);
        ArgumentNullException.ThrowIfNull(passability);

        _admissibleOffers = new ReadOnlyCollection<AdmissibleMarketOffer>(
            admissibleOffers.ToList());
        _passability = new ReadOnlyCollection<MarketOfferPassability>(
            passability.ToList());
    }

    public IReadOnlyList<AdmissibleMarketOffer> AdmissibleOffers => _admissibleOffers;

    // Technical diagnostics can contain facts that the player has not observed,
    // such as real availability or service capacity. Keep them inside the core;
    // presentation receives only the admissible, permitted observations above.
    internal IReadOnlyList<MarketOfferPassability> Passability => _passability;
}
