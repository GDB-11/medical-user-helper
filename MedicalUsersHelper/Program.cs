using System.Data;
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

namespace MedicalUsersHelper;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "medical-helper.db");
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
            });

        // Start a simple HTTP server for cross-platform compatibility
        var wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        var serverUrl = "http://localhost:8765";

        if (Directory.Exists(wwwrootPath))
        {
            // Start simple HTTP server in background
            var serverTask = Task.Run(() => StartSimpleServer(wwwrootPath, 8765));

            // Give server a moment to start
            Thread.Sleep(500);

            // Load from localhost
            window.Load($"{serverUrl}/index.html");

            // Wait for window to close
            window.WaitForClose();
        }
        else
        {
            window.LoadRawString("<html><body><h1>Error: wwwroot folder not found</h1></body></html>");
            window.WaitForClose();
        }

        // Simple HTTP server for serving static files
        static void StartSimpleServer(string path, int port)
        {
            var listener = new System.Net.HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");

            try
            {
                listener.Start();

                while (listener.IsListening)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var response = context.Response;

                    try
                    {
                        // Get requested file path
                        var relativePath = request.Url?.LocalPath.TrimStart('/') ?? "index.html";
                        if (string.IsNullOrEmpty(relativePath)) relativePath = "index.html";

                        var filePath = Path.Combine(path, relativePath);

                        if (File.Exists(filePath))
                        {
                            // Set content type
                            response.ContentType = GetContentType(filePath);

                            // Read and send file
                            var buffer = File.ReadAllBytes(filePath);
                            response.ContentLength64 = buffer.Length;
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                            response.StatusCode = 200;
                        }
                        else
                        {
                            response.StatusCode = 404;
                            var errorBytes = System.Text.Encoding.UTF8.GetBytes("404 - File Not Found");
                            response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = 500;
                    }
                    finally
                    {
                        response.OutputStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                listener.Stop();
            }
        }

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

        // Run the app
        window.WaitForClose();
    }
}