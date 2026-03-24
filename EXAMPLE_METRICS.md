# Example Metrics Output

This file shows example output from the `/metrics` endpoint when the Prometheus exporter is enabled.

## Sample Request

```bash
curl http://localhost:9101/metrics
```

## Sample Response

```prometheus
# HELP ksp_data_export_info Information about the KSP Data Export mod
# TYPE ksp_data_export_info gauge
ksp_data_export_info{version="1.0"} 1

# HELP ksp_mission_time_seconds Mission elapsed time in seconds
# TYPE ksp_mission_time_seconds counter
ksp_mission_time_seconds{vessel="Kerbal X",category="mission"} 245.67

# HELP ksp_surface_speed_m_per_s Surface Speed (m/s)
# TYPE ksp_surface_speed_m_per_s gauge
ksp_surface_speed_m_per_s{vessel="Kerbal X",category="vessel"} 1247.82

# HELP ksp_gforce_g GForce (g)
# TYPE ksp_gforce_g gauge
ksp_gforce_g{vessel="Kerbal X",category="vessel"} 2.34

# HELP ksp_twr TWR
# TYPE ksp_twr gauge
ksp_twr{vessel="Kerbal X",category="vessel"} 1.45

# HELP ksp_pitch_deg Pitch (deg)
# TYPE ksp_pitch_deg gauge
ksp_pitch_deg{vessel="Kerbal X",category="vessel"} 45.23

# HELP ksp_altitude_from_terrain_m Altitude from Terrain (m)
# TYPE ksp_altitude_from_terrain_m gauge
ksp_altitude_from_terrain_m{vessel="Kerbal X",category="position"} 18234.56

# HELP ksp_apoapsis_m Apoapsis (m)
# TYPE ksp_apoapsis_m gauge
ksp_apoapsis_m{vessel="Kerbal X",category="orbit"} 125000.00

# HELP ksp_periapsis_m Periapsis (m)
# TYPE ksp_periapsis_m gauge
ksp_periapsis_m{vessel="Kerbal X",category="orbit"} 75000.00

# HELP ksp_inclination_deg Inclination (deg)
# TYPE ksp_inclination_deg gauge
ksp_inclination_deg{vessel="Kerbal X",category="orbit"} 0.05

# HELP ksp_orbital_velocity_m_per_s Orbital Velocity (m/s)
# TYPE ksp_orbital_velocity_m_per_s gauge
ksp_orbital_velocity_m_per_s{vessel="Kerbal X",category="orbit"} 2245.67
```

## Metric Naming Convention

All metrics follow this pattern:

```
ksp_{description}_{unit_suffix}
```

### Examples:

| Original Name | Metric Name | Unit |
|--------------|-------------|------|
| Surface Speed (m/s) | `ksp_surface_speed_m_per_s` | meters per second |
| Altitude from Terrain (m) | `ksp_altitude_from_terrain_m` | meters |
| GForce (g) | `ksp_gforce_g` | g-force |
| TWR | `ksp_twr` | dimensionless |
| Pitch (deg) | `ksp_pitch_deg` | degrees |
| Pressure (kPa) | `ksp_pressure_kpa` | kilopascals |
| External Temperature (K) | `ksp_external_temperature_k` | kelvin |

## Labels

Each metric includes two labels:

1. **vessel**: The name of the vessel (e.g., "Kerbal X", "Apollo 11", "Rocket")
2. **category**: The category of the metric (e.g., "vessel", "position", "orbit", "target", "resources", "science")

### Label Usage in PromQL

Filter by vessel:
```promql
ksp_altitude_from_terrain_m{vessel="Kerbal X"}
```

Filter by category:
```promql
ksp_gforce_g{category="vessel"}
```

Combine filters:
```promql
ksp_surface_speed_m_per_s{vessel="Rocket",category="vessel"}
```

## Metric Types

The exporter uses two metric types:

- **gauge**: Values that can go up and down (most metrics)
  - Examples: altitude, speed, G-force, temperature, pressure

- **counter**: Values that only increase (mission time)
  - Example: `ksp_mission_time_seconds`

## Full List of Available Metrics

Depending on which values are enabled in `logged.vals`, you may see:

### Vessel Category (11 metrics)
- ksp_surface_speed_m_per_s
- ksp_gforce_g
- ksp_acceleration_m_per_s2
- ksp_thrust_kn
- ksp_twr
- ksp_mass_t
- ksp_pitch_deg
- ksp_heading_deg
- ksp_roll_deg
- ksp_angle_of_attack_deg
- ksp_mach_number

