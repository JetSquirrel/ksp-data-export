# Prometheus Integration Guide

This guide explains how to use the Prometheus exporter feature in KSP Data Export.

## Overview

The Prometheus exporter feature allows you to expose KSP flight data as metrics that can be scraped by Prometheus or any compatible monitoring system. This enables real-time monitoring, alerting, and visualization of your flight data using tools like Grafana.

## Quick Start

1. **Enable the exporter** - Edit `/GameData/DataExport/logged.vals`:
   ```
   prometheusEnabled=True
   prometheusPort=9101
   ```

2. **Start KSP** - Launch Kerbal Space Program and start a flight

3. **Verify metrics** - Open http://localhost:9101/metrics in your browser

4. **Configure Prometheus** - Use the example configuration provided in `prometheus.yml.example`

## Configuration

### Mod Configuration

Edit `/GameData/DataExport/logged.vals` to configure the exporter:

| Setting | Description | Default |
|---------|-------------|---------|
| `prometheusEnabled` | Enable/disable the HTTP server | `True` |
| `prometheusPort` | HTTP server port | `9101` |

### Metric Selection

The exporter respects your loggable value settings. Only values that are enabled (set to `True` in `logged.vals`) will be exported as Prometheus metrics.

For example, to export only vessel speed and altitude:
```
logSrfSpeed=True
logAltTer=True
logAltSea=False
// ... set others to False
```

## Endpoints

Once enabled, the mod exposes two HTTP endpoints:

### `/metrics` - Prometheus Metrics

Returns metrics in Prometheus text exposition format.

Example output:
```
# HELP ksp_data_export_info Information about the KSP Data Export mod
# TYPE ksp_data_export_info gauge
ksp_data_export_info{version="1.0"} 1

# HELP ksp_mission_time_seconds Mission elapsed time in seconds
# TYPE ksp_mission_time_seconds counter
ksp_mission_time_seconds{vessel="Kerbal X",category="mission"} 123.45

# HELP ksp_surface_speed_m_per_s Surface Speed (m/s)
# TYPE ksp_surface_speed_m_per_s gauge
ksp_surface_speed_m_per_s{vessel="Kerbal X",category="vessel"} 234.56

# HELP ksp_altitude_from_terrain_m Altitude from Terrain (m)
# TYPE ksp_altitude_from_terrain_m gauge
ksp_altitude_from_terrain_m{vessel="Kerbal X",category="position"} 15234.78
```

### `/` - Info Page

Returns an HTML page with basic information about the exporter and links to the metrics endpoint.

## Metric Names

Metric names are automatically generated from the loggable value names using these rules:

1. Convert to lowercase
2. Replace spaces with underscores
3. Remove special characters (parentheses, etc.)
4. Add `ksp_` prefix
5. Replace units notation (e.g., `(m/s)` becomes `_m_per_s`)

Examples:
- "Surface Speed (m/s)" → `ksp_surface_speed_m_per_s`
- "Altitude from Terrain (m)" → `ksp_altitude_from_terrain_m`
- "GForce (g)" → `ksp_gforce_g`
- "TWR" → `ksp_twr`

## Metric Labels

All metrics include these labels:

| Label | Description | Example |
|-------|-------------|---------|
| `vessel` | Name of the vessel | `"Kerbal X"` |
| `category` | Category of the metric | `"vessel"`, `"position"`, `"orbit"`, `"target"`, `"resources"`, `"science"` |

## Prometheus Setup

### Installation

1. Download Prometheus from https://prometheus.io/download/
2. Extract the archive to a directory
3. Copy `prometheus.yml.example` to the Prometheus directory as `prometheus.yml`
4. Start Prometheus: `./prometheus`
5. Access Prometheus UI at http://localhost:9090

### Querying Metrics

Example PromQL queries:

**Current altitude:**
```
ksp_altitude_from_terrain_m
```

**Surface speed over time:**
```
rate(ksp_surface_speed_m_per_s[1m])
```

**Multiple vessels (if you have multiple KSP instances):**
```
ksp_altitude_from_terrain_m{vessel="Kerbal X"}
```

**Average G-force over 30 seconds:**
```
avg_over_time(ksp_gforce_g[30s])
```

## Grafana Integration

To visualize KSP metrics in Grafana:

1. **Install Grafana** - Download from https://grafana.com/grafana/download

