using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

[Route("api/admission")]
[ApiController]
public class AdmissionController : ControllerBase
{
    private readonly string _baseUploadPath = "wwwroot/uploads"; // Root upload folder

    [HttpPost("upload")]
    public async Task<IActionResult> UploadAdmissionFile(
        [FromForm] string degreeType,
        [FromForm] IFormFile admissionFile)
    {
        // 1) Validate Institution ID
        var institutionId = Request.Headers["InstitutionID"].ToString();
        if (string.IsNullOrEmpty(institutionId))
        {
            return BadRequest(new { success = false, message = "Institution ID is required." });
        }

        // 2) Validate File
        if (admissionFile == null || admissionFile.Length == 0)
        {
            return BadRequest(new { success = false, message = "File is required." });
        }

        // Allowed file extensions
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf"};
        var fileExtension = Path.GetExtension(admissionFile.FileName);
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new { success = false, message = "Invalid file type. Only .pdf allowed." });
        }

        try
        {
            // 3) Create Upload Path
            string uploadPath = Path.Combine(_baseUploadPath, institutionId, "admission-requirements");
            Directory.CreateDirectory(uploadPath); // Ensure directory exists

            // 4) Construct File Name
            string sanitizedDegreeType = degreeType.Replace(" ", "_");
            string fileName = $"{sanitizedDegreeType}_Admission{fileExtension}";
            string filePath = Path.Combine(uploadPath, fileName);

            // 5) Save File
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await admissionFile.CopyToAsync(stream);
            }

            Debug.WriteLine($"[UploadAdmissionFile] Successfully uploaded: {fileName}");
            return Ok(new { success = true, message = "File uploaded successfully!", filePath });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UploadAdmissionFile] Error: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while uploading the file." });
        }
    }
    [HttpGet("getAll/{institutionId}")]
    public IActionResult GetAllFiles(string institutionId)
    {
        if (string.IsNullOrEmpty(institutionId))
        {
            return BadRequest(new { success = false, message = "Institution ID is required." });
        }

        string directoryPath = Path.Combine(_baseUploadPath, institutionId, "admission-requirements");
        if (!Directory.Exists(directoryPath))
        {
            return Ok(new { success = true, files = new List<object>() }); // Ensure it's always an array
        }

        var files = Directory.GetFiles(directoryPath)
                             .Select(filePath => new
                             {
                                 FileName = Path.GetFileName(filePath) ?? "Unknown_File",
                                 FileUrl = $"/uploads/{institutionId}/admission-requirements/{Path.GetFileName(filePath)}"
                             })
                             .ToList();

        Debug.WriteLine($"[GetAllFiles] Returning {files.Count} files: {string.Join(", ", files.Select(f => f.FileName))}");

        return Ok(new { success = true, files });
    }
    [HttpDelete("delete/{institutionId}/{fileName}")]
    public IActionResult DeleteAdmissionFile(string institutionId, string fileName)
    {
        // Validate inputs
        if (string.IsNullOrEmpty(institutionId) || string.IsNullOrEmpty(fileName))
        {
            return BadRequest(new { success = false, message = "Institution ID and File Name are required." });
        }

        try
        {
            // Construct the exact file path used when uploading
            string filePath = Path.Combine(_baseUploadPath, institutionId, "admission-requirements", fileName);

            // Check if file exists
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath); // Delete the file
                Debug.WriteLine($"[DeleteAdmissionFile] Deleted file: {filePath}");

                return Ok(new
                {
                    success = true,
                    message = "File deleted successfully!"
                });
            }
            else
            {
                Debug.WriteLine($"[DeleteAdmissionFile] File not found: {filePath}");
                return NotFound(new
                {
                    success = false,
                    message = "File not found."
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DeleteAdmissionFile] Error: {ex.Message}");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while deleting the file.",
                error = ex.Message
            });
        }
    }


}
