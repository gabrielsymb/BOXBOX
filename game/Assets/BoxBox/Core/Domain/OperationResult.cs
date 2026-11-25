namespace BoxBox.Domain
{
    public enum OperationCode
    {
        Success,
        WrongPhase,
        InvalidOrder,
        InsufficientFunds,
        UnknownProduct,
        InvalidPrice,
        NoCustomersRemaining,
        InvalidCustomerChoice,
        ArithmeticOverflow,
        OutOfStock,
        InvalidDelivery
    }

    public readonly struct OperationResult
    {
        public bool IsSuccess { get; }
        public OperationCode Code { get; }

        private OperationResult(bool isSuccess, OperationCode code)
        {
            IsSuccess = isSuccess;
            Code = code;
        }

        public static OperationResult Success()
        {
            return new OperationResult(true, OperationCode.Success);
        }

        public static OperationResult Failure(OperationCode code)
        {
            return new OperationResult(false, code);
        }
    }
}
