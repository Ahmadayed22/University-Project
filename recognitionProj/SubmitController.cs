using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmitController : ControllerBase
    {
        private readonly DatabaseHandler _dbHandler;
        //private readonly Emailer _emailer;

        public SubmitController(DatabaseHandler dbHandler/*, Emailer emailer*/)
        {
            _dbHandler = dbHandler;
            //_emailer = emailer;
        }

        public class SubmissionRequestDto
        {
            public int InsID { get; set; }
            public string ApplicantName { get; set; }
            public string WorkPlace { get; set; }
            public string Email { get; set; }
        }

        [HttpPost("submit-application")]
        public async Task<IActionResult> SubmitApplication([FromBody] SubmissionRequestDto req)
        {
            if (req == null || req.InsID <= 0)
            {
                return BadRequest(new { success = false, message = "Missing or invalid form data." });
            }

            // 1️⃣ Get Institution Data
            var institution = _dbHandler.GetPublicInfoById(req.InsID);
            if (institution == null)
            {
                return BadRequest(new { success = false, message = "Institution not found." });
            }

            // 2️⃣ Fetch Speciality from Users Table
            var user = _dbHandler.GetUserByInsID(req.InsID);
            if (user == null)
            {
                return BadRequest(new { success = false, message = "User not found." });
            }
            string speciality = user.Speciality ?? "Unknown";

            // 3️⃣ Find the best available supervisor
            var bestSupervisor = GetBestSupervisor(speciality);
            if (bestSupervisor == null)
            {
                return Ok(new
                {
                    success = false,
                    message = "No available supervisor found matching institution’s specialties."
                });
            }

            // 4️⃣ Assign Supervisor and Update `PublicInfo`
            _dbHandler.AssignInstitutionToSupervisor(req.InsID, bestSupervisor.SupervisorID);
            _dbHandler.UpdateSupervisorNameInPublicInfo(req.InsID, bestSupervisor.SupervisorID, bestSupervisor.Name);

            // 5️⃣ Add application to `ApplicationQueue` (for meeting scheduling)
            //_dbHandler.InsertIntoQueue(req.InsID, institution.InsName, speciality);

       
            // 7️⃣ Count pending applications in queue
            int pendingCount = _dbHandler.CountPendingApplications();

            // 8️⃣ Notify admin if 8 or more applications are in queue
            if (pendingCount >= 8)
            {
                var subject = "Supervisor Meeting Required";
                var body = $"There are {pendingCount} pending applications. Please schedule a meeting.";
                //await _emailer.SendEmail("user-018b492c-4ce3-4d6c-8cae-df4c7acaaef4@mailslurp.biz", subject, body, body);
            }

            return Ok(new
            {
                success = true,
                message = $"Assigned to supervisor {bestSupervisor.Name} (ID={bestSupervisor.SupervisorID}), and application added to queue for future meeting scheduling. Please not that no email was sent due to limit end"
            });
        }

        // ✅ Find the best available supervisor based on specialization and workload
        // ✅ Find the best available supervisor, considering fairness in workload
        private Supervisor GetBestSupervisor(string speciality)
        {
            var allSupervisors = _dbHandler.GetAllSupervisors();

            
            // 1️⃣ First, try to find supervisors matching the speciality
            var eligibleSupervisors = allSupervisors
                .Where(s => s.Speciality.Contains(speciality, StringComparison.OrdinalIgnoreCase))
                .Select(s => new
                {
                    Supervisor = s,
                    Workload = _dbHandler.CountInstitutionsAssignedToSupervisor(s.SupervisorID)
                })
                .OrderBy(s => s.Workload)
                .ToList();

            // 2️⃣ If we find matching supervisors, return the one with the least workload
            if (eligibleSupervisors.Any())
            {
                return eligibleSupervisors.First().Supervisor;
            }

            // 3️⃣ No exact match found? Assign to **any** supervisor based on fairness
            var allWithWorkload = allSupervisors
                .Select(s => new
                {
                    Supervisor = s,
                    Workload = _dbHandler.CountInstitutionsAssignedToSupervisor(s.SupervisorID)
                })
                .OrderBy(s => s.Workload)
                .ToList();

            return allWithWorkload.First().Supervisor; // Pick the one with the least workload
        }

    }
}
