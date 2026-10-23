namespace DriveX.Models;

// Maps to the existing "companies" table.
public class Company
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyPhoneNumber { get; set; } = string.Empty;
    public string CompanyCity { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<Car> Cars { get; set; } = new List<Car>();
}
