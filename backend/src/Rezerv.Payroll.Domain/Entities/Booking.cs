namespace Rezerv.Payroll.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public StudioClass Class { get; set; } = null!;

    public string CustomerName { get; set; } = string.Empty;
    public BookingStatus Status { get; set; } = BookingStatus.Completed;
}
