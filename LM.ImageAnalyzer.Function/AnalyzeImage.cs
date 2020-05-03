using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace LM.ImageAnalyzer.Function
{
    public static class AnalyzeImage
    {
        private const int NumberOfCharsInOperationId = 36;
        private static ComputerVisionClient _computerVisionClient;

        [FunctionName("AnalyzeImage")]
        public static void Run([BlobTrigger("uploadedimages/{name}", Connection = "StorageAnalyzer")]Stream imageToAnalyze, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {imageToAnalyze.Length} Bytes");

            try
            {
                _computerVisionClient =
                   new ComputerVisionClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("COMPUTER_VISION_KEY")))
                   {
                       Endpoint = Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT")
                   };


                log.LogInformation($"Analyze image");
                string analyzeResult = Analyze(imageToAnalyze);
                log.LogInformation($"Analyze result: {analyzeResult}");
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                throw;
            }
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
            return analyzeResult.ToString();
        }
    }
}