namespace SafeTrace.Domain.Enums
{
    public enum DuplicateDecision
    {
        None,
        SameUserDuplicate,
        PendingOwnerCase,
        PendingUnknownCase,
        ActiveOwnerCase,
        ActiveUnknownCase
    }
}
