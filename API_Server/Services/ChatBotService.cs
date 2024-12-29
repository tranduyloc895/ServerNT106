
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using API_Server.Models;
using static API_Server.Models.ChatBot;

namespace API_Server.Services
{
    public class ChatBotService 
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string? ApiKey;
        private string? ApiUrl;

        public ChatBotService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            ApiKey = _configuration["GeminiApi:ApiKey"];
            ApiUrl = _configuration["GeminiApi:BaseUrl"];

            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiUrl))
            {
                throw new ArgumentNullException("Gemini API Key or Base URL is not configured.");
            }
        }

        public async Task<string> GetResponseAsync(string message)
        {
            var payload = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                    new { text = message }
                    }
                }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{ApiUrl}?key={ApiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return $"Error communicating with Gemini API. Status: {response.StatusCode}, Details: {errorDetails}";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<GeminiResponse>(responseBody);

                return responseObject?.Candidates?[0]?.Content?.Parts?[0]?.Text
                    ?? "No response from Gemini.";
            }
            catch (HttpRequestException ex)
            {
                return $"Request exception: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Unexpected error: {ex.Message}";
            }
        }

        
    }
}
