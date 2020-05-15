using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LM.ImageAnalyzer.Function
{
    public static class StoreImage
    {
        [FunctionName("StoreImage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Blob("uploadedimages", Connection = "StorageAnalyzer")] CloudBlobContainer blobOutput,
            ILogger log)
        {
            var analyzerId = Guid.NewGuid();
            log.LogInformation($"Start - analyzerId: {analyzerId}.");

            try
            {
                await blobOutput.CreateIfNotExistsAsync();
                CloudBlockBlob blob = blobOutput.GetBlockBlobReference(analyzerId.ToString());
                blob.Properties.ContentType = "image/jpg";
                await blob.UploadFromStreamAsync(req.Body);
            }
            catch (Exception ex)
            {
                log.LogError("Upload to blob failed", ex);
                return new InternalServerErrorResult();
            }

            log.LogInformation($"End - analyzerId: {analyzerId}.");
            return new OkObjectResult(analyzerId);
        }
    }
}