using IPA;
using IPA.Logging;
using ChatUnifier.Chat;
using ChatUnifier.Web;
using System.Diagnostics;

namespace ChatUnifier
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Logger Log { get; private set; }

        private LocalWebServer _webServer;
        private UnifiedChatService _chatService;

        [Init]
        public Plugin(Logger logger)
        {
            Log = logger;
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log?.Info("[ChatUnifier] Starting plugin");
            _chatService = new UnifiedChatService();
            _webServer = new LocalWebServer(42069, _chatService);

            CP_SDK.Chat.Service.RegisterExternalService(_chatService);

            _chatService.Start();
            _webServer.Start();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:42069",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            _webServer?.Stop();
            _chatService?.Stop();
        }
    }
}
