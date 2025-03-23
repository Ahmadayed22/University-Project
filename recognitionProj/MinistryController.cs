using Microsoft.AspNetCore.Mvc;
using recognitionProj;
using System.Data;
using System.Text.Json; // for JSON parsing if your message is valid JSON

[ApiController]
[Route("api/ministry")]
public class MinistryController : ControllerBase
{
    private readonly DatabaseHandler _db;

    public MinistryController(DatabaseHandler db)
    {
        _db = db;
    }

    [HttpGet("GenerateMinistryLetter")]
    public IActionResult GenerateMinistryLetter(int insId, string meetingNumber)
    {
        try
        {
            // 1) Get the latest AcceptanceRecord
            string sqlAcceptance = @"
                SELECT TOP 1 *
                FROM AcceptanceRecord
                WHERE InsID = @insId
                ORDER BY RecordID DESC;
            ";
            DataTable acceptanceDt = _db.SelectRecords(sqlAcceptance, new[] { "@insId" }, new object[] { insId });
            if (acceptanceDt.Rows.Count == 0)
            {
                return BadRequest("No AcceptanceRecord found for that institution.");
            }

            DataRow accRow = acceptanceDt.Rows[0];
            int recordID = Convert.ToInt32(accRow["RecordID"]);
            string rawResult = Convert.ToString(accRow["Result"]) ?? "";
            string rawMessage = Convert.ToString(accRow["Message"]) ?? "";

            // 2) Meeting info
            string sqlMeeting = @"
                SELECT TOP 1 CreationDate
                FROM Meetings
                WHERE MeetingNumber = @mtg
                ORDER BY MeetingID DESC;
            ";
            DataTable mtgDt = _db.SelectRecords(sqlMeeting, new[] { "@mtg" }, new object[] { meetingNumber });
            string meetingCreationDate = (mtgDt.Rows.Count > 0)
                ? Convert.ToDateTime(mtgDt.Rows[0]["CreationDate"]).ToString("yyyy-MM-dd")
                : "(No meeting date)";

            // 3) Institution name & country
            string sqlPubInfo = @"
                SELECT TOP 1 InsName, Country
                FROM PublicInfo
                WHERE InsID = @insId;
            ";
            DataTable pubDt = _db.SelectRecords(sqlPubInfo, new[] { "@insId" }, new object[] { insId });
            string institutionName = (pubDt.Rows.Count > 0) ? Convert.ToString(pubDt.Rows[0]["InsName"]) : "Unknown Institution";
            string institutionCountry = (pubDt.Rows.Count > 0) ? Convert.ToString(pubDt.Rows[0]["Country"]) : "Unknown Country";

            // 4) Parse the message lines (JSON array or fallback)
            string[] messageLines;
            try
            {
                messageLines = JsonSerializer.Deserialize<string[]>(rawMessage);
            }
            catch
            {
                messageLines = rawMessage
                    .Trim('[', ']')
                    .Split(new[] { "\",", "\"," }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim('"', ' '))
                    .ToArray();
            }

            // 5) We detect if the partial lines mention diploma/bsc rejections:
            bool hasDiplomaRejection = false;
            bool hasBscRejection = false;
            bool hasHigherRejection = false;
            bool hasDiplomaRecognition = false;
            bool hasBscRecognition = false;
            bool hasHigherRecognition = false;

            // We'll also gather bullet reasons
            var reasonsList = new List<string>();
            bool isNonConventional = false;

            foreach (var line in messageLines)
            {
                string lower = line.ToLower().Trim();

                // Check if non-conventional recognized or rejected
                if (line.StartsWith("non-conventional status: rejected", StringComparison.OrdinalIgnoreCase))
                {
                    isNonConventional = true;
                    // We do *not* break or set anything special for recognizedDegreePhrase
                    // because your "something else" handles the .mainRejected for non-conventional
                    continue;
                }
                else if (line.Contains("non-conventional status: recognized", StringComparison.OrdinalIgnoreCase))
                {
                    isNonConventional = true;
                    continue;
                }

                // e.g. "Partial Rejected: Diploma Rejection"
                if (line.Contains("partial rejected: diploma rejection", StringComparison.OrdinalIgnoreCase))
                {
                    hasDiplomaRejection = true;
                }
                // e.g. "Partial Rejected: BSc Rejection"
                if (line.Contains("partial rejected: bsc rejection", StringComparison.OrdinalIgnoreCase))
                {
                    hasBscRejection = true;
                }
                // e.g. "Rejection: Higher Graduate Studies Rejected"
                if (line.Contains("rejection: higher graduate studies rejected", StringComparison.OrdinalIgnoreCase))
                {
                    hasHigherRejection = true;
                }
                // now for the recognized
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

                // Skip lines that start with partial/rejection/recognition from bullet reasons
                if (lower.StartsWith("partial") ||
                    lower.StartsWith("rejection") ||
                    lower.StartsWith("recognition") ||
                    lower.StartsWith("non-conventional status") ||
                    lower.StartsWith("rejected"))
                {
                    continue;
                }

                // Otherwise => bullet reason
                reasonsList.Add(line);
            }

            // 6) Build recognizedDegreePhrase with exactly one of the 4 strings:
            //    a) "intermediate diploma degree only"
            //    b) "bachelor's degree only"
            //    c) "intermediate diploma and bachelor's degrees"
            //    d) "higher graduate studies degree"
            //
            // "Diploma + BSc" => if we see partial rejections for both
            // "Diploma" => if partial rejection is only for diploma
            // "BSc" => if partial rejection is only for BSc
            // "Higher" => if lines mention "Rejection: Higher Graduate..." or we default to that

            string recognizedDegreePhrase = "higher graduate studies degree";
            // This is your fallback if neither diploma nor bsc rejections appear.

            // If we see partial rejections for diploma and bsc => (both)
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

            // 7) Show recognized/rejected div. 
            // You said: "If partial rejected BSc => recognizeddiv = false => mainRejected is shown."
            // We'll keep your existing logic:
            // We'll do a quick check if rawResult has "rejected" => show mainRejected
            bool showRecognizedDiv = hasBscRecognition || hasDiplomaRecognition || hasHigherRecognition;
            bool showRejectedDiv = hasBscRejection || hasDiplomaRejection || hasHigherRejection;
            if (showRecognizedDiv == showRejectedDiv)
            {
                recognizedDegreePhrase = "error, please pick either recognition or rejection error, please pick either recognition or rejection error, please pick either recognition or rejection";
                showRecognizedDiv = false;
                showRejectedDiv = true;
            }
            // 8) Bullet points
            string bulletPointsHtml = "";
            foreach (var reason in reasonsList)
            {
                bulletPointsHtml += $"<li>{reason}</li>";
            }
            
            // Return final placeholders
            var resultObject = new
            {
                recordid = recordID.ToString(),
                meetingnumber = meetingNumber,
                MeetingCreationDate = meetingCreationDate,
                insname = institutionName,
                inscountry = institutionCountry,
                recognizedDegreePhrase,
                systemPhrase = isNonConventional ? "non-conventional (Online)" : "traditional",
                showRecognizedDiv,
                showRejectedDiv,
                bulletPoints = bulletPointsHtml
            };

            return Ok(resultObject);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error generating letter: " + ex.Message);
        }
    }
    [HttpGet("GenerateMinistryLetterArabic")]
    public IActionResult GenerateMinistryLetterArabic(int insId, string meetingNumber)
    {
        try
        {
            // 1) Get the latest AcceptanceRecord
            string sqlAcceptance = @"
                SELECT TOP 1 *
                FROM AcceptanceRecord
                WHERE InsID = @insId
                ORDER BY RecordID DESC;
            ";
            DataTable acceptanceDt = _db.SelectRecords(sqlAcceptance, new[] { "@insId" }, new object[] { insId });
            if (acceptanceDt.Rows.Count == 0)
            {
                return BadRequest("No AcceptanceRecord found for that institution.");
            }

            DataRow accRow = acceptanceDt.Rows[0];
            int recordID = Convert.ToInt32(accRow["RecordID"]);
            string rawResult = Convert.ToString(accRow["Result"]) ?? "";
            string rawMessage = Convert.ToString(accRow["Message"]) ?? "";

            // 2) Meeting info
            string sqlMeeting = @"
                SELECT TOP 1 CreationDate
                FROM Meetings
                WHERE MeetingNumber = @mtg
                ORDER BY MeetingID DESC;
            ";
            DataTable mtgDt = _db.SelectRecords(sqlMeeting, new[] { "@mtg" }, new object[] { meetingNumber });
            string meetingCreationDate = (mtgDt.Rows.Count > 0)
                ? Convert.ToDateTime(mtgDt.Rows[0]["CreationDate"]).ToString("yyyy-MM-dd")
                : "(No meeting date)";

            // 3) Institution name & country
            string sqlPubInfo = @"
                SELECT TOP 1 InsName, Country
                FROM PublicInfo
                WHERE InsID = @insId;
            ";
            DataTable pubDt = _db.SelectRecords(sqlPubInfo, new[] { "@insId" }, new object[] { insId });
            string institutionName = (pubDt.Rows.Count > 0) ? Convert.ToString(pubDt.Rows[0]["InsName"]) : "Unknown Institution";
            string institutionCountry = (pubDt.Rows.Count > 0) ? Convert.ToString(pubDt.Rows[0]["Country"]) : "Unknown Country";

            // 4) Parse the message lines (JSON array or fallback)
            string[] messageLines;
            try
            {
                messageLines = JsonSerializer.Deserialize<string[]>(rawMessage);
            }
            catch
            {
                messageLines = rawMessage
                    .Trim('[', ']')
                    .Split(new[] { "\",", "\"," }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim('"', ' '))
                    .ToArray();
            }

            // 5) We detect if the partial lines mention diploma/bsc rejections:
            bool hasDiplomaRejection = false;
            bool hasBscRejection = false;
            bool hasHigherRejection = false;
            bool hasDiplomaRecognition = false;
            bool hasBscRecognition = false;
            bool hasHigherRecognition = false;

            // We'll also gather bullet reasons
            var reasonsList = new List<string>();
            bool isNonConventional = false;

            foreach (var line in messageLines)
            {
                string lower = line.ToLower().Trim();

                // Check if non-conventional recognized or rejected
                if (line.StartsWith("non-conventional status: rejected", StringComparison.OrdinalIgnoreCase))
                {
                    isNonConventional = true;
                    // We do *not* break or set anything special for recognizedDegreePhrase
                    // because your "something else" handles the .mainRejected for non-conventional
                    continue;
                }
                else if (line.Contains("non-conventional status: recognized", StringComparison.OrdinalIgnoreCase))
                {
                    isNonConventional = true;
                    continue;
                }

                // e.g. "Partial Rejected: Diploma Rejection"
                if (line.Contains("partial rejected: diploma rejection", StringComparison.OrdinalIgnoreCase))
                {
                    hasDiplomaRejection = true;
                }
                // e.g. "Partial Rejected: BSc Rejection"
                if (line.Contains("partial rejected: bsc rejection", StringComparison.OrdinalIgnoreCase))
                {
                    hasBscRejection = true;
                }
                // e.g. "Rejection: Higher Graduate Studies Rejected"
                if (line.Contains("rejection: higher graduate studies rejected", StringComparison.OrdinalIgnoreCase))
                {
                    hasHigherRejection = true;
                }
                // now for the recognized
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

                // Skip lines that start with partial/rejection/recognition from bullet reasons
                if (lower.StartsWith("partial") ||
                    lower.StartsWith("rejection") ||
                    lower.StartsWith("recognition") ||
                    lower.StartsWith("non-conventional status") ||
                    lower.StartsWith("rejected"))
                {
                    continue;
                }

                // Otherwise => bullet reason
                reasonsList.Add(line);
            }

            // 6) Build recognizedDegreePhrase with exactly one of the 4 strings:
            //    a) "intermediate diploma degree only"
            //    b) "bachelor's degree only"
            //    c) "intermediate diploma and bachelor's degrees"
            //    d) "higher graduate studies degree"
            //
            // "Diploma + BSc" => if we see partial rejections for both
            // "Diploma" => if partial rejection is only for diploma
            // "BSc" => if partial rejection is only for BSc
            // "Higher" => if lines mention "Rejection: Higher Graduate..." or we default to that

            string recognizedDegreePhrase = "higher graduate studies degree";
            // This is your fallback if neither diploma nor bsc rejections appear.

            // If we see partial rejections for diploma and bsc => (both)
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

            // 7) Show recognized/rejected div. 
            // You said: "If partial rejected BSc => recognizeddiv = false => mainRejected is shown."
            // We'll keep your existing logic:
            // We'll do a quick check if rawResult has "rejected" => show mainRejected
            bool showRecognizedDiv = hasBscRecognition || hasDiplomaRecognition || hasHigherRecognition;
            bool showRejectedDiv = hasBscRejection || hasDiplomaRejection || hasHigherRejection;
            if (showRecognizedDiv == showRejectedDiv)
            {
                recognizedDegreePhrase = "error, please pick either recognition or rejection error, please pick either recognition or rejection error, please pick either recognition or rejection";
                showRecognizedDiv = false;
                showRejectedDiv = true;
            }
            // 8) Bullet points
            string bulletPointsHtml = "";
            foreach (var reason in reasonsList)
            {
                bulletPointsHtml += $"<li>{reason}</li>";
            }

            // Now “translate” the key English phrases into Arabic using helper methods.
            string arabicRecognizedDegreePhrase = TranslateDegreePhraseToArabic(recognizedDegreePhrase);
            string arabicSystemPhrase = TranslateSystemPhraseToArabic(isNonConventional ? "non-conventional (Online)" : "traditional");
            string arabicBulletPointsHtml = TranslateBulletPointsToArabic(bulletPointsHtml);

            var resultObject = new
            {
                recordid = recordID.ToString(),
                meetingnumber = meetingNumber,
                MeetingCreationDate = meetingCreationDate,
                insname = institutionName,
                inscountry = institutionCountry,
                recognizedDegreePhrase = arabicRecognizedDegreePhrase,
                systemPhrase = arabicSystemPhrase,
                showRecognizedDiv,
                showRejectedDiv,
                bulletPoints = arabicBulletPointsHtml
            };
            
            return Ok(resultObject);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error generating letter: " + ex.Message);
        }
    }
    [HttpGet("institution-history")]
    public IActionResult GetInstitutionHistory(int insId)
    {
        try
        {
            // Use a subquery to retrieve only one (the latest) meeting date for the meeting number
            string sqlHistory = @"
            SELECT 
                a.*,
                (
                  SELECT TOP 1 CreationDate 
                  FROM Meetings 
                  WHERE MeetingNumber = a.meetingFinalized 
                  ORDER BY CreationDate DESC
                ) AS MeetingDate
            FROM AcceptanceRecord a
            WHERE a.InsID = @insId AND a.finalized IS NOT NULL
            ORDER BY a.RecordID DESC;
        ";

            DataTable dt = _db.SelectRecords(sqlHistory, new[] { "@insId" }, new object[] { insId });
            if (dt.Rows.Count == 0)
                return NotFound(new { success = false, message = "No history found for that institution." });

            var history = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                history.Add(new
                {
                    recordID = Convert.ToInt32(row["RecordID"]),
                    meetingFinalized = row.Table.Columns.Contains("meetingFinalized") ? row["meetingFinalized"].ToString() : "",
                    decisionOutcome = row.Table.Columns.Contains("decisionOutcome") ? row["decisionOutcome"].ToString() : "",
                    // Use the joined MeetingDate if available; otherwise, default to DateOfRecord (or "N/A")
                    meetingDate = row.Table.Columns.Contains("MeetingDate") && row["MeetingDate"] != DBNull.Value
                                    ? Convert.ToDateTime(row["MeetingDate"]).ToString("yyyy-MM-dd")
                                    : "N/A",
                    result = row["Result"].ToString(),
                    message = row["Message"].ToString()
                });
            }
            return Ok(new { success = true, data = history });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("finalize-acceptance")]
    public IActionResult FinalizeAcceptance([FromQuery] int insId, [FromQuery] string meetingNumber, [FromQuery] string decisionOutcome)
    {
        try
        {
            // Retrieve the latest acceptance record for the institution.
            string sqlAcceptance = @"
            SELECT TOP 1 *
            FROM AcceptanceRecord
            WHERE InsID = @insId
            ORDER BY RecordID DESC;
        ";
            DataTable dt = _db.SelectRecords(sqlAcceptance, new[] { "@insId" }, new object[] { insId });
            if (dt.Rows.Count == 0)
            {
                return NotFound(new { success = false, message = "No AcceptanceRecord found for that institution." });
            }
            DataRow row = dt.Rows[0];

            // Check if the record is already finalized
            if (row["decisionOutcome"] != DBNull.Value && row["decisionOutcome"].ToString() == "Recognized")
            {
                string existingDecision = row["decisionOutcome"].ToString();
                return BadRequest(new
                {
                    success = false,
                    message = $"Record already finalized with decision: {existingDecision}. Modifications are not allowed through the system. If you intend to reject a recognized institution, it must be done manually. The system will show you a document you can use but it is not finalized in the records database"
                });
            }

            int recordID = Convert.ToInt32(row["RecordID"]);

            // Mark the record as finalized by writing the meeting number and decision outcome.
            _db.MarkAcceptanceRecordFinalized(recordID, meetingNumber, decisionOutcome);

            return Ok(new { success = true, message = "Record finalized successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }


    // Dummy helper methods for translation
    private string TranslateDegreePhraseToArabic(string englishPhrase)
    {
        // Replace with your real translation logic or resource lookups.
        return englishPhrase.ToLower() switch
        {
            "higher graduate studies degree" => "درجة الدراسات العليا",
            "intermediate diploma and bachelor's degrees" => "دبلوم متوسط وبكالوريوس",
            "intermediate diploma degree only" => "دبلوم متوسط فقط",
            "bachelor's degree only" => "بكالوريوس فقط",
            _ => englishPhrase
        };
    }

    private string TranslateSystemPhraseToArabic(string englishPhrase)
    {
        return englishPhrase.ToLower() switch
        {
            "traditional" => "تقليدي",
            "non-conventional (online)" => "غير تقليدي (عبر الإنترنت)",
            _ => englishPhrase
        };
    }

    private string TranslateBulletPointsToArabic(string bulletPointsHtml)
    {
        
        return bulletPointsHtml; // For demonstration, no changes. this is crucial information and the english version is more accurate.
    }
}

