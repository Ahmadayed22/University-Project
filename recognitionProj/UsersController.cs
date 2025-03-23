// File: Controllers/UsersController.cs

using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System.Collections.Generic;
using System.Linq;

namespace RecognitionProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase


    {
        // In-memory list to store user information
        private static readonly List<Users> _userInfoList = new List<Users>();
        private readonly DatabaseHandler _dbHandler;
        //private readonly Emailer _emailer;

        // Constructor injection for DatabaseHandler
        public UsersController(DatabaseHandler dbHandler/*, Emailer emailer*/)
        {
            _dbHandler = dbHandler;
            //_emailer = emailer;
        }
        public class MyLoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string VerificationCode { get; set; }
            // Added for nonconventional application requests:
            public string ApplicationType { get; set; } // e.g. "normal" or "nonconventional"
            public string AcceptanceRecordNumber { get; set; } // Required if ApplicationType == "nonconventional"
        }

        // File: UsersController.cs
        // DTO for requesting a password reset
        public class PasswordResetRequestDto
        {
            public string Email { get; set; }
        }

        // DTO for confirming password reset
        public class PasswordResetConfirmDto
        {
            public string Email { get; set; }
            public string VerificationCode { get; set; }
            public string NewPassword { get; set; }
        }

        // 1) A small DTO that can handle an array of specialities
        public class UserSignupDto
        {
            public string InsName { get; set; }
            public string InsCountry { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string VerificationCode { get; set; }
            public int? Verified { get; set; }
            public int? Clearance { get; set; }

            // The client sends an array like ["Humanitarian","Scientific","Medical"]
            public List<string> speciality { get; set; }
        }

        // 2) Then your endpoint uses this DTO:
        [HttpPost("signup")]
        public IActionResult SaveUserInfo([FromBody] UserSignupDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Invalid user information data." });

            // Convert the array into a single string
            var joinedSpecialities = (dto.speciality == null)
                ? string.Empty
                : string.Join(',', dto.speciality);

            // Construct your `Users` object using all parameters your constructor needs:
            var userInfo = new Users(
                0,                      // insID ( forced to 0 or DB identity)
                dto.InsName,            // insName
                dto.InsCountry,         // insCountry
                dto.Email,              // email
                dto.Password,           // password
                "abc123",               // verificationCode forced 
                0,                      // verified forced 
                0,                      // clearance forced 
                joinedSpecialities      // store the joined array string
            );

            // (Optional) check if email already in use, etc...
            // Insert to DB
            _dbHandler.InsertUser(userInfo);
            return Ok(new { success = true, message = "User information saved successfully." });
        }

        // POST: api/users/login
        // POST: api/users/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] MyLoginRequest loginRequest)
        {
            if (loginRequest == null ||
                string.IsNullOrEmpty(loginRequest.Email) ||
                string.IsNullOrEmpty(loginRequest.Password) ||
                string.IsNullOrEmpty(loginRequest.VerificationCode))
            {
                return BadRequest(new { success = false, message = "Invalid login data." });
            }

            // Check the verification code (for example purposes, "abc123" is hardcoded)
            if (loginRequest.VerificationCode != "abc123")
            {
                return BadRequest(new { success = false, message = "Invalid verification code." });
            }

            // Hardcoded admin check
            if (loginRequest.Email.Equals("admin@admin.com", StringComparison.OrdinalIgnoreCase) &&
                loginRequest.Password == "admin")
            {
                return Ok(new
                {
                    success = true,
                    message = "Admin login successful.",
                    insID = 9999,
                    insName = "Admin",
                    clearance = 2,
                    speciality = "admin",
                    insCountry = "Admin Country",
                    verified = 1,
                    nonConventional = false
                });
            }

            // Fetch the user from the database.
            var dbUsers = _dbHandler.GetAllUsers();
            var existingUser = dbUsers.FirstOrDefault(u =>
                u.Email.Equals(loginRequest.Email, StringComparison.OrdinalIgnoreCase) &&
                u.Password == loginRequest.Password);

            if (existingUser == null)
            {
                return Unauthorized(new { success = false, message = "Invalid email or password." });
            }

            // Flag for nonconventional mode (default conventional)
            bool isNonConventional = false;

            // If the client selected "nonconventional", then check the acceptance record.
            if (!string.IsNullOrEmpty(loginRequest.ApplicationType) &&
                loginRequest.ApplicationType.Equals("nonconventional", StringComparison.OrdinalIgnoreCase))
            {
                // Make sure the client provided an Acceptance Record ID.
                if (string.IsNullOrEmpty(loginRequest.AcceptanceRecordNumber))
                {
                    return BadRequest(new { success = false, message = "Acceptance Record ID is required for nonconventional applications." });
                }

                // Try parsing the supplied record ID.
                if (!int.TryParse(loginRequest.AcceptanceRecordNumber, out int recordId))
                {
                    return BadRequest(new { success = false, message = "Invalid Acceptance Record ID format." });
                }

                // Retrieve acceptance records for this institution.
                var records = _dbHandler.GetAllAcceptanceRecordsByInsId(existingUser.InsID);
                var acceptanceRecord = records.FirstOrDefault(r => r.RecordID == recordId);

                if (acceptanceRecord == null)
                {
                    return BadRequest(new { success = false, message = "No acceptance record found for the given ID." });
                }

                // Check if the record qualifies.
                // Here we assume that either a fully accepted record (IsAccepted == true) or one whose
                // reasons include "Partial" qualifies.
                bool qualifies = acceptanceRecord.IsAccepted ||
                                 (acceptanceRecord.Reason != null && acceptanceRecord.Reason.Any(r => r.Contains("Partial")));

                if (!qualifies)
                {
                    return BadRequest(new { success = false, message = "Your acceptance record does not qualify for nonconventional application." });
                }

                // Mark the login as nonconventional.
                isNonConventional = true;
            }

            // Build the successful response.
            return Ok(new
            {
                success = true,
                message = "Login successful.",
                insID = existingUser.InsID,
                insName = existingUser.InsName,
                clearance = existingUser.Clearance,
                speciality = existingUser.Speciality,
                insCountry = existingUser.InsCountry,
                verified = existingUser.Verified,
                nonConventional = isNonConventional
            });
        }


        // GET: api/users/get-all
        [HttpGet("get-all")]
        public IActionResult GetAllUserInfo()
        {
            // 1) Fetch users from your database
            var dbUsers = _dbHandler.GetAllUsers();

            // 2) Create a "safe" list omitting Password
            var safeUsers = dbUsers.Select(u => new
            {
                InsID = u.InsID,
                InsName = u.InsName,
                InsCountry = u.InsCountry,
                Email = u.Email,
                VerificationCode = u.VerificationCode, // Optional to omit this if you wish
                Verified = u.Verified,
                Clearance = u.Clearance,
                Speciality = u.Speciality
                // Password intentionally excluded
            });

            // 3) Return them
            return Ok(safeUsers);
        }
        // POST: api/users/request-password-reset
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto requestDto)
        {
            if (requestDto == null || string.IsNullOrEmpty(requestDto.Email))
            {
                return BadRequest(new { success = false, message = "Email is required." });
            }

            // Fetch the user by email
            var user = _dbHandler.GetAllUsers().FirstOrDefault(u => u.Email.Equals(requestDto.Email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                // For security reasons, it's best to return a generic message
                return Ok(new { success = true, message = "If the email exists, a reset link has been sent." });
            }

            // Generate a new verification code
            var verificationCode = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6); // Example: 6-character code
            user.VerificationCode = verificationCode;
            user.Verified = 0; // Reset verification status
            _dbHandler.UpdateUser(user);

            // Send the verification code via email
            var subject = "Password Reset Request";
            var body = $"Your password reset verification code is: {verificationCode}. Click here to reset your password: http://localhost:5000/reset-password.html";
            //await _emailer.SendEmail(user.Email, subject, body, body);

            //return Ok(new { success = true, message = "If the email exists, a reset link has been sent." });
            return Ok(new { success = true, message = "Emails are disabled due to end of limit." });
        }

        // POST: api/users/confirm-password-reset
        [HttpPost("confirm-password-reset")]
        public IActionResult ConfirmPasswordReset([FromBody] PasswordResetConfirmDto confirmDto)
        {
            if (confirmDto == null ||
                string.IsNullOrEmpty(confirmDto.Email) ||
                string.IsNullOrEmpty(confirmDto.VerificationCode) ||
                string.IsNullOrEmpty(confirmDto.NewPassword))
            {
                return BadRequest(new { success = false, message = "All fields are required." });
            }

            // Fetch the user by email
            var user = _dbHandler.GetAllUsers().FirstOrDefault(u => u.Email.Equals(confirmDto.Email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                return BadRequest(new { success = false, message = "Invalid email or verification code." });
            }

            // Verify the verification code
            if (user.VerificationCode != confirmDto.VerificationCode || user.Verified != 0)
            {
                return BadRequest(new { success = false, message = "Invalid email or verification code." });
            }

            // Update the password without clearing the verification code
            user.Password = confirmDto.NewPassword;
            // user.VerificationCode = null; // Removed to retain the verification code
             user.Verified = 1; // Removed to keep the verification status unchanged
            _dbHandler.UpdateUser(user);

            return Ok(new { success = true, message = "Password has been reset successfully." });
        }

    }
}
