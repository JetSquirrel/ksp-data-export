# Prometheus 集成指南 (中文)

本指南说明如何使用 KSP Data Export 的 Prometheus 导出器功能。

## 概述

Prometheus 导出器功能允许您将 KSP 飞行数据作为指标公开，供 Prometheus 或任何兼容的监控系统抓取。这使您能够使用 Grafana 等工具实现实时监控、告警和可视化飞行数据。

## 快速开始

1. **启用导出器** - 编辑 `/GameData/DataExport/logged.vals`：
   ```
   prometheusEnabled=True
   prometheusPort=9101
   ```

2. **启动 KSP** - 启动 Kerbal Space Program 并开始飞行

3. **验证指标** - 在浏览器中打开 http://localhost:9101/metrics

4. **配置 Prometheus** - 使用 `prometheus.yml.example` 中提供的示例配置

## 配置说明

### Mod 配置

编辑 `/GameData/DataExport/logged.vals` 来配置导出器：

| 设置 | 描述 | 默认值 |
|------|------|--------|
| `prometheusEnabled` | 启用/禁用 HTTP 服务器 | `True` |
| `prometheusPort` | HTTP 服务器端口 | `9101` |

### 指标选择

导出器会遵循您的可记录值设置。只有在 `logged.vals` 中启用（设置为 `True`）的值才会导出为 Prometheus 指标。

例如，仅导出飞船速度和高度：
```
logSrfSpeed=True
logAltTer=True
logAltSea=False
// ... 将其他设置为 False
```

## 端点

启用后，mod 会公开两个 HTTP 端点：

### `/metrics` - Prometheus 指标

以 Prometheus 文本格式返回指标。

示例输出：
```
# HELP ksp_data_export_info Information about the KSP Data Export mod
# TYPE ksp_data_export_info gauge
ksp_data_export_info{version="1.0"} 1

# HELP ksp_mission_time_seconds Mission elapsed time in seconds
# TYPE ksp_mission_time_seconds counter
ksp_mission_time_seconds{vessel="Kerbal X"} 123.45

# HELP ksp_surface_speed_m_per_s Surface Speed (m/s)
# TYPE ksp_surface_speed_m_per_s gauge
ksp_surface_speed_m_per_s{vessel="Kerbal X",category="vessel"} 234.56
```

### `/` - 信息页面

返回一个 HTML 页面，显示导出器的基本信息和指标端点链接。

## 指标名称

指标名称会自动从可记录值名称生成，使用以下规则：

1. 转换为小写
2. 空格替换为下划线
3. 删除特殊字符（括号等）
4. 添加 `ksp_` 前缀
5. 替换单位符号（例如 `(m/s)` 变为 `_m_per_s`）

示例：
- "Surface Speed (m/s)" → `ksp_surface_speed_m_per_s`
- "Altitude from Terrain (m)" → `ksp_altitude_from_terrain_m`
- "GForce (g)" → `ksp_gforce_g`

## 指标标签

所有指标都包含这些标签：

| 标签 | 描述 | 示例 |
|------|------|------|
| `vessel` | 飞船名称 | `"Kerbal X"` |
| `category` | 指标类别 | `"vessel"`, `"position"`, `"orbit"`, `"target"`, `"resources"`, `"science"` |

## Prometheus 设置

### 安装

1. 从 https://prometheus.io/download/ 下载 Prometheus
2. 解压到目录
3. 将 `prometheus.yml.example` 复制到 Prometheus 目录并重命名为 `prometheus.yml`
4. 启动 Prometheus：`./prometheus`
5. 访问 Prometheus UI：http://localhost:9090

### 查询指标

示例 PromQL 查询：

**当前高度：**
```
ksp_altitude_from_terrain_m
```

**随时间变化的表面速度：**
```
rate(ksp_surface_speed_m_per_s[1m])
```

**30秒内的平均 G 力：**
```
avg_over_time(ksp_gforce_g[30s])
```

## Grafana 集成

要在 Grafana 中可视化 KSP 指标：

1. **安装 Grafana** - 从 https://grafana.com/grafana/download 下载

2. **添加 Prometheus 数据源**：
   - 打开 Grafana（默认：http://localhost:3000）
   - 转到 Configuration → Data Sources → Add data source
   - 选择 Prometheus
   - 设置 URL 为 http://localhost:9090
   - 点击 "Save & Test"

3. **创建仪表板**：
   - 创建新仪表板
   - 添加带有 PromQL 查询的面板
   - 示例面板查询：
     - 高度：`ksp_altitude_from_terrain_m`
     - 速度：`ksp_surface_speed_m_per_s`
     - G 力：`ksp_gforce_g`
     - 远地点/近地点：`ksp_apoapsis_m`, `ksp_periapsis_m`

## 可用指标

所有 28 个可记录值都可以导出为指标：

