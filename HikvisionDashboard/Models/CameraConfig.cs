using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ANPRViewer.Models
{
    public class CameraConfig
    {
        [JsonPropertyName("Cameras")]
        public List<ANPRCamera> Cameras { get; set; } = new();

        [JsonPropertyName("MaxStreams")]
        public int MaxStreams { get; set; } = 6;

        [JsonPropertyName("ApiUrl")]
        public string ApiUrl { get; set; } = string.Empty;
    }
}