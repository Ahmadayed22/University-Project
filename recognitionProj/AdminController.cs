using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Web;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DatabaseHandler _dbHandler;
        // In-memory list to store library information temporarily (for demonstration purposes)
        private readonly ILogger<AdminController> _logger;

        public AdminController(DatabaseHandler dbHandler, ILogger<AdminController> logger)
        {
            _dbHandler = dbHandler;
            _logger = logger;
        }
        //public void DistributeWorkload()
        //{
        //    var pendingApps = _dbHandler.GetPendingApplications();
        //    var allSupervisors = _dbHandler.GetAllSupervisors();
        //    var groupedBySpecialization = pendingApps.GroupBy(a => a.Specialization);

        //    foreach (var group in groupedBySpecialization)
        //    {
        //        var spec = group.Key;
        //        var applications = group.ToList();
        //        var supervisors = allSupervisors
        //            .Where(s => s.Speciality.Contains(spec))
        //            .OrderBy(s => _dbHandler.CountInstitutionsAssignedToSupervisor(s.SupervisorID))
        //            .ToList();

        //        if (!supervisors.Any())
        //        {
        //            supervisors = allSupervisors.OrderBy(s => _dbHandler.CountInstitutionsAssignedToSupervisor(s.SupervisorID)).ToList();
        //        }

        //        int index = 0;
        //        foreach (var app in applications)
        //        {
        //            var chosenSupervisor = supervisors[index % supervisors.Count];
        //            _dbHandler.AssignInstitutionToSupervisor(app.InsID, chosenSupervisor.SupervisorID);
        //            _dbHandler.MarkAsProcessed(app.InsID);
        //            index++;
        //        }
        //    }

        //}
        //GET: api/Admin/pending-applications
        [HttpGet("pending-applications")]
        public IActionResult GetPendingApplicationsAPI()
        {
            try
            {
                var pendingApps = _dbHandler.GetPendingApplications();
                var result = pendingApps.Select(app =>
                {
                    // Build the path to the records folder for this institution.
                    string recordsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", app.InsID.ToString(), "records");
                    string committeeRecommendation;

                    if (Directory.Exists(recordsPath))
                    {
                        // Get all HTML files in the records folder.
                        var files = Directory.GetFiles(recordsPath, "*.html");
                        if (files.Length > 0)
                        {
                            // Get the latest file by creation time.
                            var latestFile = files.OrderByDescending(f => System.IO.File.GetCreationTime(f)).First();
                            DateTime creationDate = System.IO.File.GetCreationTime(latestFile);
                            string fileName = Path.GetFileName(latestFile);
                            // Build a clickable link using the creation date as the link text.
                            committeeRecommendation = $"<a href='/uploads/{app.InsID}/records/{HttpUtility.UrlEncode(fileName)}' target='_blank'>{creationDate:yyyy-MM-dd}</a>";
                        }
                        else
                        {
                            committeeRecommendation = "Committee member did not proccess this institution yet.";
                        }
                    }
                    else
                    {
                        committeeRecommendation = "Committee member did not proccess this institution yet.";
                    }

                    return new
                    {
                        app.QueueID,
                        app.InsID,
                        app.InstitutionName,
                        app.Specialization,
                        app.SubmissionDate,
                        // New property with the clickable link
                        committeeRecommendation
                    };
                }).ToList();
                
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending applications.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("get-supervisor")]
        public IActionResult GetSupervisor(int insId)
        {
            try
            {
                // Get the PublicInfo record for the institution
                var publicInfo = _dbHandler.GetPublicInfoById(insId);
                if (publicInfo == null)
                {
                    return NotFound(new { success = false, message = "Institution not found." });
                }

                
                int supervisorID = (int)publicInfo.SupervisorID;
                if (supervisorID == 0)
                {
                    return Ok(new { success = true, supervisorName = "Not Assigned" });
                }

                // Get the supervisor details
                var supervisor = _dbHandler.GetSupervisorById(supervisorID);
                if (supervisor == null)
                {
                    return Ok(new { success = true, supervisorName = "Not Assigned" });
                }

                return Ok(new { success = true, supervisorName = supervisor.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching supervisor for insId: {InsId}", insId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpPost("create-meeting")]
        public IActionResult CreateMeeting()
        {
            int year = DateTime.Now.Year;
            int meetingNumber = _dbHandler.GetNextMeetingNumber(); // ✅ Fixed to correctly increment
            string meetingID = $"{meetingNumber}/{year}";

            var pendingApps = _dbHandler.GetPendingApplications();

            if (!pendingApps.Any())
            {
                return BadRequest(new { success = false, message = "No pending applications to process." });
            }

            // ✅ Check if meeting ID already exists
            var existingMeeting = _dbHandler.GetMeetingByNumber(meetingID);
            if (existingMeeting.Any())
            {
                return BadRequest(new { success = false, message = $"Meeting {meetingID} already exists." });
            }

            foreach (var app in pendingApps)
            {
                _dbHandler.CreateMeetingEntry(meetingID, app.InsID, app.InstitutionName, app.Specialization, app.SubmissionDate);
            }

            _dbHandler.MarkQueueAsProcessed();

            return Ok(new { success = true, message = $"Meeting {meetingID} created successfully." });
        }

        [HttpGet("institution-statuses")]
        public IActionResult GetInstitutionStatuses()
        {
            var statuses = _dbHandler.GetInstitutionStatuses();
            return Ok(new { success = true, data = statuses });
        }


        [HttpGet("meeting-applications")]
        public IActionResult GetMeetingApplications()
        {
            var applications = _dbHandler.GetMeetingApplications();
            var result = applications.Select(app =>
            {
                // Build the path to the records folder for this institution.
                string recordsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", app.InsID.ToString(), "records");
                string committeeRecommendation;  // local variable to hold the link (or default text)

                if (Directory.Exists(recordsPath))
                {
                    // Get all HTML files in the records folder.
                    var files = Directory.GetFiles(recordsPath, "*.html");
                    if (files.Length > 0)
                    {
                        // Get the latest file by creation time.
                        var latestFile = files.OrderByDescending(f => System.IO.File.GetCreationTime(f)).First();
                        DateTime creationDate = System.IO.File.GetCreationTime(latestFile);
                        string fileName = Path.GetFileName(latestFile);

                        // Build a link to the file (the creation date is used as link text).
                        committeeRecommendation = $"<a href='/uploads/{app.InsID}/records/{HttpUtility.UrlEncode(fileName)}' target='_blank'>{creationDate:yyyy-MM-dd}</a>";
                    }
                    else
                    {
                        // No HTML files found.
                        committeeRecommendation = "Committee member did not proccess this instiution yet.";
                    }
                }
                else
                {
                    // Directory does not exist.
                    committeeRecommendation = "Committee member did not proccess this instiution yet.";
                }

                // Return an anonymous object with all the original fields plus our computed property.
                return new
                {
                    app.MeetingNumber,
                    app.InsID,
                    app.InstitutionName,
                    app.Specialization,
                    app.SubmissionDate,
                    app.SupervisorName,
                    committeeRecommendation  // this is the new property that holds the link or default text
                };
            }).ToList();

            return Ok(new { success = true, data = result });

        }
        [HttpGet("meeting/{meetingNumber}")]
        public IActionResult GetMeetingByNumber(string meetingNumber)
        {
            if (string.IsNullOrWhiteSpace(meetingNumber))
            {
                return BadRequest(new { success = false, message = "Meeting number is required." });
            }

            // Decode the meeting number if necessary.
            string decodedMeetingNumber = Uri.UnescapeDataString(meetingNumber);

            // Get the meeting details (without computed fields) from your DB handler.
            var meetingDetails = _dbHandler.GetMeetingByNumber(decodedMeetingNumber);
            if (meetingDetails == null || !meetingDetails.Any())
            {
                return NotFound(new { success = false, message = $"No meeting found with MeetingNumber: {decodedMeetingNumber}" });
            }

            // Now compute the committee recommendation link for each meeting.
            var resultData = meetingDetails.Select(app =>
            {
                // Build the path to the records folder for this institution.
                string recordsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", app.InsID.ToString(), "records");
                string committeeRecommendation;
                if (Directory.Exists(recordsPath))
                {
                    // Get all HTML files in the records folder.
                    var files = Directory.GetFiles(recordsPath, "*.html");
                    if (files.Length > 0)
                    {
                        // Get the latest file by creation time.
                        var latestFile = files.OrderByDescending(f => System.IO.File.GetCreationTime(f)).First();
                        DateTime creationDate = System.IO.File.GetCreationTime(latestFile);
                        string fileName = Path.GetFileName(latestFile);
                        // Build a clickable link using the creation date as the link text.
                        committeeRecommendation = $"<a href='/uploads/{app.InsID}/records/{HttpUtility.UrlEncode(fileName)}' target='_blank'>{creationDate:yyyy-MM-dd}</a>";
                    }
                    else
                    {
                        committeeRecommendation = "Committee member did not process this institution yet.";
                    }
                }
                else
                {
                    committeeRecommendation = "Committee member did not process this institution yet.";
                }

                return new
                {
                    app.MeetingNumber,
                    app.CreationDate,
                    app.InsID,
                    app.InstitutionName,
                    app.Specialization,
                    app.SubmissionDate,
                    app.SupervisorName,
                    committeeRecommendation
                };
            }).ToList();

            return Ok(new { success = true, data = resultData });
        }

        [HttpGet("all-meeting-numbers")]
        public IActionResult GetAllMeetingNumbers()
        {
            List<MeetingInfo> meetingNumbers = _dbHandler.GetAllMeetingNumbers();
            var result = meetingNumbers.Select(meeting => new
            {
                meeting.MeetingNumber,
                CreationDate = meeting.CreationDate.ToString("yyyy-MM-dd")
            }).ToList();

            return Ok(new { success = true, data = result });
        }


        [HttpGet("supervisors")]
        public IActionResult GetSupervisors()
        {
            var supervisors = _dbHandler.GetAllSupervisors();
            return Ok(new { success = true, data = supervisors });
        }
        public class ChangeSupervisorRequest
        {
            public int MeetingID { get; set; }
            public int InsID { get; set; }
            public int SupervisorID { get; set; }
        }


        [HttpPost("change-supervisor")]
        public IActionResult ChangeSupervisor([FromBody] ChangeSupervisorRequest request)
        {
            if (request == null || request.InsID <= 0 || request.SupervisorID <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid parameters." });
            }
            try
            {
                // Update PublicInfo (this updates the supervisor name too)
                _dbHandler.AssignInstitutionToSupervisor(request.InsID, request.SupervisorID);
                // Update the Meetings record for this row
                _dbHandler.UpdateMeetingSupervisor(request.MeetingID, request.SupervisorID);

                return Ok(new { success = true, message = "Supervisor assignment updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supervisor assignment");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("return-to-supervisor")]
        public IActionResult ReturnToSupervisor([FromQuery] int insId)
        {
            try
            {
                // Check for existing decision
                string decisionCheckSql = @"
            SELECT TOP 1 decisionOutcome 
            FROM AcceptanceRecord 
            WHERE InsID = @insId 
            ORDER BY RecordID DESC";

                DataTable decisionDt = _dbHandler.SelectRecords(decisionCheckSql, new[] { "@insId" }, new object[] { insId });
                string existingDecision = null;
                bool hasDecision = false;

                if (decisionDt.Rows.Count > 0 && decisionDt.Rows[0]["decisionOutcome"] != DBNull.Value)
                {
                    hasDecision = true;
                    existingDecision = decisionDt.Rows[0]["decisionOutcome"].ToString();
                }

                // Original return logic
                var meetingRecords = _dbHandler.GetMeetingApplications()
                                            .Where(m => m.InsID == insId)
                                            .ToList();

                if (meetingRecords == null || meetingRecords.Count == 0)
                {
                    return NotFound(new { success = false, message = "No meeting record found." });
                }

                var latestMeeting = meetingRecords.OrderByDescending(m => m.MeetingID).First();

                if (latestMeeting.SupervisorID == null || latestMeeting.SupervisorID == 0)
                {
                    return BadRequest(new { success = false, message = "No supervisor assigned." });
                }

                var supervisor = _dbHandler.GetSupervisorById(latestMeeting.SupervisorID);
                if (supervisor == null)
                {
                    return BadRequest(new { success = false, message = "Supervisor not found." });
                }

                _dbHandler.UpdateSupervisorNameInPublicInfo(insId, supervisor.SupervisorID, supervisor.Name);
                _dbHandler.MarkLatestAsUnprocessed(insId);
                // Step 3: Delete the pending application record from the ApplicationQueue
                string deleteAppQueueSql = "DELETE FROM ApplicationQueue WHERE InsID = @insId AND Processed = 0";
                _dbHandler.ExecuteNonQuery(deleteAppQueueSql, new[] { "@insId" }, new object[] { insId });

                // Return warning if decision existed
                var response = new
                {
                    success = true,
                    message = $"Returned to {supervisor.Name}" + (hasDecision ? " (Had decision: " + existingDecision + ")" : ""),
                    hadDecision = hasDecision,
                    decision = existingDecision
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }



    }


}