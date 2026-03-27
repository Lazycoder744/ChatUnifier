using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ChatUnifier.Config;

namespace ChatUnifier.Auth
{
    public static class OAuthManager
    {
        private static readonly HttpClient Http = new HttpClient();

        public static async Task<bool> ExchangeGoogleCodeAsync(string code, int port)
        {
            try
            {
                var creds = AppCredentials.Data;
                var body = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]     = creds.GoogleClientId,
                    ["client_secret"] = creds.GoogleClientSecret,
                    ["code"]          = code,
                    ["grant_type"]    = "authorization_code",
                    ["redirect_uri"]  = $"http://localhost:{port}/callback/youtube"
                });

                var response = await Http.PostAsync("https://oauth2.googleapis.com/token", body);
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());

                if (!response.IsSuccessStatusCode)
                {
                    Plugin.Log?.Error($"[OAuth] Google token exchange failed: {json}");
                    return false;
                }

                var tokens = TokenStore.Data.YouTube;
                tokens.AccessToken  = json["access_token"]?.Value<string>() ?? "";
                tokens.RefreshToken = json["refresh_token"]?.Value<string>() ?? tokens.RefreshToken;
                tokens.ExpiresAt    = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                      + (json["expires_in"]?.Value<long>() ?? 3600L);
                TokenStore.Save();
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"[OAuth] Google exchange exception: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> RefreshGoogleTokenAsync()
        {
            try
            {
                var creds  = AppCredentials.Data;
                var tokens = TokenStore.Data.YouTube;

                if (string.IsNullOrEmpty(tokens.RefreshToken)) return false;

                var body = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]     = creds.GoogleClientId,
                    ["client_secret"] = creds.GoogleClientSecret,
                    ["grant_type"]    = "refresh_token",
                    ["refresh_token"] = tokens.RefreshToken
                });

                var response = await Http.PostAsync("https://oauth2.googleapis.com/token", body);
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());

                if (!response.IsSuccessStatusCode) return false;

                tokens.AccessToken = json["access_token"]?.Value<string>() ?? "";
                tokens.ExpiresAt   = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                     + (json["expires_in"]?.Value<long>() ?? 3600L);
                TokenStore.Save();
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"[OAuth] Google refresh exception: {ex.Message}");
                return false;
            }
        }

        public static async Task<string> GetGoogleEmailAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://www.googleapis.com/oauth2/v2/userinfo");
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                var response = await Http.SendAsync(request);
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                return json["email"]?.Value<string>() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
