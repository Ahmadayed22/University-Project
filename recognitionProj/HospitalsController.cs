using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace recognitionProj
{
    [Route("api/hospitals")]
    [ApiController]
    public class HospitalController : ControllerBase
    {
        private readonly DatabaseHandler _dbHandler; // Database handler

        public HospitalController(DatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        /// <summary>
        /// Handles hospital registration with a file upload.
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SaveHospitalWithFile(
      [FromForm] string hospitalJson,
      [FromForm] IFormFile? trainingFile) // Allow trainingFile to be null
        {
            // 1) Read Institution ID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 2) Parse the JSON into Hospital object
            if (string.IsNullOrWhiteSpace(hospitalJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for hospital." });
            }

            Hospitals hospital;
            try
            {
                hospital = JsonConvert.DeserializeObject<Hospitals>(hospitalJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            // Ensure HospMajor is not null to avoid NullReferenceException
            if (string.IsNullOrWhiteSpace(hospital.HospMajor))
            {
                return BadRequest(new { success = false, message = "Hospital affiliation (HospMajor) is required." });
            }

            // 3) Construct the base upload path (e.g., /wwwroot/uploads/{institutionId}/hospitals/)
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,
                "hospitals"
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file type
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };

                // For Public and Private affiliations, the file is required
                if (trainingFile == null || trainingFile.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Training contacts file is required for Public and Private hospitals." });
                }

                var fileExtension = Path.GetExtension(trainingFile.FileName);
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { success = false, message = "Invalid file type. Only .pdf allowed." });
                }

                // Save the file with the renamed Hospital Name
                string sanitizedHospitalName = hospital.HospName.Replace(" ", "_");
                string filePath = Path.Combine(baseUploadPath, $"{sanitizedHospitalName}{fileExtension}");

                try
                {
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await trainingFile.CopyToAsync(stream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SaveHospitalWithFile] Error saving file: {ex.Message}");
                    return StatusCode(500, new { success = false, message = "Error saving the training contacts file.", error = ex.Message });
                }
            
           
            // 5) Save the hospital details to the database
            try
            {
                _dbHandler.InsertHospital(hospital);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveHospitalWithFile] Error inserting hospital into database: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error saving hospital to the database.", error = ex.Message });
            }

            return Ok(new { success = true, message = "Hospital saved successfully", hospital });
        }




        /// <summary>
        /// Deletes a hospital and its associated file.
        /// </summary>
        [HttpDelete("delete")]
        public IActionResult DeleteHospital([FromBody] Hospitals hospital)
        {
            if (hospital == null || string.IsNullOrWhiteSpace(hospital.HospName) ||
                string.IsNullOrWhiteSpace(hospital.HospType) || string.IsNullOrWhiteSpace(hospital.HospMajor) || hospital.InsID == 0)
            {
                Console.WriteLine("[DeleteHospital] Invalid hospital data received.");
                return BadRequest(new { success = false, message = "Invalid hospital data." });
            }

            try
            {
                string hospNameTrimmed = hospital.HospName.Trim();
                string hospTypeTrimmed = hospital.HospType.Trim();
                string hospMajorTrimmed = hospital.HospMajor.Trim();

                Console.WriteLine($"[DeleteHospital] Attempting to delete Hospital: '{hospNameTrimmed}', Type: '{hospTypeTrimmed}', Major: '{hospMajorTrimmed}', InsID: {hospital.InsID}");

                // Ensure InsID is an integer
                if (!int.TryParse(hospital.InsID.ToString(), out int insID))
                {
                    Console.WriteLine("[DeleteHospital] InsID is not a valid integer.");
                    return BadRequest(new { success = false, message = "Invalid Institution ID format." });
                }

                // 1) Prepare Parameters for Safe Deletion
                Dictionary<string, object> parameters = new Dictionary<string, object>
        {
            { "InsID", insID },
            { "HospName", hospNameTrimmed },
            { "HospType", hospTypeTrimmed },
            { "HospMajor", hospMajorTrimmed }
        };

                // Call the updated DeleteRecord method
                int rowsAffected = _dbHandler.DeleteRecord("Hospitals", parameters);

                if (rowsAffected == 0)
                {
                    Console.WriteLine("[DeleteHospital] No matching hospital record found for deletion.");
                    return NotFound(new { success = false, message = "No hospital record found for deletion." });
                }

                // 2) Delete Hospital File from File System
                string institutionId = insID.ToString();
                string baseUploadPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    institutionId,
                    "hospitals"
                );

                string hospitalFilePath = Path.Combine(baseUploadPath, $"{hospNameTrimmed.Replace(" ", "_")}.pdf");

                if (System.IO.File.Exists(hospitalFilePath))
                {
                    System.IO.File.Delete(hospitalFilePath);
                    Console.WriteLine($"[DeleteHospital] Successfully deleted file: {hospitalFilePath}");
                }
                else
                {
                    Console.WriteLine($"[DeleteHospital] File not found: {hospitalFilePath}");
                }

                Console.WriteLine($"[DeleteHospital] Successfully deleted Hospital: {hospNameTrimmed}");
                return Ok(new { success = true, message = "Hospital deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteHospital] Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error deleting hospital.", error = ex.Message });
            }
        }



        /// <summary>
        /// Retrieves all hospitals for a given institution.
        /// </summary>
        [HttpGet("getAll/{insId}")]
        public IActionResult GetAllHospitals(int insId)
        {
            if (insId == 0)
            {
                return BadRequest(new { success = false, message = "Invalid Institution ID." });
            }

            try
            {
                List<Hospitals> hospitals = _dbHandler.GetAllHospitalsByInsID(insId);

                if (hospitals.Count == 0)
                {
                    return NotFound(new { success = false, message = "No hospitals found." });
                }

                // Construct file paths for each hospital
                string basePath = Path.Combine("uploads", insId.ToString(), "hospitals");
                var hospitalListWithFiles = hospitals.Select(hospital => new
                {
                    hospital,
                    FilePath = Path.Combine(basePath, $"{hospital.HospName.Replace(" ", "_")}.pdf")
                }).ToList();

                return Ok(hospitalListWithFiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching hospitals and files.", error = ex.Message });
            }
        }
    }
}
