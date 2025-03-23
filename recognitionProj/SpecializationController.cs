using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json; // For JSON deserialization
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using recognitionProj;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecializationController : ControllerBase
    {
        private static List<Specialization> _specializationList = new List<Specialization>();
        public List<Specialization> SpecializationList { get => _specializationList; set => _specializationList = value; }

        private readonly Verifier _verifier;
        private readonly DatabaseHandler _dbHandler;

        // Combine dependencies into a single constructor
        public SpecializationController(Verifier verifier, DatabaseHandler dbHandler)
        {
            _verifier = verifier;
            _dbHandler = dbHandler;
        }
        [HttpGet("get-specialization/{insId}/{type}")]
        public IActionResult GetSpecializationWithFiles(int insId, string type)
        {
            // Log the method entry
            Console.WriteLine($"[INFO] GetSpecializationWithFiles called with InsID: {insId}, Type: {type}");

            var specialization = _dbHandler.GetSpecialization(insId, type);
            if (specialization == null)
            {
                // Log not found scenario
                Console.WriteLine($"[WARNING] Specialization not found for InsID: {insId}, Type: {type}");
                return NotFound(new { success = false, message = "Specialization not found." });
            }

            // Log that specialization was found
            Console.WriteLine($"[INFO] Specialization found for InsID: {insId}, Type: {type}");

            // File paths
            string basePath = Path.Combine("uploads", insId.ToString(), type);
            var files = new Dictionary<string, string>
    {
        { "NumProfFile", $"{basePath}/NumProfFile.xlsx" },
        { "NumAssociativeFile", $"{basePath}/NumAssociativeFile.xlsx" },
        { "NumAssistantFile", $"{basePath}/NumAssistantFile.xlsx" },
        { "NumPhdHoldersFile", $"{basePath}/NumPhdHoldersFile.xlsx" },
        { "NumProfPracticeFile", $"{basePath}/NumProfPracticeFile.xlsx" },
        { "NumberLecturersFile", $"{basePath}/NumberLecturersFile.xlsx" },
        { "NumAssisLecturerFile", $"{basePath}/NumAssisLecturerFile.xlsx" },
        { "NumOtherTeachersFile", $"{basePath}/NumOtherTeachersFile.xlsx" }
    };

            // Log files dictionary
            Console.WriteLine("[INFO] Files included in response:");
            foreach (var file in files)
            {
                Console.WriteLine($"  {file.Key}: {file.Value}");
            }

            // Log successful response
            Console.WriteLine($"[INFO] Returning specialization data and file paths for InsID: {insId}, Type: {type}");

            return Ok(new { specialization, files });
        }


        [HttpPost("save-scientific-bachelor")]
        public async Task<IActionResult> SaveScientificBachelorWithFiles(
            [FromForm] string specializationJson,
            [FromForm] IFormFile NumProfFile,
            [FromForm] IFormFile NumAssociativeFile,
            [FromForm] IFormFile NumAssistantFile,
            [FromForm] IFormFile NumPhdHoldersFile,
            //[FromForm] IFormFile NumProfPracticeFile,
            [FromForm] IFormFile NumberLecturersFile,
            [FromForm] IFormFile NumAssisLecturerFile,
            [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Scientific Bachelor")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.ScientificBachelorRatio(specialization);
            _specializationList.Add(specialization);
            try
            {
                _dbHandler.InsertSpecialization(specialization);
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging library like Serilog, NLog, or a custom logger)
                Console.Error.WriteLine($"[Error] Failed to insert specialization into the database: {ex.Message}");
                Console.Error.WriteLine($"[Stack Trace] {ex.StackTrace}");

                // Return a meaningful error response
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An error occurred while saving the specialization to the database.",
                    details = ex.Message
                });
            }



            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Scientific Bachelor"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;

            }

            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            //bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }

        [HttpPost("save-Humanitarian-Bachelor")]
        public async Task<IActionResult> SaveHumanitarianBachelorWithFiles(
            [FromForm] string specializationJson,
            [FromForm] IFormFile NumProfFile,
            [FromForm] IFormFile NumAssociativeFile,
            [FromForm] IFormFile NumAssistantFile,
            [FromForm] IFormFile NumPhdHoldersFile,
            //[FromForm] IFormFile NumProfPracticeFile,
            [FromForm] IFormFile NumberLecturersFile,
            [FromForm] IFormFile NumAssisLecturerFile,
            [FromForm] IFormFile NumOtherTeachersFile
        )
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Humanitarian Bachelor")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Humanitarian Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Humanitarian Bachelor"
            _verifier.HumanitarianBachelorRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/Humanitarian_Bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,             // Institution subfolder
                "Humanitarian Bachelor"    // Subfolder specific to humanitarian bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }

            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            //bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set)
            return Ok(specialization);
        }
        [HttpPost("save-Scientific-Practical-Bachelor")]
        public async Task<IActionResult> SaveScientificPracticalBachelorWithFiles(
            [FromForm] string specializationJson,
            [FromForm] IFormFile NumProfFile,
            [FromForm] IFormFile NumAssociativeFile,
            [FromForm] IFormFile NumAssistantFile,
            [FromForm] IFormFile NumPhdHoldersFile,
            [FromForm] IFormFile NumProfPracticeFile,
            [FromForm] IFormFile NumberLecturersFile,
            [FromForm] IFormFile NumAssisLecturerFile,
            [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Scientific Practical Bachelor")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.ScientificPracticalBachelorRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Scientific Practical Bachelor"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || !profPracticeOk ||
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-Humnamitarian-Practical-Bachelor")]
        public async Task<IActionResult> SaveHumnamitarianPracticalBachelorWithFiles(
           [FromForm] string specializationJson,
           [FromForm] IFormFile NumProfFile,
           [FromForm] IFormFile NumAssociativeFile,
           [FromForm] IFormFile NumAssistantFile,
           [FromForm] IFormFile NumPhdHoldersFile,
           [FromForm] IFormFile NumProfPracticeFile,
           [FromForm] IFormFile NumberLecturersFile,
           [FromForm] IFormFile NumAssisLecturerFile,
           [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }
                                        
            if (specialization.Type != "Humanitarian Practical Bachelor")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.HumnamitarianPracticalBachelorRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Humnamitarian Practical Bachelor"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || !profPracticeOk ||
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-High-Diploma")]
        public async Task<IActionResult> SaveHighDiplomaWithFiles(
    [FromForm] string specializationJson,
    [FromForm] IFormFile NumProfFile,
    [FromForm] IFormFile NumAssociativeFile,
    [FromForm] IFormFile NumAssistantFile,
    [FromForm] IFormFile NumPhdHoldersFile,
    //[FromForm] IFormFile NumProfPracticeFile,
    [FromForm] IFormFile NumberLecturersFile,
    [FromForm] IFormFile NumAssisLecturerFile,
    [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "High Diploma")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.HighDiplomaRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "High Diploma"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            //bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-Scientific-Masters")]
        public async Task<IActionResult> SaveScientificMastersWithFiles(
    [FromForm] string specializationJson,
    [FromForm] IFormFile NumProfFile,
    [FromForm] IFormFile NumAssociativeFile,
    [FromForm] IFormFile NumAssistantFile,
    [FromForm] IFormFile NumPhdHoldersFile,
    //[FromForm] IFormFile NumProfPracticeFile,
    [FromForm] IFormFile NumberLecturersFile,
    [FromForm] IFormFile NumAssisLecturerFile,
    [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Scientific Masters")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.ScientificMastersRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Scientific Masters"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
           // bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-Humanitarian-Masters")]
        public async Task<IActionResult> SaveHumanitarianMastersWithFiles(
    [FromForm] string specializationJson,
    [FromForm] IFormFile NumProfFile,
    [FromForm] IFormFile NumAssociativeFile,
    [FromForm] IFormFile NumAssistantFile,
    [FromForm] IFormFile NumPhdHoldersFile,
  //  [FromForm] IFormFile NumProfPracticeFile,
    [FromForm] IFormFile NumberLecturersFile,
    [FromForm] IFormFile NumAssisLecturerFile,
    [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Humanitarian Masters")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.HumanitarianMastersRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Humanitarian Masters"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            //bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-Scientific-Practical-Masters")]
        public async Task<IActionResult> SaveScientificPracticalMastersWithFiles(
            [FromForm] string specializationJson,
            [FromForm] IFormFile NumProfFile,
            [FromForm] IFormFile NumAssociativeFile,
            [FromForm] IFormFile NumAssistantFile,
            [FromForm] IFormFile NumPhdHoldersFile,
            [FromForm] IFormFile NumProfPracticeFile,
            [FromForm] IFormFile NumberLecturersFile,
            [FromForm] IFormFile NumAssisLecturerFile,
            [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Scientific Practical Masters")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.ScientificPracticalMastersRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Scientific Practical Masters"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || !profPracticeOk ||
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-Main-Medical")]
        public async Task<IActionResult> SaveMainMedicalWithFiles(
            [FromForm] string specializationJson,
            [FromForm] IFormFile NumProfFile,
            [FromForm] IFormFile NumAssociativeFile,
            [FromForm] IFormFile NumAssistantFile,
            [FromForm] IFormFile NumPhdHoldersFile,
           // [FromForm] IFormFile NumProfPracticeFile,
            [FromForm] IFormFile NumberLecturersFile,
            [FromForm] IFormFile NumAssisLecturerFile,
            [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Main Medical")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.MainMedicalRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Main Medical"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            //bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-Residency")]
        public async Task<IActionResult> SaveResidencyWithFiles(
            [FromForm] string specializationJson,
            [FromForm] IFormFile NumProfFile,
            [FromForm] IFormFile NumAssociativeFile,
            [FromForm] IFormFile NumAssistantFile,
            [FromForm] IFormFile NumPhdHoldersFile,
           // [FromForm] IFormFile NumProfPracticeFile,
            [FromForm] IFormFile NumberLecturersFile,
            [FromForm] IFormFile NumAssisLecturerFile,
            [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Residency")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.ResidencyRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Residency"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
            //bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }
        [HttpPost("save-Doctorate")]
        public async Task<IActionResult> SaveDoctorateWithFiles(
            [FromForm] string specializationJson,
            [FromForm] IFormFile NumProfFile,
            [FromForm] IFormFile NumAssociativeFile,
            [FromForm] IFormFile NumAssistantFile,
            [FromForm] IFormFile NumPhdHoldersFile,
            //[FromForm] IFormFile NumProfPracticeFile,
            [FromForm] IFormFile NumberLecturersFile,
            [FromForm] IFormFile NumAssisLecturerFile,
            [FromForm] IFormFile NumOtherTeachersFile
)
        {
            // 0) Read InstitutionID from Headers
            var institutionId = Request.Headers["InstitutionID"].ToString();
            if (string.IsNullOrEmpty(institutionId))
            {
                return BadRequest(new { success = false, message = "Institution ID is required." });
            }

            // 1) Parse the JSON into your Specialization object
            if (string.IsNullOrWhiteSpace(specializationJson))
            {
                return BadRequest(new { success = false, message = "Missing JSON data for specialization." });
            }

            Specialization specialization;
            try
            {
                specialization = JsonConvert.DeserializeObject<Specialization>(specializationJson);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Invalid JSON data. {ex.Message}" });
            }

            if (specialization.Type != "Doctorate")
            {
                // If it's not the correct type, either handle differently or just allow it
                return BadRequest(new { success = false, message = "This endpoint is only for Scientific Bachelor type." });
            }

            // 2) Run the existing ratio logic for "Scientific Bachelor"
            _verifier.DoctorateRatio(specialization);
            _specializationList.Add(specialization);
            _dbHandler.InsertSpecialization(specialization);
            // 3) Construct the base upload path to include InstitutionID + subfolder
            // e.g. wwwroot/uploads/{institutionId}/scientific_bachelor
            string baseUploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                institutionId,            // Institution subfolder
                "Doctorate"     // Subfolder specific to scientific bachelor
            );
            Directory.CreateDirectory(baseUploadPath);

            // Allowed file types
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx", ".csv" };

            // Helper method to validate & save a file
            async Task<bool> ValidateAndSaveFileAsync(IFormFile file, string fileNamePrefix)
            {
                if (file != null && file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return false;
                    }

                    string filePath = Path.Combine(baseUploadPath, $"{fileNamePrefix}{fileExtension}");
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                return true;
            }


            // Validate and save each file
            bool profOk = await ValidateAndSaveFileAsync(NumProfFile, "NumProfFile");
            bool assocOk = await ValidateAndSaveFileAsync(NumAssociativeFile, "NumAssociativeFile");
            bool assistOk = await ValidateAndSaveFileAsync(NumAssistantFile, "NumAssistantFile");
            bool phdOk = await ValidateAndSaveFileAsync(NumPhdHoldersFile, "NumPhdHoldersFile");
           // bool profPracticeOk = await ValidateAndSaveFileAsync(NumProfPracticeFile, "NumProfPracticeFile");
            bool lecturersOk = await ValidateAndSaveFileAsync(NumberLecturersFile, "NumberLecturersFile");
            bool assisLectOk = await ValidateAndSaveFileAsync(NumAssisLecturerFile, "NumAssisLecturerFile");
            bool otherTeachOk = await ValidateAndSaveFileAsync(NumOtherTeachersFile, "NumOtherTeachersFile");

            if (!profOk || !assocOk || !assistOk || !phdOk || 
                !lecturersOk || !assisLectOk || !otherTeachOk)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "One or more files have invalid types (.xls, .xlsx, .csv only)."
                });
            }

            // 4) Return the updated specialization object (with color set) 
            return Ok(specialization);
        }












        // -----------------------------------------
        //  Other existing GET/DELETE endpoints
        // -----------------------------------------
        // GET: api/specialization/get-all
        [HttpGet("get-all")]
        public IActionResult GetAllSpecializations()
        {
            return Ok(_specializationList);
        }

        // GET: api/specialization/get-by-type/{type}
        [HttpGet("get-by-type/{type}")]
        public IActionResult GetSpecializationByType(string type)
        {
            var specializations = _specializationList.FindAll(s => s.Type.Equals(type, System.StringComparison.OrdinalIgnoreCase));
            if (specializations.Count == 0)
            {
                return NotFound(new { success = false, message = $"No specializations found for type '{type}'." });
            }
            return Ok(specializations);
        }

        // DELETE: api/specialization/delete-by-type/{type}
        [HttpDelete("delete-by-type/{type}")]
        public IActionResult DeleteSpecializationByType(string type)
        {
            var specializationsRemoved = _specializationList.RemoveAll(s => s.Type.Equals(type, System.StringComparison.OrdinalIgnoreCase));
            if (specializationsRemoved == 0)
            {
                return NotFound(new { success = false, message = $"No specializations found for type '{type}'." });
            }

            return Ok(new { success = true, message = $"All specializations of type '{type}' deleted successfully." });
        }
        [HttpGet("get-by-insid/{insId}")]
        public IActionResult GetAllSpecializationsByInsID(int insId)
        {
            // Example query using your DatabaseHandler
            // We assume you have a “GetSpecialization” that requires (InsID, Type), 
            // so we need a “GetAllSpecializationsForInsId” method or similar.
            // For demonstration, let's do a custom DB query:

            // In real code, you'd do something like:
            // var specs = _dbHandler.GetAllSpecializationsFor(insId);

            // If you don’t have such a function yet, create one in DatabaseHandler that does:
            // SELECT * FROM Specializations WHERE InsID=@InsID
            // Then map each row to a Specialization object.

            List<Specialization> specs = new List<Specialization>();
            try
            {
                // A new function you implement in DatabaseHandler:
                specs = _dbHandler.GetAllSpecializationsByInsID(insId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }

            if (specs == null || specs.Count == 0)
            {
                return Ok(new { success = true, data = new List<Specialization>() });
            }

            return Ok(new { success = true, data = specs });
        }

    }
}
