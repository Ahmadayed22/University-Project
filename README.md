Downloads


Microsoft SQL server (16.0.1000.6) (you can pick an sql server that is compatible with the sql client package for c#)

C# dotnet (8.0.13)

mailslurp package for c#

Microsoft.Data.Sqlclient package for c#

Newtonsoft.json package for c#

IDE recommended:

Visual studio 2022



Make sure to create the database, then link it by reading program.cs and putting the connectionstring of the database inside.

Make sure to go to Emailer.cs and put your own API key for mailslurp to create and send emails. Note that you will need a paid mailslurp account to sent emails outside of mailslurp. Otherwise modify Emailer.cs to create emails the way you like to

You need to uncomment these lines in SubmitController 
```
        //private readonly Emailer _emailer;

        public SubmitController(DatabaseHandler dbHandler/*, Emailer emailer*/)

        //_emailer = emailer;
```
```
        //await _emailer.SendEmail("user-018b492c-4ce3-4d6c-8cae-df4c7acaaef4@mailslurp.biz", subject, body, body);

Make sure to modify the admin's email in SubmitController.cs. The email is the one that will receive an email that 8 universities applied and to recommend making a meeting.
```
in UsersController
```
        //private readonly Emailer _emailer;
        public UsersController(DatabaseHandler dbHandler/*, Emailer emailer*/)
        //await _emailer.SendEmail(user.Email, subject, body, body);
        //return Ok(new { success = true, message = "If the email exists, a reset link has been sent." });
```
in emailer.cs, please modify it to work with your existing email system!!

Make sure to authenticate the sessions, encrypt the passwords so that they are not plaintext,get certificates for encrypted communication, handle security, privacy with the cybersecurity team. DO NOT SKIP THIS PART.
