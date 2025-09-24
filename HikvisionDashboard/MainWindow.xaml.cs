using ANPRDashboard.Helpers;
using ANPRViewer.Controls;
using ANPRViewer.Models;
using ANPRViewer.Services;
using ANPRViewer.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;


namespace HikvisionDashboard
{
    public partial class MainWindow : Window
    {
        private readonly ConfigurationService _configService;
        private readonly ApiService _apiService;
        private int _capturesCount = 0;

        // 🔹 Colección para el panel lateral
        private readonly ObservableCollection<EventItemVm> _recentEvents = new();

        public ObservableCollection<EventItemVm> RecentEvents => _recentEvents;
        public MainWindow()
        {
            InitializeComponent();
            _configService = new ConfigurationService();
            _apiService = new ApiService(_configService.Settings.ApiUrl);

            _apiService.DetectionReceived += OnDetectionReceived;
            _apiService.ErrorOccurred += OnApiError;

            DataContext = this;
        }// Necesario para el binding del XAML


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Iniciando aplicación...");

                LoadCameras();

                var connected = await _apiService.TestConnectionAsync();
                ApiStatusIndicator.Fill = new SolidColorBrush(connected ? Colors.Green : Colors.Red);

                if (connected)
                    _apiService.StartPolling();

                UpdateCaptureStatus();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al cargar MainWindow", ex);
            }
            // Iniciar el .exe externo al cargar el dashboard
            StartExternalApp();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _apiService?.Dispose();
        }

        // 👇 Aquí ajustas tu grid de cámaras como ya lo tienes implementado
        private void LoadCameras()
        {
            // ...
        }

        /// <summary>
        /// Agrega un evento al panel lateral (solo Camara#X).
        /// </summary>
        private void AddRecentEvent(AnprDetection detection)
        {
            // Solo mostrar fotos de Camara#X
            if (!(detection.ImageUrl.Contains("Camara1X") ||
                  detection.ImageUrl.Contains("Camara2X") ||
                  detection.ImageUrl.Contains("Camara3X") ||
                  detection.ImageUrl.Contains("Camara4X")))
                return;

            // Evitar duplicados
            var key = $"{detection.Placa}-{detection.AbsTime}";
            if (RecentEvents.Any(e => e.UniqueKey == key))
                return;

            // Crear item
            var fullUrl = $"{_configService.Settings.ApiUrl}{detection.ImageUrl}";
            var item = new EventItemVm(detection, fullUrl);

            RecentEvents.Insert(0, item);

            // Mantener máximo 15
            if (RecentEvents.Count > 15)
                RecentEvents.RemoveAt(RecentEvents.Count - 1);

            // 🔹 Actualizar contador con eventos válidos
            _capturesCount = RecentEvents.Count;
            UpdateCaptureStatus();
        }

        //eventos de la api
        private void OnDetectionReceived(AnprDetection detection)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // ❌ Ignorar placas vacías o desconocidas
                    if (string.IsNullOrWhiteSpace(detection.Placa) ||
                        detection.Placa.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    // ✅ Solo mostrar capturas de Camara#X
                    if (detection.ImageUrl.Contains("Camara1X") ||
                        detection.ImageUrl.Contains("Camara2X") ||
                        detection.ImageUrl.Contains("Camara3X") ||
                        detection.ImageUrl.Contains("Camara4X"))
                    {
                        var vm = new EventItemVm(detection, _configService.Settings.ApiBaseUrl);

                        // Evitar duplicados (Placa + Hora + Camara)
                        bool exists = _recentEvents.Any(e =>
                            e.Placa == vm.Placa &&
                            e.Hora == vm.Hora &&
                            e.Camara == vm.Camara);

                        if (!exists)
                        {
                            // Insertar al inicio
                            _recentEvents.Insert(0, vm);

                            // Mantener máximo 10
                            while (_recentEvents.Count > 10)
                                _recentEvents.RemoveAt(_recentEvents.Count - 1);

                            // ✅ Solo aquí incrementar el contador
                            _capturesCount = _recentEvents.Count;
                            CaptureStatusText.Text = $"{_capturesCount} capturas obtenidas";
                        }
                    }

                    // 🔹 Actualizar Entrada / Salida (siempre)
                    UpdateEntradaSalida(detection);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error en OnDetectionReceived", ex);
                }
            });
        }


        private void OnApiError(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApiStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                Logger.Error("Error API: " + message);
            }));
        }


        private void UpdateCaptureStatus()
        {
            CaptureStatusText.Text = $"{_capturesCount} capturas obtenidas";

        }
        private void UpdateEntradaSalida(AnprDetection detection)
        {
            var fullUrl = $"{_configService.Settings.ApiBaseUrl}{detection.ImageUrl}";

            // Entrada: Camara1 o Camara3
            if (detection.ImageUrl.Contains("Camara1/") || detection.ImageUrl.Contains("Camara3/"))
            {
                EntradaImage.Source = new BitmapImage(new Uri(fullUrl, UriKind.Absolute));
                EntradaPlateText.Text = detection.Placa;
                EntradaDateText.Text = detection.Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
            }

            // Salida: Camara2 o Camara4
            if (detection.ImageUrl.Contains("Camara2/") || detection.ImageUrl.Contains("Camara4/"))
            {
                SalidaImage.Source = new BitmapImage(new Uri(fullUrl, UriKind.Absolute));
                SalidaPlateText.Text = detection.Placa;
                SalidaDateText.Text = detection.Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }

        // 👇 Evento del botón "Iniciar App Externa"
        private void StartExternalApp()
            {
                try
                {
                    var exePath = _configService.Settings.ExternalExecutablePath;

                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    {
                        MessageBox.Show("La ruta del ejecutable no es válida.",
                                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Nombre del proceso (sin extensión .exe)
                    var processName = Path.GetFileNameWithoutExtension(exePath);

                    // Verificar si ya está corriendo
                    var running = Process.GetProcessesByName(processName).Any();
                    if (running)
                    {
                        MessageBox.Show("El Plate ya está iniciado.",
                                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);

                        PlateStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                        return;
                    }

                    // Iniciar el proceso
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true // Necesario para apps .NET/Win32
                    });

                    PlateStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error iniciando el Plate: {ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    PlateStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                }
        }


     }
}

