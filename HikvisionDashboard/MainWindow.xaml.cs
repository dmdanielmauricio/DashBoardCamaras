using ANPRDashboard.Helpers;
using ANPRViewer.Controls;
using ANPRViewer.Models;
using ANPRViewer.Services;
using ANPRViewer.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HikvisionDashboard
{
    public partial class MainWindow : Window
    {
        private readonly ConfigurationService _configService;
        private readonly ApiService _apiService;
        private int _capturesCount = 0;
        private bool _apiErrorShown = false;   // 🔹 flag para no spamear mensajes

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
            _apiService.ApiConnectionChanged += OnApiConnectionChanged;

            DataContext = this;
        }

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
            try
            {
                CameraGrid.Children.Clear();
                CameraGrid.RowDefinitions.Clear();
                CameraGrid.ColumnDefinitions.Clear();

                // ✅ Filtrar cámaras habilitadas desde appsettings.json
                var enabledCameras = _configService.Settings.Cameras
                    .Where(c => c.Enabled)
                    .Take(_configService.Settings.MaxStreams)
                    .ToList();

                int count = enabledCameras.Count;
                if (count == 0)
                {
                    CameraStatusText.Text = "0/0 Cámaras habilitadas";
                    return;
                }

                // ✅ Calcular distribución de filas y columnas (grid cuadrado)
                int rows = (int)Math.Ceiling(Math.Sqrt(count));
                int cols = (int)Math.Ceiling((double)count / rows);

                for (int r = 0; r < rows; r++)
                    CameraGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                for (int c = 0; c < cols; c++)
                    CameraGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // ✅ Insertar dinámicamente los controles de cámara
                int index = 0;
                foreach (var cam in enabledCameras)
                {
                    var control = new CameraControlOpenCV();
                    control.SetCamera(cam);

                    int row = index / cols;
                    int col = index % cols;

                    Grid.SetRow(control, row);
                    Grid.SetColumn(control, col);

                    CameraGrid.Children.Add(control);
                    index++;
                }

                CameraStatusText.Text = $"{count}/{_configService.Settings.Cameras.Count} Cámaras habilitadas";
            }
            catch (Exception ex)
            {
                Logger.Error("Error cargando cámaras", ex);
            }
        }


        /// <summary>
        /// Evento recibido desde la API..
        /// </summary>
        private void OnDetectionReceived(AnprDetection detection)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(detection.Placa) ||
                        detection.Placa.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                        return;

                    if (detection.ImageUrl.Contains("Camara1X") ||
                        detection.ImageUrl.Contains("Camara2X") ||
                        detection.ImageUrl.Contains("Camara3X") ||
                        detection.ImageUrl.Contains("Camara4X"))
                    {
                        var vm = new EventItemVm(detection, _configService.Settings.ApiBaseUrl);

                        // Evitar duplicados (placa + hora + camara)
                        bool exists = _recentEvents.Any(e =>
                            e.Placa == vm.Placa &&
                            e.Hora == vm.Hora &&
                            e.Camara == vm.Camara);

                        if (!exists)
                        {
                            _recentEvents.Insert(0, vm);

                            while (_recentEvents.Count > 10)
                                _recentEvents.RemoveAt(_recentEvents.Count - 1);

                            _capturesCount++;
                            CaptureStatusText.Text = $"{_capturesCount} capturas obtenidas";
                        }
                    }

                    UpdateEntradaSalida(detection);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error en OnDetectionReceived", ex);
                }
            });
        }


        // 🔹 Error controlado: solo muestra un mensaje una vez hasta que reconecte
        private void OnApiError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                // Cambiar a rojo
                ApiStatusIndicator.Fill = new SolidColorBrush(Colors.Red);

                // Mostrar error (una sola vez por desconexión si quieres)
                MessageBox.Show($"Error al consultar la API:\n{message}",
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);

                Logger.Error("Error API: " + message);
            });
        }

        private void OnApiConnectionChanged(bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                ApiStatusIndicator.Fill = new SolidColorBrush(isConnected ? Colors.Green : Colors.Red);

                if (isConnected)
                {
                    // 🔹 Resetear flag → permitirá mostrar mensaje si vuelve a fallar
                    _apiErrorShown = false;
                }
            });
        }

        private void UpdateCaptureStatus()
        {
            CaptureStatusText.Text = $"{_capturesCount} capturas obtenidas";
        }

        private void UpdateEntradaSalida(AnprDetection detection)
        {
            var fullUrl = $"{_configService.Settings.ApiBaseUrl}{detection.ImageUrl}";

            if (detection.ImageUrl.Contains("Camara1/") || detection.ImageUrl.Contains("Camara3/"))
            {
                EntradaImage.Source = new BitmapImage(new Uri(fullUrl, UriKind.Absolute));
                EntradaPlateText.Text = detection.Placa;
                EntradaDateText.Text = detection.Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
            }

            if (detection.ImageUrl.Contains("Camara2/") || detection.ImageUrl.Contains("Camara4/"))
            {
                SalidaImage.Source = new BitmapImage(new Uri(fullUrl, UriKind.Absolute));
                SalidaPlateText.Text = detection.Placa;
                SalidaDateText.Text = detection.Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }

        // 👇 Iniciar el .exe externo
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

                var processName = Path.GetFileNameWithoutExtension(exePath);
                var running = Process.GetProcessesByName(processName).Any();
                if (running)
                {
                    MessageBox.Show("El Plate ya está iniciado.",
                                    "Información", MessageBoxButton.OK, MessageBoxImage.Information);

                    PlateStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
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

