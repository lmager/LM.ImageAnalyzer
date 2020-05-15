using Microsoft.WindowsAzure.Storage.Table;

namespace LM.ImageAnalyzer.Shared.Entities
{
    public class AnalyzeResult : TableEntity
    {
        public string ImageAnalysisJson { get; set; }

        public string TextResult { get; set; }
    }
}