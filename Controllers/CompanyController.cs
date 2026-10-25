using DriveX.DTOs.Companies;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/company")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResultResponse>> AddCompany(AddCompanyRequest request)
    {
        var response = await _companyService.AddCompanyAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // Public: customers browse companies. Only active ones are returned.
    [HttpGet]
    public async Task<ActionResult<List<CompanyResponse>>> GetAllCompanies()
    {
        return Ok(await _companyService.GetActiveCompaniesAsync());
    }

    // The spelling of the URL is kept because the React page calls exactly this address.
    [HttpGet("compayThatAllCarInMaintainance")]
    public async Task<ActionResult<List<CompanyResponse>>> GetCompaniesWithoutRentableCars()
    {
        return Ok(await _companyService.GetCompaniesWithoutRentableCarsAsync());
    }

    [HttpPut("{companyId:long}")]
    public async Task<ActionResult<CompanyResultResponse>> UpdateCompany(long companyId, UpdateCompanyRequest request)
    {
        return Ok(await _companyService.UpdateCompanyAsync(companyId, request));
    }

    [HttpPatch("company-active-status-update")]
    public async Task<ActionResult<CompanyResultResponse>> UpdateCompanyActiveStatus(UpdateCompanyActiveStatusRequest request)
    {
        return Ok(await _companyService.UpdateActiveStatusAsync(request));
    }
}
