using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using TableEntities = LM.ImageAnalyzer.Shared.Entities;

namespace LM.ImageAnalyzer.Function
{
    public static class AnalyzeImage
    {
        private static ComputerVisionClient _computerVisionClient;

        [FunctionName("AnalyzeImage")]
        public static void Run([BlobTrigger("uploadedimages/{name}", Connection = "StorageAnalyzer")]Stream imageToAnalyze,
            [Table("AnalyzeResult", Connection = "")] CloudTable resultTable,
            string name, ILogger log)
        {
            log.LogInformation($"AnalyzeImage function Processed blob\n Name:{name} \n Size: {imageToAnalyze.Length} Bytes");

            try
            {
                _computerVisionClient =
                   new ComputerVisionClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("COMPUTER_VISION_KEY")))
                   {
                       Endpoint = Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT")
                   };

                log.LogInformation($"Analyze image");
                string jsonResult = Analyze(imageToAnalyze);
                log.LogInformation($"Analyze result: {jsonResult}");

                log.LogInformation($"Save json result");
                SaveResult(resultTable, name, jsonResult).Wait();
                log.LogInformation($"Ready");
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                throw;
            }
        }

        private static async Task SaveResult(CloudTable cloudTable, string imageId, string jsonResult)
        {
            await cloudTable.CreateIfNotExistsAsync();
            var tableEntry = new TableEntities.AnalyzeResult()

            {
                RowKey = imageId,
                PartitionKey = imageId,
                ImageAnalysisJson = jsonResult
            };

            await cloudTable.ExecuteAsync(TableOperation.InsertOrReplace(tableEntry));
        }

        private static string Analyze(Stream imageToAnalyze)
        {
            IList<VisualFeatureTypes> features = new List<VisualFeatureTypes>
            {
                VisualFeatureTypes.Adult,
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Faces,
                VisualFeatureTypes.ImageType,
                VisualFeatureTypes.Tags
            };

            ImageAnalysis analyzeResult = _computerVisionClient.AnalyzeImageInStreamAsync(imageToAnalyze, features).Result;
            return JsonConvert.SerializeObject(analyzeResult, Formatting.Indented);
        }
    }
}