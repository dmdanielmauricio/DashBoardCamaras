using ANPRViewer.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ANPRViewer.Services
{
    public class ApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        // 🔹 Guardar los IDs de detecciones ya procesadas
        private readonly HashSet<string> _processedDetections = new();

        public event Action<AnprDetection>? DetectionReceived;
        public event Action<string>? ErrorOccurred;

        public ApiService(string apiUrl)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10) // Control de timeout
            };
            _apiUrl = apiUrl;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<AnprDetection>> FetchDetectionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<AnprDetection>>(json) ?? new List<AnprDetection>();
            }
            catch (TaskCanceledException)
            {
                ErrorOccurred?.Invoke("Tiempo de espera excedido al consultar la API.");
                return new List<AnprDetection>();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex.Message);
                return new List<AnprDetection>();
            }
        }

        public void StartPolling(int intervalSeconds = 5)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var detections = await FetchDetectionsAsync();

                        foreach (var detection in detections)
                        {
                            // Generamos un ID único (Placa + AbsTime)
                            var detectionId = $"{detection.Placa}-{detection.AbsTime}";

                            // Solo procesamos si no está repetido
                            if (_processedDetections.Add(detectionId))
                            {
                                DetectionReceived?.Invoke(detection);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(ex.Message);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
                }
            });
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
