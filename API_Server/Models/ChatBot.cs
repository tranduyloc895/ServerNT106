using Newtonsoft.Json;

namespace API_Server.Models
{
    public class ChatBot
    {
        public class GeminiResponse
        {
            [JsonProperty("candidates")]
            public Candidate[] Candidates { get; set; }

            [JsonProperty("usageMetadata")]
            public UsageMetadata UsageMetadata { get; set; }

            [JsonProperty("modelVersion")]
            public string ModelVersion { get; set; }
        }

        public class Candidate
        {
            [JsonProperty("content")]
            public Content Content { get; set; }

            [JsonProperty("finishReason")]
            public string FinishReason { get; set; }

            [JsonProperty("avgLogprobs")]
            public double AvgLogprobs { get; set; }
        }

        public class Content
        {
            [JsonProperty("parts")]
            public Part[] Parts { get; set; }

            [JsonProperty("role")]
            public string Role { get; set; }
        }

        public class Part
        {
            [JsonProperty("text")]
            public string Text { get; set; }
        }

        public class UsageMetadata
        {
            [JsonProperty("promptTokenCount")]
            public int PromptTokenCount { get; set; }

            [JsonProperty("candidatesTokenCount")]
            public int CandidatesTokenCount { get; set; }

            [JsonProperty("totalTokenCount")]
            public int TotalTokenCount { get; set; }
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }

    }
}
