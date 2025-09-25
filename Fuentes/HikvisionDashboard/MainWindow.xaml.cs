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
using System.Net.Http;
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

                var enabledCameras = _configService.Settings.Cameras
                    .Where(c => c.Enabled)
                    .ToList();

                // Limitar por MaxStreams
                int maxStreams = _configService.Settings.MaxStreams;
                if (maxStreams <= 0) maxStreams = enabledCameras.Count; // fallback
                var cams = enabledCameras.Take(maxStreams).ToList();

                int count = cams.Count;
                if (count == 0)
                {
                    CameraStatusText.Text = "0/0 Cámaras habilitadas";
                    return;
                }

                // 👉 columnas = MaxStreams (o menos si hay menos cámaras)
                int cols = Math.Max(1, Math.Min(maxStreams, count));
                int rows = (int)Math.Ceiling(count / (double)cols);

                for (int r = 0; r < rows; r++)
                    CameraGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                for (int c = 0; c < cols; c++)
                    CameraGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                for (int i = 0; i < count; i++)
                {
                    var control = new CameraControlOpenCV();
                    control.SetCamera(cams[i]);

                    int row = i / cols;
                    int col = i % cols;

                    Grid.SetRow(control, row);
                    Grid.SetColumn(control, col);
                    control.Margin = new Thickness(6, 0, 6, 6);

                    CameraGrid.Children.Add(control);
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
      
        // ===== Helpers =====
        private static int? ParseLane(string? s)
            => int.TryParse(s, out var n) ? n : null;

        private static int? CameraFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var u = url.Replace('\\', '/').ToUpperInvariant();
            for (int i = 1; i <= 8; i++)
                if (u.Contains($"/CAMARA{i}X/") || u.Contains($"/CAMARA{i}/"))
                    return i;
            return null;
        }

        private static bool IsProcessed(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.Replace('\\', '/').ToUpperInvariant().Contains("/Procesado/");
        }

        private static bool IsX(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var u = url.Replace('\\', '/').ToUpperInvariant();
            return u.Contains("/Camara1X/") || u.Contains("/Camara2X/")
                || u.Contains("/Camara3X/") || u.Contains("/Camara4X/");
        }

        private static string? BuildProcessedFromX(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var u = url.Replace('\\', '/');
            var parts = u.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return null;

            var camPart = parts[1];  // Camara2X
            if (!camPart.EndsWith("X", StringComparison.OrdinalIgnoreCase)) return null;

            var cam = camPart[..^1];   // Camara2
            var date = parts[2];
            var file = parts[^1];
            return "/" + string.Join('/', parts[0], cam, date, "Procesado", file);
        }


        private string MakeFullUrl(string relativeOrAbsolute)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return "";
            if (relativeOrAbsolute.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return relativeOrAbsolute;

            var baseUrl = _configService.Settings.ApiBaseUrl?.TrimEnd('/') ?? "";
            var path = relativeOrAbsolute.TrimStart('/');
            return $"{baseUrl}/{path}";
        }

        private ImageSource? LoadHttpImage(string url)
        {
            try
            {
                using var client = new HttpClient();
                var data = client.GetByteArrayAsync(url).Result;

                using var ms = new MemoryStream(data);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze(); // 🔹 Congela el objeto para que sea seguro en cualquier hilo
                return bmp;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error cargando imagen desde {url}: {ex.Message}");
                return null;
            }
        }

        private void UpdateEntradaSalida(AnprDetection d)
        {
            try
            {
                int? lane = ParseLane(d.Lane);
                int? cam = CameraFromUrl(d.ImageUrl);

                bool esEntrada = (lane is 1 or 3) || (cam is 1 or 3);
                bool esSalida = (lane is 2 or 4) || (cam is 2 or 4);
                if (!esEntrada && !esSalida) return;

                // 2) Seleccionar URL candidata
                var urls = new List<string>();
                if (IsProcessed(d.ImageUrl))
                {
                    urls.Add(d.ImageUrl!);
                }
                else if (IsX(d.ImageUrl))
                {
                    var proc = BuildProcessedFromX(d.ImageUrl!);
                    if (proc != null) urls.Add(proc);
                    urls.Add(d.ImageUrl!);
                }
                else
                {
                    urls.Add(d.ImageUrl!);
                }

                // 3) Intentar cargar imagen
                ImageSource? img = null;
                string? elegido = null;
                foreach (var u in urls)
                {
                    var abs = MakeFullUrl(u);
                    img = LoadHttpImage(abs);
                    if (img != null) { elegido = abs; break; }
                }
                if (img == null) return;

                var ts = d.Timestamp == default ? DateTime.Now : d.Timestamp;

                // 🔹 Actualización de UI en el hilo correcto
                Dispatcher.Invoke(() =>
                {
                    if (esEntrada)
                    {
                        EntradaImage.Source = img;
                        EntradaPlateText.Text = d.Placa;
                        EntradaDateText.Text = ts.ToString("dd/MM/yyyy HH:mm:ss");
                        Logger.Info($"ENTRADA -> {d.Placa} | {elegido}");
                    }
                    else if (esSalida)
                    {
                        SalidaImage.Source = img;
                        SalidaPlateText.Text = d.Placa;
                        SalidaDateText.Text = ts.ToString("dd/MM/yyyy HH:mm:ss");
                        Logger.Info($"SALIDA -> {d.Placa} | {elegido}");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error en UpdateEntradaSalida: {ex.Message}");
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

