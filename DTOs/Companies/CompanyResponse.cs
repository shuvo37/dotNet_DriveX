namespace DriveX.DTOs.Companies;

// What the API returns for a company. It has exactly the fields the React pages read,
// and never the Company entity itself.
public record CompanyResponse(
    long CompanyId,
    string CompanyName,
    string CompanyEmail,
    string CompanyPhoneNumber,
    string CompanyCity,
    string CompanyAddress,
    bool IsActive);
