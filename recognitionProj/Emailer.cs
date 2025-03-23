using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mailslurp.Api;
using mailslurp.Client;
using mailslurp.Model;

namespace recognitionProj
{
    public class Emailer
    {
        private readonly string apiKey = "api key"; // Your MailSlurp API Key
        private readonly Guid senderInboxId; // Use GUID instead of email string
        private readonly Configuration config;
        private readonly InboxControllerApi inboxControllerApi;
        private readonly EmailControllerApi emailControllerApi;

        public Emailer()
        {
            // Initialize MailSlurp Configuration
            config = new Configuration { ApiKey = { ["x-api-key"] = apiKey } };
            inboxControllerApi = new InboxControllerApi(config);
            emailControllerApi = new EmailControllerApi(config);

            // Create a new inbox dynamically (You can replace this with a predefined MailSlurp inbox)
            var inbox = inboxControllerApi.CreateInboxWithDefaults();
            senderInboxId = inbox.Id; // Store the inbox GUID instead of email address

            Console.WriteLine($"✅ MailSlurp Inbox Created: {inbox.EmailAddress} (ID: {senderInboxId})");
        }

        public async Task<bool> SendEmail(string recipientEmail, string subject, string bodyHtml, string bodyText)
        {
            try
            {
                // Create email options
                var sendEmailOptions = new SendEmailOptions
                {
                    To = new List<string> { recipientEmail },
                    Subject = subject,
                    Body = bodyHtml, // HTML content
                    IsHTML = true
                };

                // Use the correct method and pass `senderInboxId` (GUID)
                var sentEmail = await inboxControllerApi.SendEmailAndConfirmAsync(senderInboxId, sendEmailOptions);

                Console.WriteLine($"✅ Email sent successfully to {recipientEmail}, Email ID: {sentEmail.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
