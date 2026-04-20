using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

namespace KSPDataExport
{
    /// <summary>
    ///     HTTP server that exposes metrics in Prometheus format
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PrometheusExporter : MonoBehaviour
    {
        private System.Net.HttpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        public static bool IsEnabled;
        public static int Port = 9101;

        private readonly object _snapshotLock = new object();
        private MetricsSnapshot _snapshot;

        private class MetricsSnapshot
        {
            public string VesselName;
            public double MissionTime;
            public Dictionary<string, MetricEntry> Metrics;
        }

        private class MetricEntry
        {
            public string Name;
            public string Category;
            public double Value;
            public string Help;
        }

        private void Start()
        {
            Debug.Log("[PrometheusExporter] Initializing");

            // Read configuration for whether Prometheus export is enabled
            IsEnabled = Config.GetValue(DataExport.CfgPath, "prometheusEnabled");

            // Read port configuration if available
            string portStr = Config.GetValueString(DataExport.CfgPath, "prometheusPort");
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int configPort))
            {
                Port = configPort;
            }

            if (IsEnabled)
            {
                StartServer();
            }
        }

        /// <summary>
        ///     Refreshes the metric snapshot on the main thread so background HTTP threads never touch Unity APIs.
        /// </summary>
        private void FixedUpdate()
        {
            if (DataExport.LoggableValues == null || DataExport.ActVess == null) return;

            var metrics = new Dictionary<string, MetricEntry>();

            foreach (var lv in DataExport.LoggableValues)
            {
                if (!lv.Logging) continue;

                string raw = lv.Value();
                if (string.IsNullOrEmpty(raw)) continue;
                if (!double.TryParse(raw, out double numericValue)) continue;

                string metricName = ConvertToPrometheusName(lv.Name);
                metrics[metricName] = new MetricEntry
                {
                    Name = metricName,
                    Category = lv.Category.ToString().ToLower(),
                    Value = numericValue,
                    Help = lv.Name
                };
            }

            lock (_snapshotLock)
            {
                _snapshot = new MetricsSnapshot
                {
                    VesselName = DataExport.ActVess.GetDisplayName(),
                    MissionTime = DataExport.ActVess.missionTime,
                    Metrics = metrics
                };
            }
        }

        /// <summary>
        ///     Starts the HTTP server on the configured port
        /// </summary>
        public void StartServer()
        {
            try
            {
                _listener = new System.Net.HttpListener();
                _listener.Prefixes.Add($"http://localhost:{Port}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(HandleRequests)
                {
                    IsBackground = true
                };
                _listenerThread.Start();

                Debug.Log($"[PrometheusExporter] HTTP server started on port {Port}");
                Debug.Log($"[PrometheusExporter] Metrics available at http://localhost:{Port}/metrics");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PrometheusExporter] Failed to start server: {e}");
                _isRunning = false;
            }
        }

        /// <summary>
        ///     Stops the HTTP server
        /// </summary>
        public void StopServer()
        {
            try
            {
                _isRunning = false;

                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                }

                if (_listenerThread != null && _listenerThread.IsAlive)
                {
                    _listenerThread.Join(1000);
                }

                Debug.Log("[PrometheusExporter] HTTP server stopped");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PrometheusExporter] Error stopping server: {e}");
            }
        }

        /// <summary>
        ///     Handles incoming HTTP requests
        /// </summary>
        private void HandleRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (System.Net.HttpListenerException)
                {
                    // Expected when stopping the listener
                    if (!_isRunning) break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PrometheusExporter] Error handling request: {e}");
                }
            }
        }

        /// <summary>
        ///     Processes a single HTTP request
        /// </summary>
        private void ProcessRequest(System.Net.HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Only respond to /metrics endpoint
                if (request.Url.AbsolutePath == "/metrics")
                {
                    string metricsData = GeneratePrometheusMetrics();
                    byte[] buffer = Encoding.UTF8.GetBytes(metricsData);

                    response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = 200;

                    using (var output = response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
                else if (request.Url.AbsolutePath == "/")
                {
                    // Root endpoint - provide basic info
                    MetricsSnapshot infoSnapshot;
                    lock (_snapshotLock) { infoSnapshot = _snapshot; }
                    string vessel = infoSnapshot != null ? infoSnapshot.VesselName : "N/A";
                    string html = "<html><body><h1>KSP Data Export - Prometheus Exporter</h1>" +
                                  $"<p>Metrics available at <a href=\"/metrics\">/metrics</a></p>" +
                                  $"<p>Vessel: {vessel}</p>" +
                                  "</body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(html);

                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = 200;

                    using (var output = response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PrometheusExporter] Error processing request: {e}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors when trying to send error response
                }
            }
        }

        /// <summary>
        ///     Generates metrics in Prometheus text exposition format from the main-thread snapshot.
        ///     This method runs on a background thread and must NOT touch Unity APIs.
        /// </summary>
        private string GeneratePrometheusMetrics()
        {
            MetricsSnapshot snapshot;
            lock (_snapshotLock)
            {
                snapshot = _snapshot;
            }

            var sb = new StringBuilder();

            // Add metadata comment
            sb.AppendLine("# HELP ksp_data_export_info Information about the KSP Data Export mod");
            sb.AppendLine("# TYPE ksp_data_export_info gauge");
            sb.AppendLine("ksp_data_export_info{version=\"1.0\"} 1");
            sb.AppendLine();

            if (snapshot == null || string.IsNullOrEmpty(snapshot.VesselName))
            {
                return sb.ToString();
            }

            string vesselName = SanitizeLabelValue(snapshot.VesselName);

            // Add mission time as gauge (resets on revert, so counter is inappropriate)
            sb.AppendLine("# HELP ksp_mission_time_seconds Mission elapsed time in seconds");
            sb.AppendLine("# TYPE ksp_mission_time_seconds gauge");
            sb.AppendLine($"ksp_mission_time_seconds{{vessel=\"{vesselName}\"}} {snapshot.MissionTime:F2}");
            sb.AppendLine();

            foreach (var kvp in snapshot.Metrics)
            {
                var m = kvp.Value;
                sb.AppendLine($"# HELP {m.Name} {m.Help}");
                sb.AppendLine($"# TYPE {m.Name} gauge");
                sb.AppendLine($"{m.Name}{{vessel=\"{vesselName}\",category=\"{m.Category}\"}} {m.Value}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Converts a human-readable name to a Prometheus-compliant metric name
        /// </summary>
        private static string ConvertToPrometheusName(string name)
        {
            // Prometheus metric names must match [a-zA-Z_:][a-zA-Z0-9_:]*
            // Convert to lowercase, replace spaces and special chars with underscores
            string metricName = name.ToLower()
                .Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("/", "_per_")
                .Replace("^", "")
                .Replace("-", "_")
                .Replace(".", "_");

            // Add ksp_ prefix
            metricName = "ksp_" + metricName;

            // Remove any remaining invalid characters
            var sb = new StringBuilder();
            foreach (char c in metricName)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == ':')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Sanitizes a label value for Prometheus (escapes special characters)
        /// </summary>
        private static string SanitizeLabelValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n");
        }

        private void OnDestroy()
        {
            StopServer();
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }
    }
}
