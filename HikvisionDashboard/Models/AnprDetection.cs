using System;
using System.Text.Json.Serialization;

namespace ANPRViewer.Models
{
    public class AnprDetection
    {
        [JsonPropertyName("absTime")]
        public string AbsTime { get; set; } = "";

        [JsonPropertyName("placa")]
        public string Placa { get; set; } = "";

        [JsonPropertyName("lane")]
        public string Lane { get; set; } = "";

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = "";

        public DateTime Timestamp =>
            DateTime.TryParseExact(AbsTime, "yyyyMMddHHmmssfff", null,
                System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.Now;
    }
}
