using ANPRViewer.Models;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ANPRViewer.Controls
{
    public partial class CameraControlOpenCV : UserControl, IDisposable
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cancellationTokenSource;
        private ANPRCamera? _camera;
        private bool _isPlaying = false;
        private bool _isDisposed = false;

        public event Action<string>? ErrorOccurred;
        public event Action<ANPRCamera, bool>? ConnectionStatusChanged;

        public CameraControlOpenCV()
        {
            InitializeComponent();
        }

        public void SetCamera(ANPRCamera camera)
        {
            _camera = camera;

            Dispatcher.Invoke(() =>
            {
                CameraNameText.Text = camera.Name;
                OverlayText.Text = "Conectando...";
                NoVideoOverlay.Visibility = Visibility.Visible;
                StatusIndicator.Fill = Brushes.Orange;
            });

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
            _ = StartCameraAsync(camera.RtspUrl, _cancellationTokenSource.Token);
        }
        //streaming de video
        private async Task StartCameraAsync(string rtspUrl, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(rtspUrl))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowError("URL RTSP no configurada");
                    StatusIndicator.Fill = Brushes.Red;
                });
                return;
            }

            try
            {
                _capture?.Release();
                _capture?.Dispose();

                // 🔹 Pipeline GStreamer optimizado para baja latencia
                string pipeline = $"rtspsrc location={rtspUrl} latency=0 protocols=tcp drop-on-latency=true " +
                                 $"! queue max-size-buffers=1 leaky=downstream " +
                                 $"! rtph264depay ! h264parse ! avdec_h264 max-threads=2 " +
                                 $"! videoconvert ! video/x-raw,format=BGR " +
                                 $"! appsink drop=true max-buffers=1 emit-signals=true sync=false";

                _capture = new VideoCapture(pipeline, VideoCaptureAPIs.GSTREAMER);

                // 🔹 Fallback a FFMPEG con configuración de baja latencia
                if (!_capture.IsOpened())
                {
                    _capture?.Release();
                    _capture?.Dispose();
                    _capture = new VideoCapture(rtspUrl, VideoCaptureAPIs.FFMPEG);

                    // Configurar FFMPEG para baja latencia
                    _capture.Set(VideoCaptureProperties.BufferSize, 1);
                    _capture.Set(VideoCaptureProperties.FourCC, VideoWriter.FourCC('H', '2', '6', '4'));
                }

                if (!_capture.IsOpened())
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ShowError($"No se pudo conectar a: {_camera?.Name}");
                        StatusIndicator.Fill = Brushes.Red;
                    });
                    ErrorOccurred?.Invoke($"No se pudo abrir cámara: {_camera?.Name}");
                    ConnectionStatusChanged?.Invoke(_camera!, false);
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    StatusIndicator.Fill = Brushes.Green;
                    QualityText.Text = "Conectado (GStreamer)";
                    NoVideoOverlay.Visibility = Visibility.Collapsed;
                });

                ConnectionStatusChanged?.Invoke(_camera!, true);

                // 🔹 Proceso de frames en thread separado para máximo rendimiento
                await Task.Run(async () =>
                {
                    using var frame = new Mat();
                    var frameCount = 0;
                    var lastUpdate = DateTime.Now;

                    while (!token.IsCancellationRequested && _capture.IsOpened() && !_isDisposed)
                    {
                        try
                        {
                            // Leer frame (esto es bloqueante pero eficiente)
                            if (!_capture.Grab())
                            {
                                await Task.Delay(5, token);
                                continue;
                            }

                            if (!_capture.Retrieve(frame) || frame.Empty())
                            {
                                continue;
                            }

                            frameCount++;

                            // 🔹 Convertir a WriteableBitmap de forma eficiente
                            var image = frame.ToWriteableBitmap();
                            image.Freeze(); // Freeze para poder usar en otro thread

                            // 🔹 Actualizar UI con prioridad alta pero sin bloquear
                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (!_isDisposed)
                                {
                                    CameraImage.Source = image;

                                    // Actualizar FPS cada segundo
                                    var now = DateTime.Now;
                                    if ((now - lastUpdate).TotalSeconds >= 1.0)
                                    {
                                        var fps = frameCount / (now - lastUpdate).TotalSeconds;
                                        QualityText.Text = $"Conectado - {fps:F1} FPS";
                                        frameCount = 0;
                                        lastUpdate = now;
                                    }
                                }
                            }, DispatcherPriority.Send); // Send en vez de Render para mayor prioridad

                            // 🔹 Pequeño yield para no saturar el CPU
                            await Task.Yield();
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            Debug.WriteLine($"Error procesando frame: {ex.Message}");
                            await Task.Delay(10, token);
                        }
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Stream cancelado para cámara: {_camera?.Name}");
            }
            catch (Exception ex)
            {
                if (!_isDisposed && !token.IsCancellationRequested)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ShowError($"Error: {ex.Message}");
                        StatusIndicator.Fill = Brushes.Orange;
                    });
                    ErrorOccurred?.Invoke($"Error en cámara {_camera?.Name}: {ex.Message}");
                    ConnectionStatusChanged?.Invoke(_camera!, false);
                }
            }
            finally
            {
                _capture?.Release();
                _capture?.Dispose();

                if (!_isDisposed)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        QualityText.Text = "Desconectado";
                        StatusIndicator.Fill = Brushes.Gray;
                        NoVideoOverlay.Visibility = Visibility.Visible;
                    });
                }
            }
        }



        private void ShowError(string message)
        {
            ErrorText.Text = message;
            OverlayText.Text = "Error de conexión";
            NoVideoOverlay.Visibility = Visibility.Visible;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_camera == null) return;

            if (_isPlaying)
            {
                _cancellationTokenSource?.Cancel();
                StatusIndicator.Fill = Brushes.Gray;
                QualityText.Text = "Pausado";
            }
            else
            {
                SetCamera(_camera);
            }
        }

        private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_camera != null)
            {
                _cancellationTokenSource?.Cancel();
                await Task.Delay(500);
                SetCamera(_camera);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            try
            {
                _cancellationTokenSource?.Cancel();
                _capture?.Release();
                _capture?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing camera control: {ex.Message}");
            }
        }
    }
}
