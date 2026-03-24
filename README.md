# KSP Data Export

![Mod Downloads](https://img.shields.io/endpoint?url=https%3A%2F%2Fmod-download-count-badge.vercel.app%3Fgithub%3Dkna27%2Fksp-data-export%26spacedock%3D2711%26curseforge%3D475559%26format%3Dcomma)

## Introduction

**[Forum Thread](https://forum.kerbalspaceprogram.com/index.php?/topic/201967-111x-export-flight-data-to-a-csv-file-mod)**

Ever wanted to view your KSP flight data in a graph? Well, this mod allows you to do that! This mod exports flight telemetry data in two powerful ways:

- **CSV Export**: Export flight data to CSV files for analysis in Excel, Google Sheets, or any spreadsheet program. Create custom charts and graphs from your flight data!

- **Prometheus Exporter**: Real-time metrics server for monitoring your flights with Prometheus and Grafana. Perfect for live dashboards and advanced monitoring!

Choose the export method that fits your needs, or use both simultaneously!

## Features

### CSV Export
- Export 28+ different flight parameters including:
  - Vessel dynamics (speed, acceleration, thrust, TWR, mass)
  - Position data (altitude, coordinates, downrange distance)
  - Orbital mechanics (apoapsis, periapsis, inclination, orbital velocity)
  - Targeting data (target distance and speed)
  - Resources (stage/vessel delta-v)
  - Science values (pressure, external temperature)
- Configurable logging via GUI and config file
- Automatic file generation with timestamps
- Adjustable logging intervals

### Prometheus Exporter
- Real-time HTTP metrics server (port 9101 by default)
- Compatible with Prometheus + Grafana for live dashboards
- All loggable values exposed as metrics
- Proper metric naming with labels (vessel name, category)
- Zero external dependencies, minimal performance impact

## Reporting Bugs

If you encounter any bugs or have any suggestions, report them at https://github.com/kna27/ksp-data-export/issues.

## Installation

This mod works on Windows, MacOS, and Linux.

This mod is available on [CKAN](https://github.com/KSP-CKAN/CKAN), [SpaceDock](https://spacedock.info/mod/2711/KSP%20Data%20Export), and [CurseForge](https://www.curseforge.com/kerbal/ksp-mods/data-export).

1. Download GameData.zip from the [latest release here](https://github.com/kna27/ksp-data-export/releases/latest)
2. Copy the DataExport folder to `YourKSPInstallDirectory/Kerbal Space Program/GameData`

Your directory should look like: `YourKSPInstallDirectory/Kerbal Space Program/GameData/DataExport` if done correctly.

## How to use

Click the mod's icon in flight to view the GUI for the mod.

This video goes in-depth on how to use the mod:

[![Help Video](https://img.youtube.com/vi/3s2SctniVLM/0.jpg)](https://www.youtube.com/watch?v=3s2SctniVLM)

## Prometheus Exporter

The mod includes a built-in HTTP server that exports flight data in Prometheus format for real-time monitoring and visualization!

### Features

- Real-time metrics exposed via HTTP on port 9101 (configurable)
- All 28+ flight parameters available as Prometheus metrics
- Integration with Grafana for beautiful dashboards
- Minimal performance impact (runs on background thread)
- Zero external dependencies

### Quick Start

1. **Enable the exporter** by editing `/GameData/DataExport/logged.vals`:

```
prometheusEnabled=True    # Enable/disable the exporter
prometheusPort=9101       # HTTP server port (default: 9101)
```

2. **Launch KSP** and start a flight

3. **View metrics** at `http://localhost:9101/metrics`

4. **Configure Prometheus** by adding to your `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'ksp'
    static_configs:
      - targets: ['localhost:9101']
```

### Available Metrics

All enabled loggable values are exported as Prometheus metrics with labels for vessel name and category. Example metrics:

-   `ksp_surface_speed_m_per_s` - Surface speed
-   `ksp_altitude_from_terrain_m` - Altitude from terrain
-   `ksp_apoapsis_m` - Apoapsis altitude
-   `ksp_periapsis_m` - Periapsis altitude
-   `ksp_gforce_g` - G-force
-   `ksp_mission_time_seconds` - Mission elapsed time

### Detailed Documentation

For complete setup instructions, Grafana integration, and troubleshooting:

- **[PROMETHEUS.md](PROMETHEUS.md)** - Complete English guide with Grafana dashboard examples
- **[PROMETHEUS_CN.md](PROMETHEUS_CN.md)** - 完整的中文指南 (Complete Chinese guide)
- **[EXAMPLE_METRICS.md](EXAMPLE_METRICS.md)** - Sample metrics output
- **[prometheus.yml.example](prometheus.yml.example)** - Example Prometheus configuration

## Support

[Email me with any questions or comments](mailto:krisharora27@gmail.com)

## Contributing

To get the references in the `.csproj` file to work, you need to add an environment variable `KSP` with the value set to the full path of `Kerbal Space Program/KSP_Data/Managed`. For example:

-   Windows: `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\KSP_x64_Data\Managed`
-   Linux: `/.steam/debian-installation/steamapps/common/Kerbal Space Program/KSP_Data/Managed`
-   MacOS: `/Users/<username>/Library/Application Support/Steam/steamapps/common/Kerbal Space Program/KSP.app/Contents/Resources/Data/Managed`
