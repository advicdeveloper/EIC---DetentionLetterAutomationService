using DetentionLetterAzureFunction.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

[assembly: FunctionsStartup(typeof(DetentionLetterAzureFunction.Startup))]

namespace DetentionLetterAzureFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IDataverseService, DataverseService>();
            builder.Services.AddScoped<ILetterDeterminationService, LetterDeterminationService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IDetentionLetterProcessingService, DetentionLetterProcessingService>();
        }
    }
}
