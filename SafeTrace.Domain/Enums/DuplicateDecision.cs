namespace SafeTrace.Domain.Enums
{
    public enum DuplicateDecision
    {
        None,
        SameUserPending,
        SameUserActive,
        PendingDuplicate,
        ApprovedDuplicate,
        AllowUnknown
    }
}
