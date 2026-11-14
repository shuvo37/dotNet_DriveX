using DriveX.DTOs.Companies;
using DriveX.Exceptions;
using DriveX.Mapping;
using DriveX.Models;
using DriveX.Repositories;

namespace DriveX.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companies;

    public CompanyService(ICompanyRepository companies)
    {
        _companies = companies;
    }

    public async Task<CompanyResultResponse> AddCompanyAsync(AddCompanyRequest request)
    {
        var name = request.CompanyName.Trim();
        var email = request.CompanyEmail.Trim();

        if (await _companies.ExistsByEmailAsync(email))
        {
            throw new AppException("Email already exists");
        }

        if (await _companies.ExistsByNameAsync(name))
        {
            throw new AppException("Company name already exists");
        }

        var company = new Company
        {
            CompanyName = name,
            CompanyEmail = email,
            CompanyPhoneNumber = request.CompanyPhoneNumber.Trim(),
            CompanyCity = request.CompanyCity.Trim(),
            CompanyAddress = request.CompanyAddress.Trim(),
            IsActive = true
        };

        // The unique indexes on name and email also protect against two saves at the same moment.
        if (!await _companies.AddAsync(company))
        {
            throw new AppException("Company name or email already exists");
        }

        return new CompanyResultResponse("Successfully added new company", company.ToResponse());
    }

    // Customers only see active companies (same as Spring).
    public async Task<List<CompanyResponse>> GetActiveCompaniesAsync()
    {
        var companies = await _companies.GetActiveAsync();

        return companies.Select(x => x.ToResponse()).ToList();
    }

    // Companies that have no car that is RENTED or AVAILABLE (so every car is in maintenance,
    // or the company has no cars). Active or not. The admin "company status" page lists these.
    public async Task<List<CompanyResponse>> GetCompaniesWithoutRentableCarsAsync()
    {
        var companies = await _companies.GetWithoutRentableCarsAsync();

        return companies.Select(x => x.ToResponse()).ToList();
    }

    public async Task<CompanyResultResponse> UpdateCompanyAsync(long companyId, UpdateCompanyRequest request)
    {
        var company = await _companies.GetByIdAsync(companyId)
            ?? throw new AppException("Company not found");

        var name = request.CompanyName.Trim();
        var email = request.CompanyEmail.Trim();

        // "!= companyId" so a company can keep its own name and email.
        if (await _companies.ExistsByNameAsync(name, companyId))
        {
            throw new AppException("Company name already exists");
        }

        if (await _companies.ExistsByEmailAsync(email, companyId))
        {
            throw new AppException("Company email already exists");
        }

        company.CompanyName = name;
        company.CompanyEmail = email;
        company.CompanyPhoneNumber = request.CompanyPhoneNumber.Trim();
        company.CompanyCity = request.CompanyCity.Trim();
        company.CompanyAddress = request.CompanyAddress.Trim();

        if (!await _companies.UpdateAsync(company))
        {
            throw new AppException("Company name or email already exists");
        }

        return new CompanyResultResponse("Successfully updated company", company.ToResponse());
    }

    // The React page identifies the company by name, as it did with Spring.
    public async Task<CompanyResultResponse> UpdateActiveStatusAsync(UpdateCompanyActiveStatusRequest request)
    {
        var name = request.CompanyName.Trim();

        var company = await _companies.GetByNameAsync(name)
            ?? throw new AppException("Company not found");

        company.IsActive = request.IsActive!.Value;

        if (!await _companies.UpdateAsync(company))
        {
            throw new AppException("Company name or email already exists");
        }

        return new CompanyResultResponse("Successfully updated company", company.ToResponse());
    }
}
