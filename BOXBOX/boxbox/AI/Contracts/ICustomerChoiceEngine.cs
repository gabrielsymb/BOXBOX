namespace Boxbox.AI.Contracts;

public interface ICustomerChoiceEngine
{
    CustomerChoiceProposal Propose(CustomerChoiceRequest request, IChoiceRandomSource randomSource);
}
