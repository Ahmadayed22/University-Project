using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System.Collections.Generic;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicInfoController : ControllerBase
    {
        // In-memory list to store public information temporarily (for demonstration purposes)
        private readonly DatabaseHandler _dbHandler;
        public PublicInfoController(DatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        // POST: api/publicinfo/save
        [HttpPost("save")]
        public IActionResult SavePublicInfo([FromBody] PublicInfo publicInfo)
        {
            if (publicInfo == null)
            {
                return BadRequest(new { success = false, message = "Invalid public information data." });
            }
            publicInfo.Supervisor = "Not Assigned"; // Default supervisor name
            publicInfo.SupervisorID = null; // Default supervisor ID


            _dbHandler.InsertPublicInfo(publicInfo);

            // Return success response
            return Ok(new { success = true, message = "Public information saved successfully." });
        }

        // GET: api/publicinfo/getbyid/{id}
        [HttpGet("getbyid/{id}")]
        public IActionResult GetPublicInfo(int id)
        {
            var result = _dbHandler.GetPublicInfoById(id);
            //purge supervisorID and supervisor from the result
           
            if (result == null)
            {
                return NotFound(new { success = false, message = $"No PublicInfo found for InsID {id}." });
            }
            result.SupervisorID = 0;
            result.Supervisor = "###";
            // Return JSON with "data" => your PublicInfo object
            return Ok(new { success = true, data = result });
        }

    }
}
