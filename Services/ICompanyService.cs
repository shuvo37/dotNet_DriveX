using DriveX.DTOs.Companies;

namespace DriveX.Services;

public interface ICompanyService
{
    Task<CompanyResultResponse> AddCompanyAsync(AddCompanyRequest request);
    Task<List<CompanyResponse>> GetActiveCompaniesAsync();
    Task<List<CompanyResponse>> GetCompaniesWithoutRentableCarsAsync();
    Task<CompanyResultResponse> UpdateCompanyAsync(long companyId, UpdateCompanyRequest request);
    Task<CompanyResultResponse> UpdateActiveStatusAsync(UpdateCompanyActiveStatusRequest request);
}
