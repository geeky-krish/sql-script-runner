using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SQLScripRunner.Common.Enums;
using SQLScripRunner.Models;
using SQLScripRunner.Services;

public class Program
{
    private const int MF_BYCOMMAND = 0x00000000;
    public const int SC_CLOSE = 0xF060;

    [DllImport("user32.dll")]
    public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    public static void Main(string[] args)
    {
        if (!IsAdministrator())
        {
            Console.WriteLine("To execute scripts from SQLScriptRunner app, It needs to run with Administrator Privileges.");
            return;
        }

        DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);


        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Read EventLogger source from appsettings.json
        string appName = configuration.GetSection("ApplicationName").Value ?? "SQLScriptRunner";

        // Ensure EventLogger object is created with the source.
        EventLoggerService eventLog = new EventLoggerService(appName);

        // Load configuration from logging.config.json
        var logConfig = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("logging.config.json", optional: false, reloadOnChange: true)
        .Build();

        // Configure SeriLog
        Log.Logger = new LoggerConfiguration()
           .ReadFrom.Configuration(logConfig) // Read SeriLog settings
           .Enrich.FromLogContext()
           .CreateLogger();

        try
        {
            var services = new ServiceCollection();

            Log.Information($"{appName} App Started, EventId: {(int)EventIds.AppStart}");

            eventLog.LogInformation($"{appName} App Started,", (int)EventIds.AppStart);

            services.AddSingleton<IConfiguration>(configuration);

            services.Configure<AppSettings>(configuration);

            // Add SeriLog as the logger
            services.AddLogging(config => config.AddSerilog());

            services.AddTransient<ScriptRunnerService>();
            services.AddTransient<ScriptExecManagerService>();

            var serviceProvider = services.BuildServiceProvider();

            var scriptRunner = serviceProvider.GetRequiredService<ScriptRunnerService>();

            Log.Information($"{appName} App Bootstrapped successfully -EventId:{(int)EventIds.AppStartedSuccessfully}");
            eventLog.LogInformation($"{appName} App Bootstrapped successfully.", (int)EventIds.AppStartedSuccessfully);

            scriptRunner.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal($"{appName} Terminated unexpectedly, Exception Details: {ex.Message}");
            eventLog.LogError($"{appName} Terminated unexpectedly, Exception Details: {ex.Message}", (int)EventIds.AppStartFailed);
            throw;
        }
    }

    private static bool IsAdministrator()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}