2. **Add Prometheus data source**:
   - Open Grafana (default: http://localhost:3000)
   - Go to Configuration → Data Sources → Add data source
   - Select Prometheus
   - Set URL to http://localhost:9090
   - Click "Save & Test"

3. **Create dashboard**:
   - Create a new dashboard
   - Add panels with PromQL queries
   - Example panel queries:
     - Altitude: `ksp_altitude_from_terrain_m`
     - Speed: `ksp_surface_speed_m_per_s`
     - G-Force: `ksp_gforce_g`
     - Apoapsis/Periapsis: `ksp_apoapsis_m`, `ksp_periapsis_m`

4. **Panel suggestions**:
   - **Time series graph**: Show altitude, speed over time
   - **Gauge**: Display current G-force, TWR
   - **Stat**: Show current mission time, max altitude reached
   - **Heatmap**: G-force distribution over flight

## Available Metrics

All 28 loggable values can be exported as metrics:

### Vessel Category
- `ksp_surface_speed_m_per_s` - Surface Speed
- `ksp_gforce_g` - G-Force
- `ksp_acceleration_m_per_s2` - Acceleration
- `ksp_thrust_kn` - Thrust
- `ksp_twr` - Thrust-to-Weight Ratio
- `ksp_mass_t` - Mass
- `ksp_pitch_deg` - Pitch
- `ksp_heading_deg` - Heading
- `ksp_roll_deg` - Roll
- `ksp_angle_of_attack_deg` - Angle of Attack
- `ksp_mach_number` - Mach Number

### Position Category
- `ksp_altitude_from_terrain_m` - Altitude from Terrain
- `ksp_altitude_from_the_sea_m` - Altitude from Sea Level
- `ksp_downrange_distance_m` - Downrange Distance
- `ksp_latitude_deg` - Latitude
- `ksp_longitude_deg` - Longitude

### Orbit Category
- `ksp_apoapsis_m` - Apoapsis
- `ksp_periapsis_m` - Periapsis
- `ksp_time_to_apoapsis_s` - Time to Apoapsis
- `ksp_time_to_periapsis_s` - Time to Periapsis
- `ksp_inclination_deg` - Inclination
- `ksp_orbital_velocity_m_per_s` - Orbital Velocity
- `ksp_gravity_m_per_s2` - Gravity

### Target Category
- `ksp_target_distance_m` - Target Distance
- `ksp_target_speed_m_per_s` - Target Speed

### Resources Category
- `ksp_stage_deltav_m_per_s` - Stage Delta-V
- `ksp_vessel_deltav_m_per_s` - Vessel Delta-V

### Science Category
- `ksp_pressure_kpa` - Pressure
- `ksp_external_temperature_k` - External Temperature

Plus the built-in metric:
- `ksp_mission_time_seconds` - Mission elapsed time (always available)

## Troubleshooting

### Cannot connect to http://localhost:9101

**Check if the exporter is enabled:**
- Verify `prometheusEnabled=True` in `/GameData/DataExport/logged.vals`
- Check KSP's debug log for "[PrometheusExporter]" messages

**Port already in use:**
- Change `prometheusPort` to a different value (e.g., `9102`)
- Make sure to update your Prometheus configuration

**Firewall blocking the port:**
- The server only listens on localhost (127.0.0.1)
- If accessing from another machine, you may need to configure firewall rules

### No metrics appearing

**Check loggable values are enabled:**
- At least one value must be set to `True` in `logged.vals`
- The exporter only exports enabled values

**Vessel not active:**
- Metrics are only available during active flight
- The `DataExport` and `PrometheusExporter` classes only load in flight scene

### Metrics show empty values

- Some values may be unavailable in certain flight situations
- For example, target distance is only available when a target is set
- The exporter skips empty or invalid values

## Performance Considerations

- The HTTP server runs on a background thread and has minimal impact on game performance
- Metrics are generated on-demand when Prometheus scrapes them
- Default scrape interval of 1 second matches the mod's default log rate
- For better performance, increase the scrape interval in Prometheus configuration

## Security Notes

- The HTTP server only listens on localhost (127.0.0.1)
- No authentication is required (local access only)
- Do not expose the port to the internet without proper security measures
- Consider using a reverse proxy with authentication if remote access is needed

## Examples

### Example 1: Basic Flight Monitoring

Enable basic vessel metrics:
```
logSrfSpeed=True
logAltTer=True
logGForce=True
logThrust=True
```

Grafana panels:
- Line graph: Altitude and speed over time
- Gauge: Current G-force
- Stat: Current thrust

### Example 2: Orbital Insertion

Enable orbital metrics:
```
logAp=True
logPe=True
logTimeToAp=True
logOrbVel=True
logInc=True
```

Grafana panels:
- Dual-axis graph: Apoapsis and Periapsis
- Stat: Time to apoapsis
- Graph: Orbital velocity

### Example 3: Atmospheric Flight

Enable atmospheric metrics:
```
logSrfSpeed=True
logAltSea=True
logMach=True
logAoA=True
logPressure=True
logExternTemp=True
```

Grafana panels:
- Line graph: Speed and altitude
- Gauge: Mach number
- Heatmap: Angle of attack distribution
- Dual-axis: Pressure and temperature

## Additional Resources

- **Prometheus Documentation**: https://prometheus.io/docs/
- **Grafana Documentation**: https://grafana.com/docs/
- **PromQL Tutorial**: https://prometheus.io/docs/prometheus/latest/querying/basics/
- **KSP Data Export GitHub**: https://github.com/kna27/ksp-data-export

## Contributing

If you encounter issues or have suggestions for the Prometheus integration:
1. Check existing issues at https://github.com/kna27/ksp-data-export/issues
2. Create a new issue with:
   - Your KSP version
   - Mod version
   - Prometheus version
   - Configuration files
   - Error messages from KSP debug log
