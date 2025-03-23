using Microsoft.AspNetCore.Mvc;
using System;
using recognitionProj; // For StudyDuration, DatabaseHandler, etc.

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudyDurationController : ControllerBase
    {
        private readonly DatabaseHandler _dbHandler;

        public StudyDurationController(DatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        // POST: api/studyduration/save
        [HttpPost("save")]
        public IActionResult SaveStudyDuration([FromBody] StudyDuration studyDuration)
        {
            Console.WriteLine("[StudyDurationController] SaveStudyDuration called.");

            if (studyDuration == null)
            {
                Console.WriteLine("[StudyDurationController] studyDuration is null.");
                return BadRequest(new { success = false, message = "Invalid study duration data." });
            }

            try
            {
                Console.WriteLine("[StudyDurationController] Inserting StudyDuration into DB for InsID=" + studyDuration.InsID);
                _dbHandler.InsertStudyDuration(studyDuration);

                return Ok(new { success = true, message = "Study duration saved successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StudyDurationController] Exception: " + ex.ToString());
                return StatusCode(500, new { success = false, message = "DB Error: " + ex.Message });
            }

        }

        // GET: api/studyduration/get-all
        [HttpGet("get-all")]
        public IActionResult GetAllStudyDurations()
        {
            Console.WriteLine("[StudyDurationController] GetAllStudyDurations called.");
            try
            {
                var list = _dbHandler.GetAllStudyDurations();
                return Ok(list);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StudyDurationController] Exception in GetAll: " + ex.Message);
                return StatusCode(500, new { success = false, message = "DB Error: " + ex.Message });
            }
        }

        // GET: api/studyduration/get-by-insid/
        
        [HttpGet("get-by-insid/{insID}")]
        public IActionResult GetStudyDurationByInsID(int insID)
        {
            Console.WriteLine("[StudyDurationController] GetStudyDurationByInsID called with insID=" + insID);
            try
            {
                var studyDuration = _dbHandler.GetStudyDurationByInsID(insID);
                if (studyDuration == null)
                {
                    return NotFound(new { success = false, message = "Study duration not found for InsID=" + insID });
                }
                return Ok(studyDuration);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StudyDurationController] Exception in GetByInsID: " + ex.Message);
                return StatusCode(500, new { success = false, message = "DB Error: " + ex.Message });
            }
        }
    }
}
