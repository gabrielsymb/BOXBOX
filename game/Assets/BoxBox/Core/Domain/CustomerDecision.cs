namespace BoxBox.Domain
{
    public enum CustomerOutcome
    {
        BoughtFromPlayer,
        BoughtFromCompetitor,
        Passed
    }

    public readonly struct CustomerDecision
    {
        public string ProductId { get; }
        public CustomerOutcome Outcome { get; }

        public CustomerDecision(string productId, CustomerOutcome outcome)
        {
            ProductId = productId;
            Outcome = outcome;
        }
    }
}
