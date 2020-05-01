using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using LM.ImageAnalyzer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LM.ImageAnalyzer.Web.Controllers
{
    public class AnalyzeController : Controller
    {
        private readonly IOptions<AnalyserFunctionSettings> _configAnalyzerFunction;
        private readonly HttpClient _client;

        public AnalyzeController(IOptions<AnalyserFunctionSettings> configAnalyzerFunction, HttpClient client)
        {
            _configAnalyzerFunction = configAnalyzerFunction;
            _client = client;
        }

        [Route("")]
        public IActionResult Capture()
        {
            return View();
        }

        [Route("Capture")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capture(CaptureModel model)
        {
            if (ModelState.IsValid)
            {
                using var memoryStream = new MemoryStream();
                await model.ImageToAnalyze.CopyToAsync(memoryStream);
                string imageId = await SendImageToStorage(memoryStream.ToArray()).ConfigureAwait(false);
                imageId = imageId.Replace("\"", "");

                ViewData["ImageName"] = imageId;
                return View("Result");
            }

            return BadRequest();
        }

        public IActionResult Result(string imageId)
        {
            ViewData["ImageName"] = imageId;
            return View();
        }

        private async Task<string> SendImageToStorage(byte[] imageData)
        {
            using (var content = new ByteArrayContent(imageData))
            {
                content.Headers.Add("x-functions-key", _configAnalyzerFunction.Value.SaveImageKey);

                string functionUrl = $"{_configAnalyzerFunction.Value.BaseEndPoint.Trim('/')}/StoreImage";
                HttpResponseMessage result = await _client.PostAsync(functionUrl, content).ConfigureAwait(false);

                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            return string.Empty;
        }
    }
}