using Microsoft.AspNetCore.Mvc;
using recognitionProj;       // For DatabaseHandler, AcceptanceRecord, etc.
using System.Security.Cryptography;
using System.Text;
using System;
using System.Collections.Generic;
using System.Transactions;
using System.Data;
using System.Text.Json; // for JSON parsing if your message is valid JSON
using System.Text.RegularExpressions;
using System.Globalization;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupervisorController : ControllerBase
    {
        private readonly DatabaseHandler _db;

        public SupervisorController(DatabaseHandler db)
        {
            _db = db;
        }
        // POST: api/supervisor/add
        // Admin adds a new supervisor
        [HttpPost("add")]
        public IActionResult AddSupervisor([FromBody] Supervisor supervisorData)
        {
            if (supervisorData == null)
            {
                return BadRequest(new { success = false, message = "Invalid data for new supervisor." });
            }

            _db.InsertSupervisor(supervisorData);
            return Ok(new { success = true, message = "Supervisor created successfully." });
        }

        [HttpPost("login")]
        public IActionResult SupervisorLogin([FromBody] SupervisorLoginRequest req)
        {
            // 1) Check verification code
            if (req.VerificationCode != "abc123")
            {
                return Unauthorized(new { success = false, message = "Invalid verification code." });
            }

            // 2) If the user is your admin
            if (req.Email == "admin@admin.com" && req.Password == "abc123")
            {
                return Ok(new
                {
                    success = true,
                    isAdmin = true,
                    message = "Admin login success."
                    // optionally pass more fields
                });
            }

            // 3) Else check normal Supervisor table
            var found = _db.GetAllSupervisors().FirstOrDefault(s =>
              s.Email.Equals(req.Email, StringComparison.OrdinalIgnoreCase)
              && s.Password == req.Password
            );
            if (found == null)
            {
                return Unauthorized(new { success = false, message = "Invalid email or password." });
            }

            // normal supervisor => success
            return Ok(new
            {
                success = true,
                isAdmin = false,
                message = "Supervisor login success.",
                supervisorID = found.SupervisorID,
                name = found.Name,
                speciality = found.Speciality
            });
        }


        // A small request DTO for login
        public class SupervisorLoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }

            public string VerificationCode { get; set; }
        }

        [HttpPut("update-acceptance")]
        public IActionResult UpdateAcceptance([FromQuery] int insId, [FromBody] AcceptanceRecord updatedRecord)
        {
            if (updatedRecord == null)
            {
                return BadRequest(new { success = false, message = "Missing AcceptanceRecord data." });
            }

            try
            {
                // 1. Retrieve existing acceptance records for this institution.
                var records = _db.GetAllAcceptanceRecordsByInsId(insId);

                // Determine if this submission is non-conventional.
                bool isNonConvSubmission = (updatedRecord.Reason != null &&
                                            updatedRecord.Reason.Count > 0 &&
                                            updatedRecord.Reason[0].StartsWith("Non-Conventional Status:"));

                // Use today's date for the new record.
                var newDate = DateOnly.FromDateTime(DateTime.Now);

                if (!isNonConvSubmission)
                {
                    // Conventional submission.
                    updatedRecord.Date = newDate;
                    _db.InsertAcceptanceRecord(insId, updatedRecord);
                }
                else
                {
                    // Non-conventional submission.
                    var latestRecord = records.LastOrDefault();
                    var newReasons = new List<string>();
                    if (latestRecord?.Reason != null)
                    {
                        foreach (var r in latestRecord.Reason)
                        {
                            if (!r.StartsWith("Non-Conventional Status:"))
                            {
                                newReasons.Add(r);
                            }
                        }
                    }
                    // Prepend the non-conventional status from the incoming record.
                    newReasons.Insert(0, updatedRecord.Reason[0]);
                    var newRecord = new AcceptanceRecord(
                        isAccepted: latestRecord?.IsAccepted ?? false,
                        date: newDate,
                        reason: newReasons
                    );
                    _db.InsertAcceptanceRecord(insId, newRecord);
                }

                // 2. Wait until the inserted record is available in the database.
                int retry = 0;
                List<AcceptanceRecord> allRecordsForReport = null;
                while (retry < 10)
                {
                    allRecordsForReport = _db.GetAllAcceptanceRecordsByInsId(insId);
                    if (allRecordsForReport != null && allRecordsForReport.Count > 0 &&
                        allRecordsForReport.Last().Date == newDate)
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                    retry++;
                }
                var lastRecord = allRecordsForReport.Last();
                int lastRecordId = (int)lastRecord.RecordID;

                // 3. Get institution info.
                var institution = _db.GetPublicInfoById(insId);
                string insName = institution.InsName;
                string insCountry = institution.Country;
                

                // 4. Format the record's date.
                string recordDate = lastRecord.Date.ToString("yyyy-MM-dd");

                // 5. Process the record's reasons.
                bool hasDiplomaRejection = false;
                bool hasBscRejection = false;
                bool hasHigherRejection = false;
                bool hasDiplomaRecognition = false;
                bool hasBscRecognition = false;
                bool hasHigherRecognition = false;
                bool isNonConventional = false;
                var reasonsList = new List<string>();

                foreach (var line in lastRecord.Reason)
                {
                    string lower = line.ToLower().Trim();

                    if (line.StartsWith("non-conventional status: rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        isNonConventional = true;
                        continue;
                    }
                    else if (line.Contains("non-conventional status: recognized", StringComparison.OrdinalIgnoreCase))
                    {
                        isNonConventional = true;
                        continue;
                    }

                    if (line.Contains("partial rejected: diploma rejection", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDiplomaRejection = true;
                    }
                    if (line.Contains("partial rejected: bsc rejection", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBscRejection = true;
                    }
                    if (line.Contains("rejection: higher graduate studies rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        hasHigherRejection = true;
                    }
                    if (line.Contains("partial: diplomas recognized", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDiplomaRecognition = true;
                    }
                    if (line.Contains("partial: bachelor's recognized", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBscRecognition = true;
                    }
                    if (line.Contains("higher graduate studies recognized", StringComparison.OrdinalIgnoreCase))
                    {
                        hasHigherRecognition = true;
                    }

                    // Skip status keywords.
                    if (lower.StartsWith("partial") ||
                        lower.StartsWith("rejection") ||
                        lower.StartsWith("recognition") ||
                        lower.StartsWith("non-conventional status") ||
                        lower.StartsWith("rejected"))
                    {
                        continue;
                    }

                    reasonsList.Add(line);
                }

                // Decide the recognized degree phrase.
                string recognizedDegreePhrase = "higher graduate studies degree"; // default
                if ((hasDiplomaRejection && hasBscRejection) || (hasDiplomaRecognition && hasBscRecognition))
                {
                    recognizedDegreePhrase = "intermediate diploma and bachelor's degrees";
                }
                else if ((hasDiplomaRejection && !hasBscRejection) || (hasDiplomaRecognition && !hasBscRecognition))
                {
                    recognizedDegreePhrase = "intermediate diploma degree only";
                }
                else if ((!hasDiplomaRejection && hasBscRejection) || (!hasDiplomaRecognition && hasBscRecognition))
                {
                    recognizedDegreePhrase = "bachelor's degree only";
                }
                else if (hasHigherRejection)
                {
                    recognizedDegreePhrase = "higher graduate studies degree";
                }

                // Determine which block should be shown.
                bool showRecognizedDiv = hasBscRecognition || hasDiplomaRecognition || hasHigherRecognition;
                bool showRejectedDiv = hasBscRejection || hasDiplomaRejection || hasHigherRejection;
                if (showRecognizedDiv == showRejectedDiv)
                {
                    recognizedDegreePhrase = "error, please pick either recognition or rejection";
                    showRecognizedDiv = false;
                    showRejectedDiv = true;
                }

                // Build bullet points HTML.
                var bulletPointsBuilder = new StringBuilder();
                foreach (var reason in reasonsList)
                {
                    bulletPointsBuilder.AppendLine($"<li>{reason}</li>");
                }
                string bulletPointsHtml = bulletPointsBuilder.ToString();
                string systemPhrase = isNonConventional ? "non-conventional (Online)" : "traditional";

                // 6. Read the new recommendation.html template (which contains no JavaScript).
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string templatePath = Path.Combine(webRootPath, "recommendation.html");
                string templateContent = System.IO.File.ReadAllText(templatePath);

                // 7. Replace basic placeholders.
                templateContent = templateContent.Replace("{recordid}", lastRecordId.ToString());
                templateContent = templateContent.Replace("{date}", recordDate);

                var hijriCalendar = new HijriCalendar();
                int hijriYear = hijriCalendar.GetYear(DateTime.Today);
                int hijriMonth = hijriCalendar.GetMonth(DateTime.Today);
                int hijriDay = hijriCalendar.GetDayOfMonth(DateTime.Today);
                string hijriDate = $"{hijriYear:0000}/{hijriMonth:00}/{hijriDay:00}";

                templateContent = templateContent.Replace("{dateh}", hijriDate);

                templateContent = templateContent.Replace("{insname}", insName);
                templateContent = templateContent.Replace("{inscountry}", insCountry);
                templateContent = templateContent.Replace(
                    "{intermediate diploma degree only/bachelor's degree only/intermediate diploma and bachelor's degrees/higher graduate studies degree}",
                    recognizedDegreePhrase);
                templateContent = templateContent.Replace("{traditional/non-conventional (Online)}", systemPhrase);

                // 8. Replace the bullet points block.
                // Use a regex that ignores whitespace differences.
                templateContent = Regex.Replace(
                    templateContent,
                    @"<ul>\s*<li>\s*\{Reason\s+#1\}\s*</li>\s*<li>\s*\{Reason\s+#2\}\s*</li>\s*</ul>",
                    $"<ul>{bulletPointsHtml}</ul>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline
                );

                // 9. Remove the unused div block.
                if (showRejectedDiv)
                {
                    // Remove the recognized block.
                    templateContent = Regex.Replace(
                        templateContent,
                        @"<div\s+class\s*=\s*[""']placeholder\s+mainRecognized[""'][^>]*>.*?</div>",
                        "",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline
                    );
                }
                if (showRecognizedDiv)
                {
                    // Remove the rejected block.
                    templateContent = Regex.Replace(
                        templateContent,
                        @"<div\s+class\s*=\s*[""']placeholder\s+mainRejected[""'][^>]*>.*?</div>",
                        "",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline
                    );
                }

                // 10. Write the output file.
                string dirPath = Path.Combine(webRootPath, "uploads", insId.ToString(), "records");
                Directory.CreateDirectory(dirPath);
                string recordIdHash = GenerateHash(lastRecordId); // Your GenerateHash method.
                string fileName = $"{recordIdHash}.html";
                string filePath = Path.Combine(dirPath, fileName);
                System.IO.File.WriteAllText(filePath, templateContent);


                string speciality = "";
                var acainfo = _db.GetAcademicInfoById(insId);
                speciality = acainfo.Speciality;
                _db.InsertIntoQueue(insId, institution.InsName, speciality);

                // (Optional) Clear supervisor fields.
                //_db.ClearSupervisorForInstitution(insId);

                string reportLink = $"/uploads/{insId}/records/{fileName}";
                return Ok(new
                {
                    success = true,
                    message = "Recommendation record updated successfully.",
                    reportLink = reportLink
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



        [HttpGet("my-institutions")]
        public IActionResult GetMyInstitutions([FromQuery] int supervisorID)
        {
            // 1) Find all PublicInfo records where SupervisorID = supervisorID
            var allPubInfos = _db.GetAllPublicInfo(); // or a direct SELECT
            var appQ = _db.GetPendingApplications();

            // Get a list of InsIDs that have pending applications
            var pendingInsIDs = appQ.Select(app => app.InsID).ToList();

            // 2) Get institutions assigned to the supervisor and exclude those with pending applications
            var assigned = allPubInfos
                .Where(pi => pi.SupervisorID == supervisorID && !pendingInsIDs.Contains(pi.InsID))
                .Select(pi => new
                {
                    insID = pi.InsID,
                    insName = pi.InsName,
                    // maybe compute a "taskCount" or something
                    taskCount = 1 // or real logic if you want some measure of "open tasks"
                })
                .ToList();

            return Ok(new { success = true, data = assigned });
        }

        // GET: api/supervisor/all
        [HttpGet("all")]
        public IActionResult GetAllSupervisors()
        {
            try
            {
                var list = _db.GetAllSupervisors(); // Should return List<Supervisor>
                return Ok(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/supervisor/{id}
        [HttpDelete("{id:int}")]
        public IActionResult DeleteSupervisor(int id)
        {
            try
            {
                // Start a transaction to ensure atomicity
                using (var scope = new TransactionScope())
                {
                    // 1) Check if the supervisor exists
                    var supervisor = _db.GetAllSupervisors().FirstOrDefault(s => s.SupervisorID == id);
                    if (supervisor == null)
                    {
                        return NotFound(new { success = false, message = $"Supervisor with ID {id} not found." });
                    }

                    // 2) Check how many institutions are assigned
                    int assignedCount = _db.CountInstitutionsAssignedToSupervisor(id);
                    if (assignedCount > 0)
                    {
                        // Find the institutions assigned
                        var assignedInstitutions = _db.GetPublicInfoBySupervisorID(id);

                        // 3) Reassign each institution to the best available supervisor
                        foreach (var inst in assignedInstitutions)
                        {
                            // Get the speciality of the institution via USERS table
                            var user = _db.GetUserByInsID(inst.InsID);
                            string instSpeciality = user?.Speciality ?? "Unknown";

                            // Find the best supervisor excluding the one being deleted
                            var newSupervisor = GetBestSupervisorForReassign(instSpeciality, id);

                            if (newSupervisor != null)
                            {
                                // Reassign the institution to the new supervisor
                                _db.AssignInstitutionToSupervisor(inst.InsID, newSupervisor.SupervisorID);
                                _db.UpdateSupervisorNameInPublicInfo(inst.InsID, newSupervisor.SupervisorID, newSupervisor.Name);
                                // **Update SupervisorID in Meetings table**
                                _db.UpdateMeetingSupervisor(inst.InsID, id, newSupervisor.SupervisorID);

                            }
                            else
                            {
                                // Handle the case where no suitable supervisor is found
                                Console.WriteLine($"[DeleteSupervisor] No suitable supervisor found for institution {inst.InsID}.");
                            }
                        }
                    }

                    // 4) Delete the supervisor
                    _db.DeleteSupervisor(id);

                    // Complete the transaction
                    scope.Complete();

                    return Ok(new
                    {
                        success = true,
                        message = $"Supervisor with ID {id} deleted successfully. {assignedCount} institutions were reassigned."
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception details as needed
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        /// <summary>
        /// Finds the best available supervisor based on specialization and current workload.
        /// Excludes the supervisor with ID `excludeSupervisorID`.
        /// </summary>
        /// <param name="speciality">Specialization required.</param>
        /// <param name="excludeSupervisorID">Supervisor ID to exclude from selection.</param>
        /// <returns>The best available Supervisor or null if none found.</returns>
        private Supervisor GetBestSupervisorForReassign(string speciality, int excludeSupervisorID)
        {
            var allSupervisors = _db.GetAllSupervisors()
                                    .Where(s => s.SupervisorID != excludeSupervisorID)
                                    .ToList();

            // 1) Find supervisors matching the speciality
            var matchedSupervisors = allSupervisors
                .Where(s => !string.IsNullOrEmpty(s.Speciality) &&
                            s.Speciality.Equals(speciality, StringComparison.OrdinalIgnoreCase))
                .Select(s => new
                {
                    Supervisor = s,
                    Workload = _db.CountInstitutionsAssignedToSupervisor(s.SupervisorID)
                })
                .OrderBy(s => s.Workload)
                .ToList();

            if (matchedSupervisors.Any())
            {
                return matchedSupervisors.First().Supervisor;
            }

            // 2) If no matching supervisor, assign to any supervisor with the least workload
            var fallbackSupervisor = allSupervisors
                .Select(s => new
                {
                    Supervisor = s,
                    Workload = _db.CountInstitutionsAssignedToSupervisor(s.SupervisorID)
                })
                .OrderBy(s => s.Workload)
                .FirstOrDefault();

            return fallbackSupervisor?.Supervisor;
        }
        private string GenerateHash(int recordId)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(recordId.ToString());
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

    }
}
