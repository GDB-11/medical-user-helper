using Application.Core.Interfaces.DrugEnforcementAdministration;
using Application.Core.Interfaces.License;
using Application.Core.Interfaces.NationalProviderIdentifier;
using Application.Core.Service.DrugEnforcementAdministration;
using Application.Core.Service.License;
using Application.Core.Service.NationalProviderIdentifier;
using Infrastructure.Core.Interfaces.DEARegistrationNumber;
using Infrastructure.Core.Interfaces.License;
using Infrastructure.Core.Interfaces.NationalProviderIdentifier;
using Infrastructure.Core.Services.DEARegistrationNumber;
using Infrastructure.Core.Services.License;
using Infrastructure.Core.Services.NationalProviderIdentifier;
using MedicalUsersHelper.DatabaseHelpers;
using MedicalUsersHelper.MessageHandlers;
using MedicalUsersHelper.MessageHandlers.Handlers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Photino.NET;
using System.Data;
using System.Text;

namespace MedicalUsersHelper;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Get the appropriate data directory for the platform
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MedicalUsersHelper"
        );

        // Ensure the directory exists
        Directory.CreateDirectory(appDataDir);

        // Create database path
        var databasePath = Path.Combine(appDataDir, "medical-helper.db");
        var databaseInitializer = new DatabaseInitializer(databasePath);
        databaseInitializer.Initialize();

        var services = new ServiceCollection();

        services.AddSingleton<IDbConnection>(sp =>
        {
            var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open(); // Open immediately for use
            return connection;
        });

        // Register message handlers
        services.AddTransient<IMessageHandler, DeaHandler>();
        services.AddTransient<IMessageHandler, LicenseHandler>();
        services.AddTransient<IMessageHandler, NpiHandler>();

        services.AddSingleton<IDeaRegistrationNumberRepository, DeaRegistrationNumberRepository>();
        services.AddSingleton<INdeaRegistrationNumberRepository, NdeaRegistrationNumberRepository>();
        services.AddSingleton<ILicenseNumberRepository, LicenseNumberRepository>();
        services.AddSingleton<INationalProviderIdentifierRepository, NationalProviderIdentifierRepository>();

        services.AddSingleton<IDrugEnforcementAdministration, DrugEnforcementAdministrationService>();
        services.AddSingleton<ILicenseNumber, LicenseNumberService>();
        services.AddSingleton<INationalProviderIdentifier, NationalProviderIdentifierService>();

        // Register message router
        services.AddSingleton<MessageRouter>();

        var serviceProvider = services.BuildServiceProvider();

        // Get the message router from DI
        var messageRouter = serviceProvider.GetRequiredService<MessageRouter>();

        // Create window
        var window = new PhotinoWindow()
            .SetTitle("Medical User Helper")
            .SetMaximized(maximized: true)
            .SetResizable(true)
            .RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var photinoWindow = (PhotinoWindow)sender!;
                messageRouter.RouteMessage(photinoWindow, message);
            })
            .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
            {
                contentType = "text/plain";
                var uri = new Uri(url);
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", uri.AbsolutePath.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    contentType = GetContentType(filePath);
                    return new FileStream(filePath, FileMode.Open, FileAccess.Read);
                }

                return new MemoryStream(Encoding.UTF8.GetBytes("404 - File Not Found"));
            })
            .Load("wwwroot/index.html");

        // Run the app
        window.WaitForClose();

        static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream"
            };
        }
    }
}