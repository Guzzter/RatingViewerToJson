using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace RatingViewerToJson
{
    public class RatingFunctions
    {
        private readonly BlobServiceClient _blobServiceClient;

        public RatingFunctions(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        /// <summary>
        /// Saves a copy for the archive with date prefix: 2022-01-sga-rating.json
        /// </summary>
        [FunctionName("RatingViewerToArchiveFileJson")]
        public async Task Run([TimerTrigger("0 0 12 1 * *", RunOnStartup = true)] TimerInfo t, ILogger log)
        {
            var fileName = $"{DateTime.Now.ToString("yyyy-MM", DateTimeFormatInfo.InvariantInfo)}-sga-rating.json";

            // 0 0 12 1 * * At 12:00 on day-of-month 1 (https://ncrontab.swimburger.net/)
            log.LogInformation($"RatingViewerToArchiveFileJson Timer trigger function executed at: {DateTime.Now} to write file: {fileName}");

            var containerClient = _blobServiceClient.GetBlobContainerClient("output");
            var blobClient = containerClient.GetBlockBlobClient(fileName);

            await SaveRatingToAzStorage(blobClient, true);

            // OLD CODE: works, but sets content type to application/octet-stream which results in no direct viewing (only download)
            // Add as param: IBinder binder
            //var outboundBlob = new BlobAttribute($"output/{fileName}", FileAccess.Write);
            //using var writer = binder.Bind<Stream>(outboundBlob);
            //await RatingDumper.Dump(writer);
        }

        /// <summary>
        /// Saves the latest ratings list: latest-sga-rating.json
        /// </summary>
        [FunctionName("RatingViewerToCurrentJson")]
        public async Task RunJsonAsync(
            [TimerTrigger("0 0 12 1/7 * *", RunOnStartup = true)] TimerInfo t, ILogger log,
            [Blob("output/latest-sga-rating.json", FileAccess.Write)] BlockBlobClient blobClient)
        {
            // At 12:00 on day-of-month 1 (https://ncrontab.swimburger.net/)
            log.LogInformation($"RatingViewerToCurrentJson Timer trigger function executed at: {DateTime.Now} to write latest-sga-rating.json");

            await SaveRatingToAzStorage(blobClient, false);
        }

        private static async Task SaveRatingToAzStorage(BlockBlobClient blobClient, bool isArchiveRun)
        {
            Stream dump = new MemoryStream();
            await RatingDumper.Dump(dump, DateTime.UtcNow.Year, DateTime.UtcNow.Month, isArchiveRun);
            dump.Position = 0;

            if (dump.Length > 0)
            {
                await blobClient.UploadAsync(dump, new BlobHttpHeaders { ContentType = "application/json" });
            }
        }
    }
}