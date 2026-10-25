using DriveX.Data;
using DriveX.DTOs.Companies;
using DriveX.Enums;
using DriveX.Exceptions;
using DriveX.Mapping;
using DriveX.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveX.Services;

public class CompanyService : ICompanyService
{
    private readonly DriveXDbContext _db;

    public CompanyService(DriveXDbContext db)
    {
        _db = db;
    }

    public async Task<CompanyResultResponse> AddCompanyAsync(AddCompanyRequest request)
    {
        var name = request.CompanyName.Trim();
        var email = request.CompanyEmail.Trim();

        if (await _db.Companies.AnyAsync(c => c.CompanyEmail == email))
        {
            throw new AppException("Email already exists");
        }

        if (await _db.Companies.AnyAsync(c => c.CompanyName == name))
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

        _db.Companies.Add(company);
        await SaveAsync();

        return new CompanyResultResponse("Successfully added new company", company.ToResponse());
    }

    // Customers only see active companies (same as Spring).
    public async Task<List<CompanyResponse>> GetActiveCompaniesAsync()
    {
        var companies = await _db.Companies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyId)
            .ToListAsync();

        return companies.Select(x => x.ToResponse()).ToList();
    }

    // Companies that have no car that is RENTED or AVAILABLE (so every car is in maintenance,
    // or the company has no cars). Active or not. The admin "company status" page lists these.
    public async Task<List<CompanyResponse>> GetCompaniesWithoutRentableCarsAsync()
    {
        var companies = await _db.Companies
            .AsNoTracking()
            .Where(c => !_db.Cars.Any(car =>
                car.CompanyId == c.CompanyId &&
                (car.RentalStatus == RentalStatus.RENTED || car.RentalStatus == RentalStatus.AVAILABLE)))
            .OrderBy(c => c.CompanyId)
            .ToListAsync();

        return companies.Select(x => x.ToResponse()).ToList();
    }

    public async Task<CompanyResultResponse> UpdateCompanyAsync(long companyId, UpdateCompanyRequest request)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId)
            ?? throw new AppException("Company not found");

        var name = request.CompanyName.Trim();
        var email = request.CompanyEmail.Trim();

        // "!= companyId" so a company can keep its own name and email.
        if (await _db.Companies.AnyAsync(c => c.CompanyName == name && c.CompanyId != companyId))
        {
            throw new AppException("Company name already exists");
        }

        if (await _db.Companies.AnyAsync(c => c.CompanyEmail == email && c.CompanyId != companyId))
        {
            throw new AppException("Company email already exists");
        }

        company.CompanyName = name;
        company.CompanyEmail = email;
        company.CompanyPhoneNumber = request.CompanyPhoneNumber.Trim();
        company.CompanyCity = request.CompanyCity.Trim();
        company.CompanyAddress = request.CompanyAddress.Trim();

        await SaveAsync();

        return new CompanyResultResponse("Successfully updated company", company.ToResponse());
    }

    // The React page identifies the company by name, as it did with Spring.
    public async Task<CompanyResultResponse> UpdateActiveStatusAsync(UpdateCompanyActiveStatusRequest request)
    {
        var name = request.CompanyName.Trim();

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyName == name)
            ?? throw new AppException("Company not found");

        company.IsActive = request.IsActive!.Value;
        await _db.SaveChangesAsync();

        return new CompanyResultResponse("Successfully updated company", company.ToResponse());
    }

    // The database has unique indexes on name and email. If two admins save the same name at the
    // same moment, the second save fails here and becomes a normal 400 message instead of a 500.
    private async Task SaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new AppException("Company name or email already exists");
        }
    }
}
