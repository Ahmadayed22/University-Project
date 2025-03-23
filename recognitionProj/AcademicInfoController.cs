using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System.Collections.Generic;
using System.Text.Json;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AcademicInfoController : ControllerBase
    {
        private readonly DatabaseHandler _dbHandler;
        // In-memory list to store library information temporarily (for demonstration purposes)
        private readonly ILogger<AcademicInfoController> _logger;

        public AcademicInfoController(DatabaseHandler dbHandler, ILogger<AcademicInfoController> logger)
        {
            _dbHandler = dbHandler;
            _logger = logger;
        }

        // POST: api/academicinfo/save
        [HttpPost("save")]                                               
        public IActionResult SaveAcademicInfo([FromBody] AcademicInfo academicInfo)
        {
            _logger.LogInformation("Received AcademicInfo: {AcademicInfo}", JsonSerializer.Serialize(academicInfo));
            if (academicInfo == null)
            {
                return BadRequest(new { success = false, message = "Invalid academic information data." });
            }
            try
            {
                _dbHandler.InsertAcademicInfo(academicInfo);
                var spec = academicInfo.Speciality;
                var id = academicInfo.InsID;
                _dbHandler.UpdateUserSpeciality(id, spec);
                return Ok(new { success = true, message = "Academic information saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving academic information. Input: {AcademicInfo}", JsonSerializer.Serialize(academicInfo));
                return StatusCode(500, new { success = false, message = "An error occurred while saving academic information." });
            }

        }
        // GET: api/academicinfo/getbyid/{id}
        [HttpGet("getbyid/{id}")]
        public IActionResult GetAcademicInfo(int id)
        {
            var data = _dbHandler.GetAcademicInfoById(id);
            if (data == null)
            {
                return NotFound(new { success = false, message = $"No AcademicInfo found for InsID = {id}." });
            }
            return Ok(new { success = true, data = data });
        }

    }


}
