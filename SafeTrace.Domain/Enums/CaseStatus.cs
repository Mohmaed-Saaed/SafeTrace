namespace SafeTrace.Domain.Enums
{
    public enum CaseStatus
    {
        Pending, // Waiting for approval
        Active, // Case is active and being worked on
        Closed, // Case is closed and deleted by the user as long as case is not found
        Found, // Case is found and closed by the user
        Rejected, // Case is rejected by the admin
        Expired // Case is expired for urgent cases that are not found within 48 hours
    }
}
