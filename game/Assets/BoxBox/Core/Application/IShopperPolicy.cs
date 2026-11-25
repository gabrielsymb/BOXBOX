using BoxBox.Domain;

namespace BoxBox.Application
{
    public interface IShopperPolicy
    {
        CustomerDecision Decide(ShopSnapshot shop);
    }
}
