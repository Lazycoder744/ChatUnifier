using CP_SDK.Chat.Interfaces;

namespace ChatUnifier.Chat
{
    public class ChatUser : IChatUser
    {
        public string        Id            { get; }
        public string        UserName      { get; }
        public string        DisplayName   { get; }
        public string        Color         { get; }
        public bool          IsBroadcaster { get; }
        public bool          IsModerator   { get; }
        public bool          IsSubscriber  { get; }
        public bool          IsTurbo       { get; }
        public bool          IsVip         { get; }
        public string        PaintedName   { get; set; } = "";
        public IChatBadge[]  Badges        { get; set; } = new IChatBadge[0];

        public ChatUser(
            string id,
            string userName,
            string displayName,
            string color        = "#FFFFFF",
            bool isBroadcaster  = false,
            bool isModerator    = false,
            bool isSubscriber   = false,
            bool isTurbo        = false,
            bool isVip          = false)
        {
            Id            = id;
            UserName      = userName;

            // Fix Failed: Attempt to give username, not working...
            DisplayName   = string.IsNullOrWhiteSpace(displayName) ? userName : displayName;

            Color         = color;
            IsBroadcaster = isBroadcaster;
            IsModerator   = isModerator;
            IsSubscriber  = isSubscriber;
            IsTurbo       = isTurbo;
            IsVip         = isVip;
        }
    }
}