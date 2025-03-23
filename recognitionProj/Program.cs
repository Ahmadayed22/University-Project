//DISCLAIMER:
//This software is provided "as is", without warranty of any kind, express or implied, 
//including but not limited to the warranties of merchantability, fitness for a particular 
//purpose, and noninfringement. In no event shall the author(s) or copyright holder(s) 
//be liable for any claim, damages, or other liability, whether in an action of contract, 
//tort, or otherwise, arising from, out of, or in connection with the software or the 
//use or other dealings in the software.



using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using recognitionProj;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Register controllers with JSON options to include fields during serialization/deserialization
//IMPORTANT!!! THE CONTROLLERS ARE OPEN TO ANY API REQUESTS WITHOUT AUTHENTICATION
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.IncludeFields = true; // Serialize fields in addition to properties
    });

// Register Verifier and DatabaseHandler services
builder.Services.AddSingleton<Verifier>();
builder.Services.AddSingleton<Emailer>();
//IMPORTANT!!! THE CERTIFICATE IS NOT VALIDATED
//IMPORTANT!!! PASSWORDS ARE NOT HASHED
//IMPORTANT!!! AUTH.JS AND OTHER AUTH JS IS VULNERABLE TO ADJUSTMENTS TO LOCALSTORAGE
var connectionString = "Server=ABOAYED22;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";
builder.Services.AddSingleton(new DatabaseHandler(connectionString));

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add HTTP logging
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    options.RequestBodyLogLimit = 4096; // Log up to 4 KB of the request body
    options.ResponseBodyLogLimit = 4096; // Log up to 4 KB of the response body
});

var app = builder.Build();

// Add middleware for development environment
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpLogging();
app.UseStaticFiles(); // Serve static files from wwwroot
app.UseRouting();
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

// Log server startup
var url = "http://localhost:5000";
Console.WriteLine($"Server is running at {url}");

// Open Mspec3.html in the default browser
var htmlFilePath = $"{url}/homepage.html";
Process.Start(new ProcessStartInfo
{
    FileName = htmlFilePath,
    UseShellExecute = true
});

// Run the application
await app.RunAsync();