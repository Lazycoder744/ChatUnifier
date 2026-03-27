using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using ChatUnifier.Auth;
using ChatUnifier.Chat;
using ChatUnifier.Config;

namespace ChatUnifier.Web
{
    public class LocalWebServer
    {
        private readonly int _port;
        private readonly UnifiedChatService _service;
        private HttpListener _listener;
        private CancellationTokenSource _cts;

        private bool _youtubeConnected;

        public LocalWebServer(int port, UnifiedChatService service)
        {
            _port = port;
            _service = service;
        }

        public void Start()
        {
            _youtubeConnected = TokenStore.Data.YouTube.HasToken;

            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();

            Plugin.Log?.Info($"[WebServer] Listening on http://localhost:{_port}/");
            Task.Run(AcceptLoopAsync);

            if (_youtubeConnected)
            {
                Plugin.Log?.Info("[WebServer] Existing YouTube token found, auto-connecting");
                _service.ConnectYouTube();
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(ctx));
                }
                catch (Exception ex) when (!_cts.IsCancellationRequested)
                {
                    Plugin.Log?.Warn($"[WebServer] Accept error: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;

            try
            {
                var path = req.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
                if (string.IsNullOrEmpty(path)) path = "/";

                if (path == "" || path == "/")
                {
                    ServeHtml(res, DashboardHtml.Build(
                        _port, _youtubeConnected, AppCredentials.GoogleConfigured));
                    return;
                }

                if (path == "/login/youtube")
                {
                    var creds = AppCredentials.Data;
                    var authUrl = "https://accounts.google.com/o/oauth2/v2/auth"
                        + $"?client_id={Uri.EscapeDataString(creds.GoogleClientId)}"
                        + $"&redirect_uri={Uri.EscapeDataString($"http://localhost:{_port}/callback/youtube")}"
                        + "&response_type=code"
                        + "&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fyoutube.readonly"
                        + "&access_type=offline"
                        + "&prompt=consent";
                    Redirect(res, authUrl);
                    return;
                }

                if (path == "/callback/youtube")
                {
                    var code = HttpUtility.ParseQueryString(req.Url.Query)["code"];
                    if (!string.IsNullOrEmpty(code))
                    {
                        var ok = OAuthManager.ExchangeGoogleCodeAsync(code, _port).GetAwaiter().GetResult();
                        if (ok)
                        {
                            _youtubeConnected = true;
                            _service.ConnectYouTube();
                        }
                    }
                    Redirect(res, "/");
                    return;
                }

                if (path == "/credentials/youtube" && req.HttpMethod == "POST")
                {
                    var body = ReadBody(req);
                    var json = JObject.Parse(body);
                    AppCredentials.Data.GoogleClientId     = json["client_id"]?.Value<string>() ?? "";
                    AppCredentials.Data.GoogleClientSecret = json["client_secret"]?.Value<string>() ?? "";
                    AppCredentials.Save();
                    ServeJson(res, "{\"ok\":true}");
                    return;
                }

                if (path == "/import/google-client" && req.HttpMethod == "POST")
                {
                    var body = ReadBody(req);
                    var json = JObject.Parse(body);

                    // Accept either our simple format: { client_id, client_secret }
                    // or Google's downloaded OAuth client JSON: { installed: { client_id, client_secret, ... } } or { web: { ... } }
                    var directId     = json["client_id"]?.Value<string>();
                    var directSecret = json["client_secret"]?.Value<string>();

                    if (string.IsNullOrEmpty(directId) || string.IsNullOrEmpty(directSecret))
                    {
                        var container = (JObject)(json["installed"] as JObject ?? json["web"] as JObject);
                        directId     = container?["client_id"]?.Value<string>();
                        directSecret = container?["client_secret"]?.Value<string>();
                    }

                    if (string.IsNullOrEmpty(directId) || string.IsNullOrEmpty(directSecret))
                    {
                        ServeJson(res, "{\"ok\":false,\"error\":\"Could not find client_id/client_secret in JSON.\"}");
                        return;
                    }

                    AppCredentials.Data.GoogleClientId     = directId;
                    AppCredentials.Data.GoogleClientSecret = directSecret;
                    AppCredentials.Save();
                    ServeJson(res, "{\"ok\":true}");
                    return;
                }

                if (path == "/import/tokens" && req.HttpMethod == "POST")
                {
                    var body = ReadBody(req);
                    var json = JObject.Parse(body);

                    // Expected format: { youtube: { access_token, refresh_token, expires_at } }
                    var yt = json["youtube"] as JObject;
                    if (yt == null)
                    {
                        ServeJson(res, "{\"ok\":false,\"error\":\"Expected JSON with a 'youtube' object.\"}");
                        return;
                    }

                    TokenStore.Data.YouTube.AccessToken  = yt["access_token"]?.Value<string>() ?? "";
                    TokenStore.Data.YouTube.RefreshToken = yt["refresh_token"]?.Value<string>() ?? "";
                    TokenStore.Data.YouTube.ExpiresAt    = yt["expires_at"]?.Value<long>() ?? 0;
                    TokenStore.Save();

                    _youtubeConnected = TokenStore.Data.YouTube.HasToken;
                    if (_youtubeConnected) _service.ConnectYouTube();

                    ServeJson(res, "{\"ok\":true}");
                    return;
                }

                if (path == "/disconnect/youtube" && req.HttpMethod == "POST")
                {
                    _youtubeConnected = false;
                    TokenStore.Data.YouTube = new PlatformTokens();
                    TokenStore.Save();
                    ServeJson(res, "{\"ok\":true}");
                    return;
                }

                if (path == "/status")
                {
                    ServeJson(res, $"{{\"youtube\":{_youtubeConnected.ToString().ToLower()}}}");
                    return;
                }

                res.StatusCode = 404;
                res.Close();
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"[WebServer] Request error: {ex.Message}");
                try { res.StatusCode = 500; res.Close(); } catch { }
            }
        }

        private static void ServeHtml(HttpListenerResponse res, string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            res.ContentType = "text/html; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.Close();
        }

        private static void ServeJson(HttpListenerResponse res, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json";
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.Close();
        }

        private static void Redirect(HttpListenerResponse res, string url)
        {
            res.StatusCode = 302;
            res.Headers["Location"] = url;
            res.Close();
        }

        private static string ReadBody(HttpListenerRequest req)
        {
            using (var reader = new System.IO.StreamReader(req.InputStream, req.ContentEncoding))
                return reader.ReadToEnd();
        }
    }
}
