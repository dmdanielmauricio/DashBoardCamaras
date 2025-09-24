using ANPRDashboard;
using ANPRViewer.Models;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ANPRDashboard.Helpers;


namespace ANPRViewer.Controls
{
    public partial class CameraControl : UserControl, IDisposable
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private ANPRCamera? _camera;

        // Eventos para comunicar con MainWindow
        public event Action<string>? ErrorOccurred;
        public event Action<ANPRCamera, bool>? ConnectionStatusChanged;

        public CameraControl()
        {
            InitializeComponent();

            // 📌 Definir la ruta de VLC relativa al ejecutable publicado
            string vlcPath = Path.Combine(AppContext.BaseDirectory, "vlc-3.0.21");
            Core.Initialize(vlcPath);
            //string vlcPath = @"C:\Users\ASUS\Desktop\Proyectos C#\HikvisionDashboard\HikvisionDashboard\HikvisionDashboard\bin\Release\net9.0-windows\publish\vlc-3.0.21";
            //Core.Initialize(vlcPath);


            _libVLC = new LibVLC(
                "--verbose=2",
                "--plugin-path=" + Path.Combine(vlcPath, "plugins")
            );

            _mediaPlayer = new MediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer;
        }

        public void SetCamera(ANPRCamera camera)
        {
            _camera = camera;
            PlayStream();
        }

        private void PlayStream()
        {
            try
            {
                if (_camera == null || string.IsNullOrEmpty(_camera.RtspUrl))
                {
                    Logger.Error("La cámara no tiene URL RTSP configurada.");
                    return;
                }

                _mediaPlayer.Stop();
                using var media = new Media(_libVLC, new Uri(_camera.RtspUrl));
                _mediaPlayer.Play(media);

                Logger.Info($"Reproduciendo stream de {_camera.Name} en {_camera.RtspUrl}");
                ConnectionStatusChanged?.Invoke(_camera, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error iniciando stream de {_camera?.Name}", ex);
                if (_camera != null)
                    ConnectionStatusChanged?.Invoke(_camera, false);
            }
        }


        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Pause();
            else
                _mediaPlayer.Play();
        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            PlayStream();
        }

        private void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_camera != null && !string.IsNullOrEmpty(_camera.RtspUrl))
                PlayStream();
        }

        public void Dispose()
        {
            try
            {
                _mediaPlayer?.Dispose();
                _libVLC?.Dispose();
            }
            catch { }
        }
    }
}
