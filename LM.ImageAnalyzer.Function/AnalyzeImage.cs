using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LM.ImageAnalyzer.Function
{
    public static class AnalyzeImage
    {
        private static ComputerVisionClient _computerVisionClient;

        [FunctionName("AnalyzeImage")]
        public static async Task<HttpResponseMessage> Run([BlobTrigger("uploadedimages/{name}", Connection = "StorageAnalyzer")]Stream imageToAnalyze, string name, ILogger log)
        {
            log.LogInformation($"C# AnalyzeImage function Processed blob\n Name:{name} \n Size: {imageToAnalyze.Length} Bytes");

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
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonResult, Encoding.UTF8, "application/json")
                };
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(e.Message, Encoding.UTF8, "application/json")
                };
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
            return JsonConvert.SerializeObject(analyzeResult, Formatting.Indented);
        }
    }
}