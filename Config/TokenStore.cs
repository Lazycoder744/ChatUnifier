using System;
using System.IO;
using Newtonsoft.Json;

namespace ChatUnifier.Config
{
    public class PlatformTokens
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; } = "";

        [JsonProperty("expires_at")]
        public long ExpiresAt { get; set; } = 0;

        [JsonIgnore]
        public bool IsExpired => ExpiresAt > 0 && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= ExpiresAt;

        [JsonIgnore]
        public bool HasToken => !string.IsNullOrEmpty(AccessToken);
    }

    public class TokenStoreData
    {
        [JsonProperty("youtube")]
        public PlatformTokens YouTube { get; set; } = new PlatformTokens();
    }

    public static class TokenStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatUnifier", "tokens.json"
        );

        private static TokenStoreData _data;

        public static TokenStoreData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    _data = JsonConvert.DeserializeObject<TokenStoreData>(File.ReadAllText(FilePath)) ?? new TokenStoreData();
                else
                    _data = new TokenStoreData();
            }
            catch
            {
                _data = new TokenStoreData();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(_data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error($"[TokenStore] Failed to save: {ex.Message}");
            }
        }
    }
}
