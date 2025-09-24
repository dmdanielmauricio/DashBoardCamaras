# ANPR Viewer - Sistema de Monitoreo de Placas

Sistema profesional de monitoreo ANPR (Automatic Number Plate Recognition) desarrollado en WPF .NET 9 con integración LibVLC para streaming de cámaras IP.

## 🚀 Características

### ✅ Funcionalidades Implementadas

- **Streaming en Tiempo Real**: Visualización simultánea de hasta 6 cámaras IP via RTSP
- **Detección ANPR**: Integración con API para recepción de detecciones de placas
- **Interface Moderna**: UI responsiva con tema oscuro profesional
- **Gestión de Eventos**: Lista en tiempo real de detecciones con historial
- **Monitoreo de Estado**: Indicadores visuales de conexión de cámaras y API
- **Configuración Flexible**: Archivos JSON para configuración de cámaras y API
- **Manejo de Errores**: Sistema robusto de logging y manejo de excepciones
- **Auto-reconexión**: Reconexión automática de streams perdidos

## 📋 Requisitos del Sistema

- Windows 10/11
- .NET 9 Runtime
- VLC Media Player (redistributable incluido)
- Mínimo 4GB RAM (8GB recomendado)
- Conexión de red estable para streams RTSP

## 🛠️ Instalación

1. **Clonar el repositorio**
   ```bash
   git clone [url-repositorio]
   cd ANPRViewer
   ```

2. **Restaurar paquetes NuGet**
   ```bash
   dotnet restore
   ```

3. **Compilar el proyecto**
   ```bash
   dotnet build --configuration Release
   ```

4. **Ejecutar**
   ```bash
   dotnet run
   ```

## ⚙️ Configuración

### Archivo `config.json`
```json
{
  "cameras": [
    {
      "name": "Camara1",
      "rtsp": "rtsp://usuario:password@ip:puerto/ruta"
    }
  ],
  "maxStreams": 4,
  "apiUrl": "http://servidor:puerto/api/anpr/capturas"
}
```

### Archivo `appsettings.json`
```json
{
  "Cameras": [
    {
      "Name": "Camara1",
      "ImagePath": "C:\\ANPR\\Camara1\\imagen.jpg"
    }
  ]
}
```

## 🏗️ Arquitectura del Proyecto

```
ANPRViewer/
├── Models/                 # Modelos de datos
│   └── Camera.cs
├── Services/               # Servicios de negocio
│   ├── ConfigurationService.cs
│   └── ApiService.cs
├── Controls/               # Controles personalizados
│   ├── CameraControl.xaml
│   └── CameraControl.xaml.cs
├── Assets/                 # Recursos visuales
│   ├── logo1.jpeg
│   ├── fondo.jpeg
│   └── fondo2.jpeg
├── MainWindow.xaml         # Ventana principal
├── MainWindow.xaml.cs
├── App.xaml               # Configuración de aplicación
└── App.xaml.cs
```

## 🔧 Componentes Principales

### 1. **CameraControl**
- Control personalizado para cada cámara
- Integración LibVLC para streaming RTSP
- Indicadores de estado y controles de reproducción
- Auto-reconexión en caso de pérdida de señal

### 2. **ConfigurationService**
- Carga y gestión de archivos de configuración JSON
- Combina información de múltiples fuentes de configuración
- Validación de configuraciones

### 3. **ApiService**
- Cliente HTTP para comunicación con API ANPR
- Polling automático para nuevas detecciones
- Manejo de reconexión y errores de red

### 4. **MainWindow**
- Interface principal del sistema
- Grid dinámico de cámaras (2x2 o 3x2 según cantidad)
- Paneles de entrada/salida con últimas detecciones
- Lista de eventos en tiempo real

## 🔄 Flujo de Datos

1. **Inicialización**:
   - Carga configuraciones desde JSON
   - Inicializa controles de cámara
   - Establece conexión con API

2. **Streaming**:
   - Cada CameraControl maneja su stream RTSP independientemente
   - Reconexión automática en caso de fallo
   - Indicadores visuales de estado

3. **Detecciones ANPR**:
   - Polling continuo a la API cada 5 segundos
   - Procesamiento de nuevas detecciones
   - Actualización de UI en tiempo real

## 📊 Monitoreo y Logging

- **Indicadores de Estado**: API, cámaras conectadas, sistema general
- **Logging de Errores**: Archivo de log diario en `%AppData%/ANPRViewer/Logs/`
- **Manejo de Excepciones**: Captura global con notificación al usuario

## 🚀 Despliegue en Producción

### Checklist Pre-Despliegue

- [ ] Verificar URLs RTSP de todas las cámaras
- [ ] Confirmar conectividad con API ANPR
- [ ] Validar rutas de imágenes en configuración
- [ ] Probar reconexión automática
- [ ] Verificar permisos de escritura para logs
- [ ] Configurar firewall para puertos RTSP y HTTP

### Configuración de Red

- **Puertos RTSP**: Típicamente 554
- **Puerto API**: Según configuración del servidor ANPR
- **Bandwidth**: ~2Mbps por cámara HD

### Optimizaciones de Rendimiento

- Reducir `network-caching` para menor latencia
- Ajustar `live-caching` según red local
- Monitorear uso de memoria con múltiples streams
- Configurar límites de eventos históricos

## 🔍 Troubleshooting

### Problemas Comunes

1. **Cámara no conecta**:
   - Verificar URL RTSP y credenciales
   - Comprobar conectividad de red
   - Revisar firewall

2. **API no responde**:
   - Validar URL y puerto de API
   - Verificar estado del servidor ANPR
   - Revisar logs de errores

3. **Performance bajo**:
   - Reducir número de streams simultáneos
   - Ajustar configuración de VLC
   - Verificar recursos del sistema

## 📞 Soporte

Para soporte técnico y reportes de bugs, contactar al equipo de desarrollo de SYSCON PARK.

## 🔄 Actualizaciones Futuras

- [ ] Grabación de eventos de video
- [ ] Dashboard web complementario
- [ ] Alertas push/email
- [ ] Base de datos local para respaldo
- [ ] Reportes automáticos
- [ ] Integración con sistemas de control de acceso