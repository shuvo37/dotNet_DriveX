namespace DriveX.DTOs.Companies;

// Returned after add, update and active-status change: { "message": "...", "company": { ... } }
public record CompanyResultResponse(string Message, CompanyResponse Company);