### 飞船类别（Vessel）
- `ksp_surface_speed_m_per_s` - 表面速度
- `ksp_gforce_g` - G 力
- `ksp_acceleration_m_per_s2` - 加速度
- `ksp_thrust_kn` - 推力
- `ksp_twr` - 推重比
- `ksp_mass_t` - 质量
- `ksp_pitch_deg` - 俯仰角
- `ksp_heading_deg` - 航向角
- `ksp_roll_deg` - 滚转角
- `ksp_angle_of_attack_deg` - 攻角
- `ksp_mach_number` - 马赫数

### 位置类别（Position）
- `ksp_altitude_from_terrain_m` - 地面高度
- `ksp_altitude_from_the_sea_m` - 海平面高度
- `ksp_downrange_distance_m` - 下行距离
- `ksp_latitude_deg` - 纬度
- `ksp_longitude_deg` - 经度

### 轨道类别（Orbit）
- `ksp_apoapsis_m` - 远地点
- `ksp_periapsis_m` - 近地点
- `ksp_time_to_apoapsis_s` - 到达远地点时间
- `ksp_time_to_periapsis_s` - 到达近地点时间
- `ksp_inclination_deg` - 倾角
- `ksp_orbital_velocity_m_per_s` - 轨道速度
- `ksp_gravity_m_per_s2` - 重力

### 目标类别（Target）
- `ksp_target_distance_m` - 目标距离
- `ksp_target_speed_m_per_s` - 目标速度

### 资源类别（Resources）
- `ksp_stage_deltav_m_per_s` - 级段 Delta-V
- `ksp_vessel_deltav_m_per_s` - 飞船 Delta-V

### 科学类别（Science）
- `ksp_pressure_kpa` - 压力
- `ksp_external_temperature_k` - 外部温度

另外还有内置指标：
- `ksp_mission_time_seconds` - 任务经过时间（始终可用）

## 故障排除

### 无法连接到 http://localhost:9101

**检查导出器是否已启用：**
- 验证 `/GameData/DataExport/logged.vals` 中 `prometheusEnabled=True`
- 检查 KSP 调试日志中的 "[PrometheusExporter]" 消息

**端口已被占用：**
- 将 `prometheusPort` 更改为其他值（例如 `9102`）
- 确保更新 Prometheus 配置

**防火墙阻止端口：**
- 服务器仅监听 localhost (127.0.0.1)
- 如果从其他机器访问，可能需要配置防火墙规则

### 没有指标显示

**检查可记录值是否已启用：**
- `logged.vals` 中至少有一个值必须设置为 `True`
- 导出器仅导出已启用的值

**飞船未激活：**
- 指标仅在主动飞行期间可用
- `DataExport` 和 `PrometheusExporter` 类仅在飞行场景中加载

## 性能考虑

- HTTP 服务器在后台线程上运行，对游戏性能影响最小
- 指标在 Prometheus 抓取时按需生成
- 默认抓取间隔为 1 秒，与 mod 的默认日志速率匹配
- 为了更好的性能，可以在 Prometheus 配置中增加抓取间隔

## 安全说明

- HTTP 服务器仅监听 localhost (127.0.0.1)
- 不需要身份验证（仅限本地访问）
- 不要在没有适当安全措施的情况下将端口暴露到互联网
- 如果需要远程访问，考虑使用带身份验证的反向代理

## 使用示例

### 示例 1：基本飞行监控

启用基本飞船指标：
```
logSrfSpeed=True
logAltTer=True
logGForce=True
logThrust=True
```

Grafana 面板：
- 折线图：随时间变化的高度和速度
- 仪表：当前 G 力
- 统计：当前推力

### 示例 2：轨道插入

启用轨道指标：
```
logAp=True
logPe=True
logTimeToAp=True
logOrbVel=True
logInc=True
```

Grafana 面板：
- 双轴图：远地点和近地点
- 统计：到达远地点的时间
- 图表：轨道速度

### 示例 3：大气飞行

启用大气指标：
```
logSrfSpeed=True
logAltSea=True
logMach=True
logAoA=True
logPressure=True
logExternTemp=True
```

Grafana 面板：
- 折线图：速度和高度
- 仪表：马赫数
- 热图：攻角分布
- 双轴：压力和温度

## 更多资源

- **Prometheus 文档**: https://prometheus.io/docs/
- **Grafana 文档**: https://grafana.com/docs/
- **PromQL 教程**: https://prometheus.io/docs/prometheus/latest/querying/basics/
- **KSP Data Export GitHub**: https://github.com/kna27/ksp-data-export

## 贡献

如果遇到问题或对 Prometheus 集成有建议：
1. 在 https://github.com/kna27/ksp-data-export/issues 查看现有问题
2. 创建新问题并提供：
   - KSP 版本
   - Mod 版本
   - Prometheus 版本
   - 配置文件
   - KSP 调试日志中的错误消息
