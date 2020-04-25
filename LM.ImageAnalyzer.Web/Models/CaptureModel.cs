using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LM.ImageAnalyzer.Web.Models
{
    public class CaptureModel
    {
        [Required]
        public IFormFile ImageToAnalyze { get; set; }
    }
}