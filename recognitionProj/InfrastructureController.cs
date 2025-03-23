using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using recognitionProj;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

[Route("api/infrastructure")]
[ApiController]
public class InfrastructureController : ControllerBase
{
    private readonly DatabaseHandler _dbHandler;
    private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB Limit

    public InfrastructureController(DatabaseHandler dbHandler)
    {
        _dbHandler = dbHandler;
    }
    // ✅ This is the root folder where your uploads are stored.
    //    Adjust this path to match your environment, e.g. "C:\\inetpub\\wwwroot\\uploads"
    private readonly string _baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
    [HttpPost("save")]
    public async Task<IActionResult> SaveInfrastructureInfo(
        [FromForm] string infrastructureJson,
        [FromForm] IFormFile AreaFile,
        [FromForm] IFormFile LabsFile)
    {
        // 1) Read InstitutionID from Headers
        var institutionId = Request.Headers["InstitutionID"].ToString();
        if (string.IsNullOrEmpty(institutionId))
        {
            return BadRequest(new { success = false, message = "Institution ID is required." });
        }

        // 2) Parse JSON
        if (string.IsNullOrWhiteSpace(infrastructureJson))
        {
            return BadRequest(new { success = false, message = "Missing JSON data for infrastructure." });
        }

        Infrastructure infrastructure;
        try
        {
            infrastructure = JsonConvert.DeserializeObject<Infrastructure>(infrastructureJson);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
        }

        // 3) Construct Upload Path (wwwroot/uploads/{InstitutionID}/infrastructure)
        string baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", institutionId, "infrastructure");
        Directory.CreateDirectory(baseUploadPath);

        // Allowed file types
        var areaAllowedExtensions = new HashSet<string> { ".pdf", ".jpg", ".png" };
        var labsAllowedExtensions = new HashSet<string> { ".xls", ".xlsx", ".csv" };

        // Helper method to validate & save a file with a unique name
        async Task<string> ValidateAndSaveFileAsync(IFormFile file, string filePrefix, HashSet<string> allowedExtensions)
        {
            if (file != null && file.Length > 0)
            {
                if (file.Length > MAX_FILE_SIZE) return "SIZE_EXCEEDED";

                var fileExtension = Path.GetExtension(file.FileName);
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return "INVALID_TYPE";
                }

                // Generate unique filename
                string uniqueFileName = $"{filePrefix}{fileExtension}";
                string filePath = Path.Combine(baseUploadPath, uniqueFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                return filePath;
            }
            return null;
        }

        // 4) Validate and save files
        string areaFilePath = await ValidateAndSaveFileAsync(AreaFile, "Area", areaAllowedExtensions);
        string labsFilePath = await ValidateAndSaveFileAsync(LabsFile, "Labs", labsAllowedExtensions);

        if (areaFilePath == "SIZE_EXCEEDED" || labsFilePath == "SIZE_EXCEEDED")
        {
            return BadRequest(new { success = false, message = "File size exceeds the 5MB limit." });
        }

        if (areaFilePath == "INVALID_TYPE")
        {
            return BadRequest(new { success = false, message = "Invalid AreaFile type (.pdf, .jpg, .png only)." });
        }

        if (labsFilePath == "INVALID_TYPE")
        {
            return BadRequest(new { success = false, message = "Invalid LabsFile type (.xls, .xlsx only)." });
        }

        // 5) Save infrastructure data in the database (excluding file paths)
        _dbHandler.InsertInfrastructure(infrastructure);

        return Ok(new
        {
            success = true,
            message = "Infrastructure information saved successfully.",
            files = new
            {
                AreaFile = areaFilePath != null ? "Saved Successfully" : "Not Provided",
                LabsFile = labsFilePath != null ? "Saved Successfully" : "Not Provided"
            }
        });
    }
    [HttpGet("get/{insId}")]
    public IActionResult GetInfrastructureData(int insId)
    {
        if (insId == 0)
        {
            return BadRequest(new { success = false, message = "Invalid Institution ID." });
        }

        try
        {
            // ✅ Fetch Infrastructure Data from DB
            Infrastructure infrastructure = _dbHandler.GetInfrastructureByInsID(insId);
            if (infrastructure == null)
            {
                return NotFound(new { success = false, message = "No infrastructure data found." });
            }

            // ✅ Construct File Paths
            string baseUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", insId.ToString(), "infrastructure");
            string areaFilePath = Path.Combine(baseUploadPath, "Area.pdf");
            string labsFilePath = Path.Combine(baseUploadPath, "Labs.xlsx");

            // ✅ Convert Files to Base64 (if they exist)
            string areaFileBase64 = System.IO.File.Exists(areaFilePath) ? Convert.ToBase64String(System.IO.File.ReadAllBytes(areaFilePath)) : null;
            string labsFileBase64 = System.IO.File.Exists(labsFilePath) ? Convert.ToBase64String(System.IO.File.ReadAllBytes(labsFilePath)) : null;

            return Ok(new
            {
                success = true,
                data = new
                {
                    insID = infrastructure.InsID,
                    area = infrastructure.Area,
                    sites = infrastructure.Sites,
                    terr = infrastructure.Terr,
                    halls = infrastructure.Halls,
                    library = infrastructure.Library,
                    areaFile = areaFileBase64,  // ✅ Sending actual file content
                    labsFile = labsFileBase64   // ✅ Sending actual file content
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error fetching infrastructure data.", error = ex.Message });
        }
    }
    [HttpDelete("delete")]
    public IActionResult DeleteInfrastructureFile([FromHeader] string InstitutionID, [FromQuery] string fileType)
    {
        // 1) Validate
        if (string.IsNullOrWhiteSpace(InstitutionID))
        {
            return BadRequest(new { success = false, message = "Institution ID is required." });
        }
        if (string.IsNullOrWhiteSpace(fileType))
        {
            return BadRequest(new { success = false, message = "File Type is required (e.g. 'area' or 'labs')." });
        }

        try
        {
            // 2) Build folder path
            string infraDir = Path.Combine(_baseUploadPath, InstitutionID, "infrastructure");

            // 3) Determine file name
            // e.g., "Area.pdf" or "Labs.xlsx"
            string fileName = fileType.Equals("area", StringComparison.OrdinalIgnoreCase) ? "Area.pdf"
                           : fileType.Equals("labs", StringComparison.OrdinalIgnoreCase) ? "Labs.xlsx"
                           : null;

            if (fileName == null)
            {
                return BadRequest(new { success = false, message = "Invalid fileType parameter ('area' or 'labs')." });
            }

            // Combine full path
            string fullPath = Path.Combine(infraDir, fileName);

            // 4) Check & delete
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.WriteLine($"[DeleteInfrastructureFile] File not found: {fullPath}");
                return NotFound(new { success = false, message = "File not found." });
            }

            System.IO.File.Delete(fullPath);
            Debug.WriteLine($"[DeleteInfrastructureFile] Deleted file: {fullPath}");

            return Ok(new
            {
                success = true,
                message = $"'{fileType}' file deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeleteInfrastructureFile] Error: {ex.Message}");
            return StatusCode(500, new
            {
                success = false,
                message = "Error deleting file.",
                error = ex.Message
            });
        }
    }


}
