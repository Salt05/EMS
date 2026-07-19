namespace EMS.Core.Entities.Enums;

public enum RegistrationStatus
{
    Pending = 1,
    Confirmed = 2,
    Approved = 2, // Alias for Confirmed (used in MVC Student Portal)
    Waitlisted = 3,
    Cancelled = 4,
    Rejected = 5,
    PendingPayment = 6
}
