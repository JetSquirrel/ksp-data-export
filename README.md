# KSP Data Export

![Mod Downloads](https://img.shields.io/endpoint?url=https%3A%2F%2Fmod-download-count-badge.vercel.app%3Fgithub%3Dkna27%2Fksp-data-export%26spacedock%3D2711%26curseforge%3D475559%26format%3Dcomma)

## Introduction

**[Forum Thread](https://forum.kerbalspaceprogram.com/index.php?/topic/201967-111x-export-flight-data-to-a-csv-file-mod)**

Ever wanted to view your KSP flight data in a graph? Well, this mod allows you to do that! This mod exports flight telemetry data in three powerful ways:

- **CSV Export**: Export flight data to CSV files for analysis in Excel, Google Sheets, or any spreadsheet program. Create custom charts and graphs from your flight data!

- **Prometheus Exporter**: Real-time metrics server for monitoring your flights with Prometheus and Grafana. Perfect for live dashboards and advanced monitoring!

- **GreptimeDB Exporter**: Push telemetry directly to GreptimeDB (or any InfluxDB-compatible database) using InfluxDB Line Protocol. Ideal for high-performance time-series storage and analytics!

Choose the export method that fits your needs, or use all three simultaneously!

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
- Supports vessel switching during flight

### Prometheus Exporter
- Real-time HTTP metrics server (port 9101 by default)
- Compatible with Prometheus + Grafana for live dashboards
- All loggable values exposed as metrics
- Proper metric naming with labels (vessel name, category)
- Thread-safe metric snapshotting - zero impact on game performance
- Zero external dependencies

### GreptimeDB Exporter
- Push metrics to GreptimeDB using InfluxDB Line Protocol
- Configurable endpoint, database, and authentication
- Automatic batching and exponential backoff on errors
- Background thread sending - no frame drops
- Works with any InfluxDB-compatible endpoint

### Reliability
- Thread-safe config file access
- Graceful error handling with automatic recovery
- Vessel switching support (EVA, docking, scene changes)
- Correct TWR calculation for any celestial body

## Reporting Bugs

If you encounter any bugs or have any suggestions, report them at https://github.com/kna27/ksp-data-export/issues.

## Installation

This mod works on Windows

This mod is available on [CKAN](https://github.com/KSP-CKAN/CKAN), [SpaceDock](https://spacedock.info/mod/2711/KSP%20Data%20Export), and [CurseForge](https://www.curseforge.com/kerbal/ksp-mods/data-export).

1. Download GameData.zip from the [latest release here](https://github.com/kna27/ksp-data-export/releases/latest)
2. Copy the DataExport folder to `YourKSPInstallDirectory/Kerbal Space Program/GameData`

Your directory should look like: `YourKSPInstallDirectory/Kerbal Space Program/GameData/DataExport` if done correctly.

## How to use

Click the mod's icon in flight to view the GUI for the mod.

This video goes in-depth on how to use the mod:

[![Help Video](https://img.youtube.com/vi/3s2SctniVLM/0.jpg)](https://www.youtube.com/watch?v=3s2SctniVLM)

## Configuration

Edit `/GameData/DataExport/logged.vals` to configure the mod:

```
// CSV Logging
defaultLogState=False       # Start logging automatically when entering flight

// Prometheus Exporter
prometheusEnabled=True      # Enable/disable the Prometheus HTTP server
prometheusPort=9101         # HTTP server port

// GreptimeDB Exporter (InfluxDB Line Protocol)
greptimeEnabled=True        # Enable/disable GreptimeDB pushing
greptimeUrl=http://localhost:4000
greptimeDatabase=public
greptimeUsername=
greptimePassword=
greptimeInterval=1          # Send interval in seconds

// Per-metric toggles
logSrfSpeed=True
logGForce=True
...
```

## Prometheus Exporter

The mod includes a built-in HTTP server that exports flight data in Prometheus format for real-time monitoring and visualization!

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
- **[PROMETHEUS_CN.md](PROMETHEUS_CN.md)** - Complete Chinese guide (完整的中文指南)
- **[EXAMPLE_METRICS.md](EXAMPLE_METRICS.md)** - Sample metrics output
- **[prometheus.yml.example](prometheus.yml.example)** - Example Prometheus configuration

## GreptimeDB Exporter

Push flight telemetry directly to GreptimeDB or any InfluxDB-compatible time-series database.

### Quick Start

1. **Enable the exporter** by editing `/GameData/DataExport/logged.vals`:

```
greptimeEnabled=True              # Enable/disable the exporter
greptimeUrl=http://localhost:4000 # GreptimeDB HTTP endpoint
greptimeDatabase=public           # Database name
greptimeInterval=1                # Push interval in seconds (min 0.5)
```

2. **Launch KSP** and start a flight

3. Data will be automatically pushed to your GreptimeDB instance

### Authentication (optional)

If your GreptimeDB instance requires authentication:

```
greptimeUsername=your_username
greptimePassword=your_password
```

## Support

[Email me with any questions or comments](mailto:krisharora27@gmail.com)

## Contributing

To get the references in the `.csproj` file to work, you need to add an environment variable `KSP` with the value set to the full path of `Kerbal Space Program/KSP_Data/Managed`. For example:

-   Windows: `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\KSP_x64_Data\Managed`
-   Linux: `/.steam/debian-installation/steamapps/common/Kerbal Space Program/KSP_Data/Managed`
-   MacOS: `/Users/<username>/Library/Application Support/Steam/steamapps/common/Kerbal Space Program/KSP.app/Contents/Resources/Data/Managed`
