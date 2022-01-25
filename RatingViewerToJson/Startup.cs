using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;

[assembly: FunctionsStartup(typeof(RatingViewerToJson.Startup))]

namespace RatingViewerToJson
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var hostContext = builder.GetContext();

            // bring in services
            builder.Services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(hostContext.Configuration["AzureWebJobsStorage"]);
            });
        }
    }
}