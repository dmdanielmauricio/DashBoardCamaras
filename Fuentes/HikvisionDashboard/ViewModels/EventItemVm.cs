using ANPRViewer.Models;

namespace ANPRViewer.ViewModels
{
    public class EventItemVm
    {
        public string Placa { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Camara { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string FullImageUrl { get; set; } = string.Empty;

        public string UniqueKey => $"{Placa}-{Hora}";

        // 🔹 Constructor vacío para que puedas usar inicializadores
        public EventItemVm() { }

        // 🔹 Constructor opcional si quieres construir directo desde AnprDetection
        public EventItemVm(AnprDetection detection, string baseUrl)
        {
            Placa = detection.Placa;
            Hora = detection.Timestamp.ToString("HH:mm:ss");
            Camara = $"Camara{detection.Lane}";
            ImageUrl = detection.ImageUrl;
            FullImageUrl = $"{baseUrl}{detection.ImageUrl}";
        }
    }
}