### Position Category (5 metrics)
- ksp_altitude_from_terrain_m
- ksp_altitude_from_the_sea_m
- ksp_downrange_distance_m
- ksp_latitude_deg
- ksp_longitude_deg

### Orbit Category (7 metrics)
- ksp_apoapsis_m
- ksp_periapsis_m
- ksp_time_to_apoapsis_s
- ksp_time_to_periapsis_s
- ksp_inclination_deg
- ksp_orbital_velocity_m_per_s
- ksp_gravity_m_per_s2

### Target Category (2 metrics)
- ksp_target_distance_m
- ksp_target_speed_m_per_s

### Resources Category (2 metrics)
- ksp_stage_deltav_m_per_s
- ksp_vessel_deltav_m_per_s

### Science Category (2 metrics)
- ksp_pressure_kpa
- ksp_external_temperature_k

### Built-in Metrics (2 metrics)
- ksp_data_export_info (metadata)
- ksp_mission_time_seconds (always available)

## Testing the Endpoint

### Using curl

```bash
# Get all metrics
curl http://localhost:9101/metrics

# Save to file
curl http://localhost:9101/metrics > ksp_metrics.txt

# Watch metrics update in real-time
watch -n 1 'curl -s http://localhost:9101/metrics | grep altitude'
```

### Using wget

```bash
wget http://localhost:9101/metrics -O ksp_metrics.txt
```

### Using PowerShell (Windows)

```powershell
Invoke-WebRequest -Uri http://localhost:9101/metrics | Select-Object -ExpandProperty Content
```

### Using a Web Browser

Simply navigate to:
- http://localhost:9101/metrics - View metrics
- http://localhost:9101/ - View info page

## Prometheus Query Examples

Once metrics are being scraped by Prometheus, you can query them:

### Current Values

```promql
# Current altitude
ksp_altitude_from_terrain_m

# Current speed
ksp_surface_speed_m_per_s

# Current G-force
ksp_gforce_g
```

### Rate of Change

```promql
# Rate of altitude change (vertical speed)
rate(ksp_altitude_from_terrain_m[10s])

# Rate of speed change (acceleration)
rate(ksp_surface_speed_m_per_s[10s])
```

### Aggregations

```promql
# Average G-force over last 30 seconds
avg_over_time(ksp_gforce_g[30s])

# Maximum altitude reached in last 5 minutes
max_over_time(ksp_altitude_from_terrain_m[5m])

# Minimum periapsis in last minute
min_over_time(ksp_periapsis_m[1m])
```

### Comparisons

```promql
# Difference between apoapsis and periapsis
ksp_apoapsis_m - ksp_periapsis_m

# Speed as percentage of orbital velocity
(ksp_surface_speed_m_per_s / ksp_orbital_velocity_m_per_s) * 100
```

## Grafana Dashboard Examples

### Panel 1: Altitude Graph
- **Query**: `ksp_altitude_from_terrain_m`
- **Type**: Time series
- **Y-axis**: Altitude (m)

### Panel 2: Speed and G-Force
- **Query 1**: `ksp_surface_speed_m_per_s`
- **Query 2**: `ksp_gforce_g`
- **Type**: Time series with dual Y-axis

### Panel 3: Current Mission Time
- **Query**: `ksp_mission_time_seconds`
- **Type**: Stat panel
- **Unit**: seconds

### Panel 4: Orbital Parameters
- **Query 1**: `ksp_apoapsis_m{vessel="Kerbal X"}`
- **Query 2**: `ksp_periapsis_m{vessel="Kerbal X"}`
- **Type**: Time series
- **Y-axis**: Altitude (m)

### Panel 5: G-Force Gauge
- **Query**: `ksp_gforce_g`
- **Type**: Gauge
- **Thresholds**:
  - Green: 0-2
  - Yellow: 2-4
  - Red: 4+

## Troubleshooting Metrics

If metrics don't appear:

1. **Check if exporter is enabled**:
   ```bash
   curl http://localhost:9101/
   ```
   Should return an HTML info page.

2. **Check if values are enabled** in `logged.vals`:
   ```
   logSrfSpeed=True  # Must be True to export
   ```

3. **Check KSP debug log** for errors:
   ```
   [PrometheusExporter] HTTP server started on port 9101
   ```

4. **Verify Prometheus is scraping**:
   - Check Prometheus targets page: http://localhost:9090/targets
   - Should show "ksp" target as UP

5. **Test metric presence**:
   ```bash
   curl http://localhost:9101/metrics | grep ksp_surface_speed
   ```
