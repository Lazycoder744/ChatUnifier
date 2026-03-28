using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ChatUnifier.Auth;
using ChatUnifier.Chat;
using ChatUnifier.Config;

namespace ChatUnifier.Platforms
{
    public class YouTubeClient
    {
        private readonly UnifiedChatService _service;
        private CancellationTokenSource _cts;
        private static readonly HttpClient Http = new HttpClient();
        private ChatChannel _channel;

        public YouTubeClient(UnifiedChatService service)
        {
            _service = service;
        }

        public void Connect()
        {
            _cts = new CancellationTokenSource();
            Plugin.Log?.Info("[YouTube] Connect requested");
            Task.Run(PollLoopAsync);
        }

        public void Disconnect()
        {
            _cts?.Cancel();
        }

        private async Task PollLoopAsync()
        {
            var tokens = TokenStore.Data.YouTube;
            if (!tokens.HasToken) return;

            Plugin.Log?.Info("[YouTube] Starting chat loop");

            if (tokens.IsExpired)
            {
                var refreshed = await OAuthManager.RefreshGoogleTokenAsync();
                if (!refreshed)
                {
                    Plugin.Log?.Error("[YouTube] Token expired and refresh failed.");
                    _service.FireSystemMessage("[ChatUnifier] YouTube token expired. Please re-authenticate in the local dashboard.");
                    return;
                }
                tokens = TokenStore.Data.YouTube;
            }

            var liveChatId = await GetActiveLiveChatIdAsync(tokens.AccessToken);
            if (string.IsNullOrEmpty(liveChatId))
            {
                Plugin.Log?.Warn("[YouTube] No active livestream found.");
                _service.FireSystemMessage("[ChatUnifier] No active YouTube livestream detected.");
                return;
            }

            var channelTitle = await GetChannelTitleAsync(tokens.AccessToken);
            _channel = new ChatChannel(liveChatId, channelTitle ?? "YouTube");
            _service.FireLogin();
            _service.FireJoinChannel(_channel);

            Plugin.Log?.Info($"[YouTube] Connected to live chat: {liveChatId}");

            string pageToken = null;
            int pollingIntervalMs = 5000;

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (TokenStore.Data.YouTube.IsExpired)
                    {
                        await OAuthManager.RefreshGoogleTokenAsync();
                        tokens = TokenStore.Data.YouTube;
                    }

                    var (messages, nextPageToken, nextInterval) =
                        await FetchMessagesAsync(liveChatId, tokens.AccessToken, pageToken);

                    pageToken = nextPageToken;
                    if (nextInterval > 0) pollingIntervalMs = nextInterval;

                    foreach (var msg in messages)
                        _service.FireTextMessage(msg);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Warn($"[YouTube] Poll error: {ex.Message}");
                }

                await Task.Delay(pollingIntervalMs, _cts.Token).ContinueWith(_ => { });
            }

            _service.FireLeaveChannel(_channel);
        }

        private async Task<string> GetActiveLiveChatIdAsync(string accessToken)
        {
            try
            {
                var url = "https://www.googleapis.com/youtube/v3/liveBroadcasts"
                        + "?part=snippet&broadcastStatus=active&broadcastType=all&maxResults=1";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Plugin.Log?.Warn($"[YouTube] liveBroadcasts.list failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                return json["items"]?[0]?["snippet"]?["liveChatId"]?.Value<string>();
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"[YouTube] GetActiveLiveChatId error: {ex.Message}");
                return null;
            }
        }

        private async Task<string> GetChannelTitleAsync(string accessToken)
        {
            try
            {
                var url = "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await Http.SendAsync(request);
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                return json["items"]?[0]?["snippet"]?["title"]?.Value<string>();
            }
            catch { return null; }
        }

        private async Task<(ChatMessage[] messages, string nextPageToken, int pollingIntervalMs)>
            FetchMessagesAsync(string liveChatId, string accessToken, string pageToken)
        {
            var url = $"https://www.googleapis.com/youtube/v3/liveChat/messages"
                    + $"?liveChatId={Uri.EscapeDataString(liveChatId)}&part=snippet,authorDetails&maxResults=200";

            if (!string.IsNullOrEmpty(pageToken))
                url += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                Plugin.Log?.Warn($"[YouTube] liveChatMessages.list failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            var items = json["items"] as JArray ?? new JArray();
            var results = new System.Collections.Generic.List<ChatMessage>();

            foreach (var item in items)
            {
                var snippet = item["snippet"];
                var author = item["authorDetails"];

                if (snippet?["type"]?.Value<string>() != "textMessageEvent") continue;

                var msgId = item["id"]?.Value<string>() ?? Guid.NewGuid().ToString();
                var msgText = snippet["displayMessage"]?.Value<string>() ?? "";
                var userId = author?["channelId"]?.Value<string>()
                          ?? snippet?["authorChannelId"]?.Value<string>()
                          ?? Guid.NewGuid().ToString();
                var displayName = author?["displayName"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = author?["displayName"]?.ToString();
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = $"YouTubeUser-{userId.Substring(0, Math.Min(8, userId.Length))}";
                var isMod = author?["isChatModerator"]?.Value<bool>() ?? false;
                var isOwner = author?["isChatOwner"]?.Value<bool>() ?? false;
                var user = new ChatUser(userId, displayName, displayName, "#FF0000", isOwner, isMod);
                results.Add(new ChatMessage(msgId, msgText, user, _channel ?? new ChatChannel(liveChatId, "YouTube")));
            }

            var nextToken = json["nextPageToken"]?.Value<string>();
            var intervalMs = json["pollingIntervalMillis"]?.Value<int>() ?? 5000;

            return (results.ToArray(), nextToken, intervalMs);
        }
    }
}