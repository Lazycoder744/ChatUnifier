using CP_SDK.Chat.Interfaces;

namespace ChatUnifier.Chat
{
    public class ChatChannel : IChatChannel
    {
        public string Id          { get; }
        public string Name        { get; }
        public int    ViewerCount { get; set; } = 0;
        public bool   IsTemp      { get; set; } = false;
        public string Prefix      { get; set; } = "";
        public bool   CanSendMessages { get; set; } = false;
        public bool   Live        { get; set; } = false;

        public ChatChannel(string id, string name)
        {
            Id   = id;
            Name = name;
        }
    }
}