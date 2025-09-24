using System.Text.Json.Serialization;

namespace ANPRViewer.Models
{
    public class ANPRCamera
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("RtspUrl")]
        public string RtspUrl { get; set; } = string.Empty;

        [JsonPropertyName("ImagePath")]
        public string? ImagePath { get; set; }

        public bool Enabled { get; set; }
    }
}