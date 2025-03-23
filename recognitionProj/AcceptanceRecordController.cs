using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System.Collections.Generic;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AcceptanceRecordController : ControllerBase
    {
        private readonly DatabaseHandler _dbHandler;
        
        public AcceptanceRecordController(DatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        // POST: api/acceptancerecord/save
        [HttpPost("save")]
        public IActionResult SaveAcceptanceRecord([FromBody] AcceptanceRecord acceptanceRecord)
        {
            if (acceptanceRecord == null)
            {
                return BadRequest(new { success = false, message = "Invalid acceptance record data." });
            }

            _dbHandler.InsertAcceptanceRecord(acceptanceRecord);

            // Return success response
            return Ok(new { success = true, message = "Acceptance record saved successfully." });
        }

        // GET: api/acceptancerecord/get-all
        [HttpGet("get-all")]
        public IActionResult GetAllAcceptanceRecords()
        {
           var a= _dbHandler.GetAllAcceptanceRecords();
            return Ok(a);
        }
        // GET: api/acceptancerecord/get-by-insid/123
        [HttpGet("get-by-insid/{insId}")]
        public IActionResult GetRecordsByInsId(int insId)
        {
            // 1) Query DB for all records associated with this InsID
            var records = _dbHandler.GetAllAcceptanceRecordsByInsId(insId);

            // 2) Return the list as JSON (with a success flag or any structure you like)
            return Ok(new { success = true, data = records });
        }
        // GET: api/institutions
        [HttpGet("/api/institutions")]
        public IActionResult GetAllInstitutions()
        {
            var institutions = _dbHandler.GetAllPublicInfo(); // Fetch all institutions
            var data = institutions.Select(inst => new
            {
                InsID = inst.InsID,
                InsName = inst.InsName
            }).ToList();

            return Ok(new { success = true, data = data });
        }

    }
}
