using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ANPRViewer.Models
{
    public class AppSettings
    {
        [JsonPropertyName("ApiUrl")]
        public string ApiUrl { get; set; } = "";

        [JsonPropertyName("ApiBaseUrl")]
        public string ApiBaseUrl { get; set; } = "";

        [JsonPropertyName("PollingIntervalSeconds")]
        public int PollingIntervalSeconds { get; set; } = 5;

        [JsonPropertyName("MaxStreams")]
        public int MaxStreams { get; set; } = 2;

        [JsonPropertyName("ExternalExecutablePath")]
        public string ExternalExecutablePath { get; set; } = "";

        [JsonPropertyName("Cameras")]
        public List<ANPRCamera> Cameras { get; set; } = new();
    }
}
