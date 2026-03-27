using System;
using System.IO;
using Newtonsoft.Json;

namespace ChatUnifier.Config
{
    public class AppCredentialsData
    {
        [JsonProperty("google_client_id")]
        public string GoogleClientId { get; set; } = "";

        [JsonProperty("google_client_secret")]
        public string GoogleClientSecret { get; set; } = "";
    }

    public static class AppCredentials
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatUnifier", "credentials.json"
        );

        private static AppCredentialsData _data;

        public static AppCredentialsData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static bool GoogleConfigured =>
            !string.IsNullOrEmpty(Data.GoogleClientId) && !string.IsNullOrEmpty(Data.GoogleClientSecret);

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    _data = JsonConvert.DeserializeObject<AppCredentialsData>(File.ReadAllText(FilePath)) ?? new AppCredentialsData();
                else
                    _data = new AppCredentialsData();
            }
            catch
            {
                _data = new AppCredentialsData();
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
                Plugin.Log?.Error($"[AppCredentials] Failed to save: {ex.Message}");
            }
        }
    }
}
