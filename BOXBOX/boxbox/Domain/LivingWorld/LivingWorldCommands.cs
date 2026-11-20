using Boxbox.Domain.Market;

namespace Boxbox.Domain.LivingWorld;

public abstract record LivingWorldCommand(string CommandId, string ActorId);

public sealed record IncreaseFoodNeedCommand(string CommandId, string ActorId, int Amount)
    : LivingWorldCommand(CommandId, ActorId);

public sealed record PaySalaryCommand(string CommandId, string ActorId)
    : LivingWorldCommand(CommandId, ActorId);

public sealed record LoseJobCommand(string CommandId, string ActorId, string FactId)
    : LivingWorldCommand(CommandId, ActorId);

public sealed record PurchaseMealCommand(string CommandId, string ActorId)
    : LivingWorldCommand(CommandId, ActorId);

public sealed record BorrowForEssentialsCommand(string CommandId, string ActorId, long AmountCents)
    : LivingWorldCommand(CommandId, ActorId);

public sealed record SeekEmploymentCommand(string CommandId, string ActorId, bool Succeeded)
    : LivingWorldCommand(CommandId, ActorId);

public sealed record SetStoreOpenCommand(string CommandId, bool IsOpen)
    : LivingWorldCommand(CommandId, "player");

public enum LivingWorldEffectKind
{
    NeedIncreased,
    SalaryPaid,
    JobLost,
    MealPurchased,
    MarketPurchaseResolved,
    LoanReceived,
    EmploymentFound,
    EmploymentSearchFailed,
    StoreOpened,
    StoreClosed
}

public enum LivingWorldRejectionReason
{
    None,
    DuplicateCommand,
    UnknownActor,
    InvalidAmount,
    ActorNotEmployed,
    ActorAlreadyEmployed,
    InsufficientEmployerCash,
    StoreClosed,
    StoreOutOfStock,
    InsufficientActorCash,
    NeedNotPresent,
    DebtLimitExceeded,
    InsufficientBankCash,
    MarketResolutionRejected,
    MarketResolutionMismatch,
    UnsupportedCommand
}

public sealed record LivingWorldEffect(
    LivingWorldEffectKind Kind,
    string ActorId,
    string ReasonKey,
    long CashDeltaCents = 0,
    int NeedDelta = 0,
    int StoreStockDelta = 0,
    MarketPurchaseOutcomeKind? MarketOutcome = null,
    string? SelectedShopId = null);

public sealed record LivingWorldCommandResult(
    bool Completed,
    LivingWorldState ResultingState,
    LivingWorldEffect? Effect,
    LivingWorldRejectionReason RejectionReason)
{
    public static LivingWorldCommandResult Rejected(
        LivingWorldState state,
        LivingWorldRejectionReason reason) => new(false, state, null, reason);

    public static LivingWorldCommandResult Succeeded(
        LivingWorldState state,
        LivingWorldEffect effect) => new(true, state, effect, LivingWorldRejectionReason.None);
}
