using CP_SDK.Chat.Interfaces;

namespace ChatUnifier.Chat
{
    public class ChatMessage : IChatMessage
    {
        public string        Id              { get; }
        public bool          IsSystemMessage { get; }
        public bool          IsActionMessage { get; }
        public bool          IsHighlighted   { get; }
        public bool          IsGiganticEmote { get; }
        public bool          IsPing          { get; }
        public string        Message         { get; }
        public IChatUser     Sender          { get; }
        public IChatChannel  Channel         { get; }
        public IChatEmote[]  Emotes          { get; }

        public ChatMessage(
            string      id,
            string      message,
            IChatUser   sender,
            IChatChannel channel,
            bool isSystemMessage = false,
            bool isActionMessage = false,
            bool isHighlighted   = false,
            bool isGiganticEmote = false,
            bool isPing          = false)
        {
            Id              = string.IsNullOrWhiteSpace(id) ? System.Guid.NewGuid().ToString() : id;
            Message         = message ?? string.Empty;
            Sender          = sender;
            Channel         = channel;

            Emotes          = new IChatEmote[0];

            IsSystemMessage = isSystemMessage;
            IsActionMessage = isActionMessage;
            IsHighlighted   = isHighlighted;
            IsGiganticEmote = isGiganticEmote;
            IsPing          = isPing;
        }
    }
}