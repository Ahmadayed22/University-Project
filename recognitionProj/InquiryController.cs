using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System.Collections.Generic;

[Route("api/inquiry")]
[ApiController]
public class InquiryController : ControllerBase
{
    private readonly DatabaseHandler _dbHandler;

    public InquiryController(DatabaseHandler dbHandler)
    {
        _dbHandler = dbHandler;
    }

    [HttpGet("public-institution-statuses")]
    public IActionResult GetPublicInstitutionStatuses()
    {
        List<PublicInstitutionStatus> statuses = _dbHandler.GetPublicInstitutionStatuses();
        return Ok(new { success = true, data = statuses });
    }
}
