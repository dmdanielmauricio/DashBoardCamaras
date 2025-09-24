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
        private bool _isConnected = true; // Estado inicial asumido como conectado

        public event Action<AnprDetection>? DetectionReceived;
        public event Action<string>? ErrorOccurred;
        public event Action<bool>? ApiConnectionChanged;
        private readonly HashSet<string> _processedDetections = new();


        public ApiService(string apiUrl)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _apiUrl = apiUrl;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);
                var ok = response.IsSuccessStatusCode;

                UpdateConnectionStatus(ok);
                return ok;
            }
            catch
            {
                UpdateConnectionStatus(false);
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
                ApiConnectionChanged?.Invoke(false);
                ErrorOccurred?.Invoke("Tiempo de espera excedido al consultar la API.");
                return new List<AnprDetection>();
            }
            catch (Exception ex)
            {
                ApiConnectionChanged?.Invoke(false);
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

                        if (detections.Count > 0)
                        {
                            ApiConnectionChanged?.Invoke(true);

                            foreach (var detection in detections)
                            {
                                var detectionId = $"{detection.Placa}-{detection.AbsTime}";
                                if (_processedDetections.Add(detectionId))
                                    DetectionReceived?.Invoke(detection);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ApiConnectionChanged?.Invoke(false);
                        ErrorOccurred?.Invoke(ex.Message);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
                }
            });
        }


        private void UpdateConnectionStatus(bool connected)
        {
            if (_isConnected != connected)
            {
                _isConnected = connected;
                ApiConnectionChanged?.Invoke(connected);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}

