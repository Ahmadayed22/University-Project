using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace recognitionProj
{
    [Route("api/faculty")]
    [ApiController]
    public class FacultyController : ControllerBase
    {
        private readonly DatabaseHandler _dbHandler; // Assuming you have a DB handler

        public FacultyController(DatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        /// <summary>
        /// Handles faculty registration with a file upload.
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SaveFacultyWithFile(
            [FromForm] string facultyJson,
            [FromForm] IFormFile programFile,           // Undergraduate program file
            [FromForm] IFormFile pgProgramFile)        // Postgraduate program file
        {
            // 1) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 2) Parse the JSON into Faculty object
            if (string.IsNullOrWhiteSpace(facultyJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for faculty." });
            }

            Faculty faculty;
            try
            {
                faculty = JsonConvert.DeserializeObject<Faculty>(facultyJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            // 3) Construct the base upload path (e.g., /wwwroot/uploads/{institutionId}/faculties/)
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,
                "faculties"
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // 4) Save Undergraduate Program File
            if (programFile != null && programFile.Length > 0)
            {
                var ugFileExtension = Path.GetExtension(programFile.FileName);
                if (!allowedExtensions.Contains(ugFileExtension))
                {
                    return BadRequest(new { success = false, message = "Invalid undergraduate file type. Only .xls, .xlsx, .csv allowed." });
                }

                string ugFilePath = Path.Combine(baseUploadPath, $"{faculty.FacultyName.Replace(" ", "_")}_Undergraduate{ugFileExtension}");
                using (var stream = new FileStream(ugFilePath, FileMode.Create))
                {
                    await programFile.CopyToAsync(stream);
                }
            }
            else
            {
                return BadRequest(new { success = false, message = "Undergraduate program file is required." });
            }

            // 5) Save Postgraduate Program File
            if (pgProgramFile != null && pgProgramFile.Length > 0)
            {
                var pgFileExtension = Path.GetExtension(pgProgramFile.FileName);
                if (!allowedExtensions.Contains(pgFileExtension))
                {
                    return BadRequest(new { success = false, message = "Invalid postgraduate file type. Only .xls, .xlsx, .csv allowed." });
                }

                string pgFilePath = Path.Combine(baseUploadPath, $"{faculty.FacultyName.Replace(" ", "_")}_Postgraduate{pgFileExtension}");
                using (var stream = new FileStream(pgFilePath, FileMode.Create))
                {
                    await pgProgramFile.CopyToAsync(stream);
                }
            }
            else
            {
                return BadRequest(new { success = false, message = "Postgraduate program file is required." });
            }

            // 6) Save the faculty details to the database (no file paths saved)
            _dbHandler.InsertFaculty(faculty);

            return Ok(new { success = true, message = "Faculty saved successfully", faculty });
        }
        [HttpDelete("delete")]
        public IActionResult DeleteFaculty([FromBody] Faculty faculty)
        {
            if (faculty == null || string.IsNullOrWhiteSpace(faculty.FacultyName) || faculty.InsID == 0)
            {
                Console.WriteLine("[DeleteFaculty] Invalid faculty data received.");
                return BadRequest(new { success = false, message = "Invalid faculty data." });
            }

            try
            {
                Console.WriteLine($"[DeleteFaculty] Received request to delete Faculty: {faculty.FacultyName}, InsID: {faculty.InsID}");

                // Ensure InsID is an integer (avoid type issues)
                if (!int.TryParse(faculty.InsID.ToString(), out int insID))
                {
                    Console.WriteLine("[DeleteFaculty] InsID is not a valid integer.");
                    return BadRequest(new { success = false, message = "Invalid Institution ID format." });
                }

                // 1) Prepare Parameters for Safe Deletion
                Dictionary<string, object> parameters = new Dictionary<string, object>
        {
            { "InsID", insID },
            { "FacultyName", faculty.FacultyName }
        };

                // Call the updated DeleteRecord method
                int rowsAffected = _dbHandler.DeleteRecord("Faculty", parameters);

                if (rowsAffected == 0)
                {
                    Console.WriteLine("[DeleteFaculty] No matching faculty record found for deletion.");
                    return NotFound(new { success = false, message = "No faculty record found for deletion." });
                }

                // 2) Delete Faculty Files (Undergraduate and Postgraduate) from File System
                string institutionId = insID.ToString();
                string baseUploadPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    institutionId,
                    "faculties"
                );

                // File name sanitization
                string sanitizedFacultyName = faculty.FacultyName.Replace(" ", "_");

                // Define file names for both undergraduate and postgraduate
                string[] fileTypes = { "Undergraduate", "Postgraduate" };
                string[] extensions = { ".xlsx", ".xls", ".csv" };

                foreach (var fileType in fileTypes)
                {
                    foreach (var ext in extensions)
                    {
                        string filePath = Path.Combine(baseUploadPath, $"{sanitizedFacultyName}_{fileType}{ext}");

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            Console.WriteLine($"[DeleteFaculty] Successfully deleted file: {filePath}");
                        }
                        else
                        {
                            Console.WriteLine($"[DeleteFaculty] File not found: {filePath}");
                        }
                    }
                }

                Console.WriteLine($"[DeleteFaculty] Successfully deleted Faculty: {faculty.FacultyName}");
                return Ok(new { success = true, message = "Faculty and associated files deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteFaculty] Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error deleting faculty.", error = ex.Message });
            }
        }


        [HttpGet("getAll/{insId}")]
        public IActionResult GetAllFaculties(int insId)
        {
            if (insId == 0)
            {
                return BadRequest(new { success = false, message = "Invalid Institution ID." });
            }

            try
            {
                List<Faculty> faculties = _dbHandler.GetAllFacultiesByInsID(insId);

                if (faculties.Count == 0)
                {
                    return NotFound(new { success = false, message = "No faculties found." });
                }

                // Correctly construct file paths including institution ID
                var facultyListWithFiles = faculties.Select(faculty => new
                {
                    faculty,
                    ProgramFilePath = GetFilePath(insId, faculty.FacultyName, "Undergraduate"),
                    PgProgramFilePath = GetFilePath(insId, faculty.FacultyName, "Postgraduate")
                }).ToList();

                return Ok(facultyListWithFiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching faculties and files.", error = ex.Message });
            }
        }

        // Helper function to generate file paths
        private string GetFilePath(int insId, string facultyName, string programType)
        {
            string sanitizedFacultyName = facultyName.Replace(" ", "_");
            string[] extensions = { ".xlsx", ".xls", ".csv" };

            foreach (var ext in extensions)
            {
                string relativePath = Path.Combine("uploads", insId.ToString(), "faculties", $"{sanitizedFacultyName}_{programType}{ext}");
                string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                if (System.IO.File.Exists(absolutePath))
                {
                    return relativePath.Replace("\\", "/");  // Ensure web-compatible slashes
                }
            }

            return "Not Available";
        }





    }
}
