using ANPRViewer.Models;
using System.Collections.Generic;

namespace ANPRDashboard
{
    public class ConfigModel
    {
        public List<ANPRViewer.Models.ANPRCamera> Cameras { get; set; }
        public int MaxStreams { get; set; }
        public string ApiUrl { get; set; }
    }

    public class CameraConfig
    {
        public string Name { get; set; } = "";
        public string ImagePath { get; set; } = "";
    }
}

