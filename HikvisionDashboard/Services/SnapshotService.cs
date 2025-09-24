using System.IO;
using System.Linq;
using ANPRViewer.Models;

namespace ANPRViewer.Services
{
    public static class SnapshotService
    {
        public static void AssignSnapshot(ANPRCamera camera)
        {
            if (string.IsNullOrEmpty(camera.ImagePath))
            {
                camera.ImagePath = null;
                return;
            }

            // Reemplazar YYYYMMDD con la fecha actual
            var today = System.DateTime.Now.ToString("yyyyMMdd");
            var baseFolder = camera.ImagePath.Replace("YYYYMMDD", today);

            if (!Directory.Exists(baseFolder))
            {
                camera.ImagePath = null;
                return;
            }

            // Buscar la última imagen en la carpeta del día
            var latestImage = Directory.GetFiles(baseFolder, "*.jpg")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            camera.ImagePath = latestImage; // puede quedar en null si no hay imágenes
        }

        public static void AssignSnapshots(IEnumerable<ANPRCamera> cameras)
        {
            foreach (var camera in cameras)
            {
                AssignSnapshot(camera);
            }
        }
    }
}
