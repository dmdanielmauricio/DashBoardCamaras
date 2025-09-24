using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ANPRViewer.Models;

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

                // Reemplazamos la configuración de GStreamer con la de OpenCV
                _capture = new VideoCapture(rtspUrl);

                // Opción para forzar la decodificación por hardware si está disponible
                // _capture.Set(VideoCaptureProperties.HwAccel, 1);
                // _capture.Set(VideoCaptureProperties.GstreamerHwAccel, 1);

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
                    QualityText.Text = "Conectado";
                    _isPlaying = true;
                    PlayPauseButton.Content = "⏸";
                });

                ConnectionStatusChanged?.Invoke(_camera!, true);

                using var frame = new Mat();

                while (!token.IsCancellationRequested && _capture.IsOpened() && !_isDisposed)
                {
                    if (!_capture.Read(frame) || frame.Empty())
                    {
                        await Task.Delay(10, token);
                        continue;
                    }

                    var image = frame.ToWriteableBitmap();

                    if (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (!_isDisposed)
                            {
                                CameraImage.Source = image;
                                NoVideoOverlay.Visibility = Visibility.Collapsed;
                            }
                        });
                    }

                    await Task.Delay(1, token);
                }
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
                if (!_isDisposed)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _isPlaying = false;
                        PlayPauseButton.Content = "▶";
                        QualityText.Text = "Desconectado";
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
