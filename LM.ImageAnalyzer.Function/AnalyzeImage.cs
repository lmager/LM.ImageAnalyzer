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


                //log.LogInformation($"Analyze image");
                //string analyzeResult = Analyze(imageToAnalyze);
                //log.LogInformation($"Analyze result: {analyzeResult}");

                log.LogInformation($"Read text on image");
                IList<TextRecognitionResult> textResult = GetTextAsync(imageToAnalyze).Result;
                string text = LogTextResult(textResult);
                log.LogInformation($"Text Result: {text}");
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                throw;
            }
        }

        //private static string Analyze(Stream imageToAnalyze)
        //{
        //    IList<VisualFeatureTypes> features = new List<VisualFeatureTypes>
        //    {
        //        VisualFeatureTypes.Adult,
        //        VisualFeatureTypes.Description,
        //        VisualFeatureTypes.Faces,
        //        VisualFeatureTypes.ImageType,
        //        VisualFeatureTypes.Tags
        //    };

        //    ImageAnalysis analyzeResult = _computerVisionClient.AnalyzeImageInStreamAsync(imageToAnalyze, features).Result;
        //    return analyzeResult.ToString();
        //}

        public static async Task<IList<TextRecognitionResult>> GetTextAsync(Stream imageToAnalyze)
        {
            BatchReadFileInStreamHeaders textHeaders = await _computerVisionClient.BatchReadFileInStreamAsync(imageToAnalyze);
            string operationLocation = textHeaders.OperationLocation;
            string operationId = operationLocation.Substring(operationLocation.Length - NumberOfCharsInOperationId);

            ReadOperationResult result = await _computerVisionClient.GetReadOperationResultAsync(operationId);

            int i = 0;
            int maxRetries = 10;
            while ((result.Status == TextOperationStatusCodes.Running || result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
            {
                await Task.Delay(1000);
                result = await _computerVisionClient.GetReadOperationResultAsync(operationId);
            }

            IList<TextRecognitionResult> recResults = result.RecognitionResults;
            return recResults;
        }

        private static string LogTextResult(IList<TextRecognitionResult> results)
        {
            var stringBuilder = new StringBuilder();
            if (results != null && results.Any())
            {
                stringBuilder.AppendLine();

                foreach (TextRecognitionResult result in results)
                {
                    foreach (Line line in result.Lines)
                    {
                        foreach (Word word in line.Words)
                        {
                            stringBuilder.Append(word.Text);
                            stringBuilder.Append(" ");
                        }
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.AppendLine();
                }
            }
            return stringBuilder.ToString();
        }
    }
}