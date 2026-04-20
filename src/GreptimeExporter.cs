using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace KSPDataExport
{
    /// <summary>
    ///     Pushes telemetry data to GreptimeDB using InfluxDB Line Protocol
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GreptimeExporter : MonoBehaviour
    {
        public static bool IsEnabled;
        private static string _url = "http://127.0.0.1:4000";
        private static string _database = "public";
        private static string _username = "";
        private static string _password = "";
        private static double _interval = 1;

        private const int MaxQueueSize = 30;

        private float _lastSendTime;
        private Thread _senderThread;
        private volatile bool _isRunning;
        private bool _initialized;
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);

        // Error backoff
        private volatile bool _inBackoff;
        private DateTime _backoffUntil;
        private int _consecutiveErrors;

        private void Start()
        {
            Debug.Log("[GreptimeExporter] Initializing (waiting for DataExport)");
        }

        /// <summary>
        ///     Reads config after DataExport.CfgPath is available
        /// </summary>
        private void Initialize()
        {
            _initialized = true;

            IsEnabled = Config.GetValue(DataExport.CfgPath, "greptimeEnabled");

            string url = Config.GetValueString(DataExport.CfgPath, "greptimeUrl");
            if (!string.IsNullOrEmpty(url))
            {
                // Avoid DNS resolution in Unity Mono — replace localhost with 127.0.0.1
                _url = url.TrimEnd('/').Replace("://localhost", "://127.0.0.1");
            }

            string db = Config.GetValueString(DataExport.CfgPath, "greptimeDatabase");
            if (!string.IsNullOrEmpty(db))
                _database = db;

            _username = Config.GetValueString(DataExport.CfgPath, "greptimeUsername");
            _password = Config.GetValueString(DataExport.CfgPath, "greptimePassword");

            string intervalStr = Config.GetValueString(DataExport.CfgPath, "greptimeInterval");
            if (!string.IsNullOrEmpty(intervalStr) && double.TryParse(intervalStr, out double parsed) && parsed >= 0.5)
                _interval = parsed;

            Debug.Log($"[GreptimeExporter] Config loaded, enabled={IsEnabled}");

            if (IsEnabled)
            {
                StartSender();
            }
        }

        private void StartSender()
        {
            _isRunning = true;
            _senderThread = new Thread(SendLoop) { IsBackground = true };
            _senderThread.Start();
            Debug.Log($"[GreptimeExporter] Started, sending to {_url} db={_database} every {_interval}s");
        }

        private void StopSender()
        {
            _isRunning = false;
            _signal.Set();

            if (_senderThread != null && _senderThread.IsAlive)
            {
                _senderThread.Join(2000);
            }

            Debug.Log("[GreptimeExporter] Stopped");
        }

        private void FixedUpdate()
        {
            // Defer initialization until DataExport.CfgPath is ready
            if (!_initialized)
            {
                if (string.IsNullOrEmpty(DataExport.CfgPath)) return;
                Initialize();
            }

            if (!IsEnabled) return;
            if (DataExport.LoggableValues == null || DataExport.ActVess == null) return;

            float now = Time.unscaledTime;
            if (now < _lastSendTime + (float)_interval) return;
            _lastSendTime = now;

            // Drop data if queue is full or in error backoff
            if (_queue.Count >= MaxQueueSize || _inBackoff) return;

            string payload = BuildLineProtocol();
            if (!string.IsNullOrEmpty(payload))
            {
                _queue.Enqueue(payload);
                _signal.Set();
            }
        }

        /// <summary>
        ///     Builds InfluxDB Line Protocol payload from current telemetry values.
        ///     Each metric becomes its own measurement (table) for PromQL compatibility.
        /// </summary>
        private string BuildLineProtocol()
        {
            long timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string vesselName = EscapeTagValue(DataExport.ActVess.GetDisplayName());

            var sb = new StringBuilder();

            foreach (var lv in DataExport.LoggableValues)
            {
                if (!lv.Logging) continue;

                string raw = lv.Value();
                if (string.IsNullOrEmpty(raw)) continue;
                if (!double.TryParse(raw, out double numericValue)) continue;

                string measurement = "ksp_" + ConvertToFieldKey(lv.Name);
                sb.Append($"{measurement},vessel={vesselName} value={numericValue} {timestampMs}\n");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        ///     Converts a human-readable name to a snake_case field key
        /// </summary>
        private static string ConvertToFieldKey(string name)
        {
            string key = name.ToLower()
                .Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("/", "_per_")
                .Replace("^", "")
                .Replace("-", "_")
                .Replace(".", "_");

            var sb = new StringBuilder();
            foreach (char c in key)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Escapes special characters in InfluxDB tag values (spaces, commas, equals)
        /// </summary>
        private static string EscapeTagValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "unknown";
            return value
                .Replace(" ", "\\ ")
                .Replace(",", "\\,")
                .Replace("=", "\\=");
        }

        /// <summary>
        ///     Background loop that dequeues and sends data to GreptimeDB
        /// </summary>
        private void SendLoop()
        {
            while (_isRunning)
            {
                _signal.WaitOne(5000);
                if (!_isRunning) break;

                // Check backoff
                if (_inBackoff)
                {
                    if (DateTime.UtcNow < _backoffUntil)
                        continue;
                    _inBackoff = false;
                }

                // Batch all queued payloads
                var batch = new StringBuilder();
                while (_queue.TryDequeue(out string payload))
                {
                    batch.Append(payload).Append('\n');
                }

                string data = batch.ToString().TrimEnd();
                if (string.IsNullOrEmpty(data)) continue;

                try
                {
                    PostData(data);
                    _consecutiveErrors = 0;
                }
                catch (Exception e)
                {
                    _consecutiveErrors++;
                    // Exponential backoff: 5s, 10s, 20s, 40s, max 60s
                    int backoffSeconds = Math.Min(5 * (1 << (_consecutiveErrors - 1)), 60);
                    _backoffUntil = DateTime.UtcNow.AddSeconds(backoffSeconds);
                    _inBackoff = true;
                    Debug.LogWarning($"[GreptimeExporter] Send failed (retry in {backoffSeconds}s): {e.Message}");
                }
            }
        }

        /// <summary>
        ///     POSTs InfluxDB Line Protocol data to GreptimeDB
        /// </summary>
        private void PostData(string lineProtocolData)
        {
            string endpoint = $"{_url}/v1/influxdb/api/v2/write?db={_database}&precision=ms";

            var request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "text/plain; charset=utf-8";
            request.Timeout = 3000;
            request.ReadWriteTimeout = 3000;
            request.KeepAlive = true;
            request.ServicePoint.ConnectionLimit = 2;

            if (!string.IsNullOrEmpty(_username))
            {
                request.Headers["Authorization"] = $"token {_username}:{_password}";
            }

            byte[] body = Encoding.UTF8.GetBytes(lineProtocolData);
            request.ContentLength = body.Length;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if ((int)response.StatusCode >= 300)
                {
                    Debug.LogError($"[GreptimeExporter] HTTP {response.StatusCode}: {response.StatusDescription}");
                }
            }
        }

        private void OnDestroy()
        {
            StopSender();
        }

        private void OnApplicationQuit()
        {
            StopSender();
        }
    }
}
