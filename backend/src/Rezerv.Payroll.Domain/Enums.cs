namespace Rezerv.Payroll.Domain;

public enum ClassStatus
{
    Completed = 1,
    Cancelled = 2,
}

public enum BookingStatus
{
    Completed = 1,
    Cancelled = 2,
    NoShow = 3,
}

public enum SaleType
{
    Membership = 1,
    Package = 2,
}

/// <summary>
/// Studio-level configuration for how instructor payout is handled when a booked
/// participant does not attend a class.
/// </summary>
public enum NoShowPayoutPolicy
{
    /// <summary>Instructor still receives the per-booking payout for no-show bookings.</summary>
    PayInstructor = 1,

    /// <summary>Instructor only receives per-booking payout for attended bookings.</summary>
    NoPayout = 2,
}

public enum PayrollRunStatus
{
    Generated = 1,
}

/// <summary>
/// Every amount in a payroll entry is explained by one or more ledger line items of these types.
/// Zero-amount informational items (e.g. unpaid no-shows) are kept so auditors can see what
/// was considered but not paid.
/// </summary>
public enum PayrollLineItemType
{
    FixedClassFee = 1,
    BookingPayout = 2,
    NoShowBookingPayout = 3,
    NoShowBookingUnpaid = 4,
    CancelledClassNoPayout = 5,
    AttendanceBonus = 6,
    SalesCommission = 7,
    RefundAdjustment = 8,
    ManualAdjustment = 9,
}
