namespace Rezerv.Payroll.Domain.Entities;

public class Instructor
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public Studio Studio { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<StudioClass> Classes { get; set; } = new List<StudioClass>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<ManualAdjustment> ManualAdjustments { get; set; } = new List<ManualAdjustment>();
}
