using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using ANPRDashboard.Helpers;


namespace ANPRDashboard
{
    public static class ConfigHelper
    {
        public static ConfigModel LoadConfig(string path = "appsettings.json")
        {
            try
            {
                if (!File.Exists(path))
                {
                    Logger.Error($"No se encontró el archivo de configuración en: {path}");
                    return new ConfigModel { Cameras = new List<ANPRViewer.Models.ANPRCamera>() };
                }

                string json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<ConfigModel>(json);

                if (config == null)
                {
                    Logger.Error("El archivo de configuración está vacío o es inválido.");
                    return new ConfigModel { Cameras = new List<ANPRViewer.Models.ANPRCamera>() };
                }

                // 🔄 Reemplazar YYYYMMDD en rutas de cámaras
                string today = DateTime.Now.ToString("yyyyMMdd");
                foreach (var cam in config.Cameras)
                {
                    if (!string.IsNullOrEmpty(cam.ImagePath))
                    {
                        cam.ImagePath = cam.ImagePath.Replace("YYYYMMDD", today);
                        Logger.Info($"Ruta configurada para {cam.Name}: {cam.ImagePath}");
                    }

                    if (string.IsNullOrEmpty(cam.RtspUrl))
                        Logger.Error($"La cámara {cam.Name} no tiene RtspUrl configurado.");
                }

                Logger.Info("Archivo de configuración cargado correctamente.");
                return config;
            }
            catch (Exception ex)
            {
                Logger.Error("Error cargando configuración", ex);
                return new ConfigModel { Cameras = new List<ANPRViewer.Models.ANPRCamera>() };
            }
        }
    }
}
