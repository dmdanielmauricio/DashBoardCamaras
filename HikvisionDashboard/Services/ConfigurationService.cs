using System;
using System.IO;
using System.Text.Json;
using ANPRViewer.Models;

namespace ANPRViewer.Services
{
    public class ConfigurationService
    {
        private readonly string _configFilePath = "appsettings.json";
        public AppSettings Settings { get; private set; } = new AppSettings();


        public ConfigurationService()
        {
            LoadConfiguration();
        }

        public void LoadConfiguration()
        {
            if (!File.Exists(_configFilePath))
            {
                Settings = new AppSettings();
                return;
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Settings = JsonSerializer.Deserialize<AppSettings>(json, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando configuración: {ex.Message}");
                Settings = new AppSettings();
            }
        }
    }
}
