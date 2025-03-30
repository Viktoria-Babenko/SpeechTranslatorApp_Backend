using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SpeechTranslatorApp.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SpeechController : ControllerBase
	{
		private readonly IConfiguration _config;
		public SpeechController(IConfiguration config)
		{
			_config = config;
		}

		[HttpPost]
		public async Task<IActionResult> Post()
		{
			var speechKey = _config["AzureSpeech:Key"];
			var speechRegion = _config["AzureSpeech:Region"];
			var translatorKey = _config["AzureTranslator:Key"];
			var translatorRegion = _config["AzureTranslator:Region"];

			var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
			speechConfig.SpeechRecognitionLanguage = "ru-RU";

			using var audioStream = Request.Body;
			using var memoryStream = new MemoryStream();
			await audioStream.CopyToAsync(memoryStream);
			memoryStream.Position = 0;

			var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
			var audioInput = AudioConfig.FromStreamInput(new BinaryAudioStreamReader(memoryStream), audioFormat);

			using var recognizer = new SpeechRecognizer(speechConfig, audioInput);
			var result = await recognizer.RecognizeOnceAsync();

			if (result.Reason != ResultReason.RecognizedSpeech)
				return BadRequest("Не удалось распознать речь");

			var recognizedText = result.Text;
			var translatedText = await TranslateText(recognizedText, translatorKey, translatorRegion);

			return Ok(new
			{
				text = recognizedText,
				translation = translatedText
			});
		}

		private async Task<string> TranslateText(string text, string key, string region)
		{
			var endpoint = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=en";
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
			client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", region);

			var body = new[] { new { Text = text } };
			var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
			var response = await client.PostAsync(endpoint, content);
			var json = await response.Content.ReadAsStringAsync();
			return System.Text.Json.JsonDocument.Parse(json).RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString();
		}
	}
}
