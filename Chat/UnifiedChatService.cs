using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CP_SDK.Chat.Interfaces;
using CP_SDK.Unity;
using UnityEngine;
using ChatUnifier.Platforms;

namespace ChatUnifier.Chat
{
    public class UnifiedChatService : IChatService
    {
        // Identity
        public string DisplayName => "ChatUnifier";
        public Color  AccentColor => new Color(0.82f, 0.04f, 0.04f); // Edit these values to change badge colour, set to a nice red rn

        // Channels
        public ReadOnlyCollection<(IChatService, IChatChannel)> Channels =>
            _channels.AsReadOnly();

        private readonly List<(IChatService, IChatChannel)> _channels =
            new List<(IChatService, IChatChannel)>();

        // Events required by the IChatService
        public event Action<IChatService, IChatMessage>  OnTextMessageReceived;
        public event Action<IChatService>                OnLogin;
        public event Action<IChatService, IChatChannel>  OnJoinChannel;
        public event Action<IChatService, IChatChannel>  OnLeaveChannel;
        public event Action<IChatService, string>        OnSystemMessage;
        public event Action<IChatService, IChatChannel>  OnRoomStateUpdated;
        public event Action<IChatService, IChatChannel, bool, int> OnLiveStatusUpdated;
        public event Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>> OnChannelResourceDataCached;
        public event Action<IChatService, IChatChannel, IChatUser> OnChannelFollow;
        public event Action<IChatService, IChatChannel, IChatUser, int> OnChannelBits;
        public event Action<IChatService, IChatChannel, IChatUser, IChatChannelPointEvent> OnChannelPoints;
        public event Action<IChatService, IChatChannel, IChatUser, IChatSubscriptionEvent> OnChannelSubscription;
        public event Action<IChatService, IChatChannel, IChatUser, int> OnChannelRaid;
        public event Action<IChatService, string> OnChatCleared;
        public event Action<IChatService, string> OnMessageCleared;

        // Platform clients
        private YouTubeClient _youtube;

        // Cycle
        public void Start()
        {
            _youtube = new YouTubeClient(this);
        }

        public void Stop()
        {
            _youtube?.Disconnect();
            _channels.Clear();
        }

        public void ConnectYouTube() => _youtube?.Connect();

        // IChatService: emote cache - not verified working yet
        public void RecacheEmotes() { /* no-op — no emote cache in this service */ }

        // IChatService: ChatPlex settings web page (unused here)
        public string WebPageHTML()       => "";
        public string WebPageHTMLForm()   => "";
        public string WebPageJS()         => "";
        public string WebPageJSValidate() => "";
        public void   WebPageOnPost(Dictionary<string, string> p) { /* no-op */ }

        // IChatService: queries
        public bool   IsConnectedAndLive()  => _channels.Count > 0;
        public string PrimaryChannelName()  => _channels.Count > 0 ? _channels[0].Item2.Name : "";

        // IChatService: messaging
        public void SendTextMessage(IChatChannel channel, string message) { /* no-op */ }

        // IChatService: temp channels
        public void JoinTempChannel(string id, string name, string prefix, bool canSend)
        {
            var ch = new ChatChannel(id, name)
            {
                Prefix = prefix,
                CanSendMessages = canSend,
                IsTemp = true
            };
            FireJoinChannel(ch);
        }

        public void LeaveTempChannel(string channelName)
        {
            var idx = _channels.FindIndex(t => t.Item2.IsTemp && (t.Item2.Id == channelName || t.Item2.Name == channelName));
            if (idx >= 0)
            {
                var ch = _channels[idx].Item2;
                _channels.RemoveAt(idx);
                MTMainThreadInvoker.Enqueue(() => OnLeaveChannel?.Invoke(this, ch));
            }
        }

        public bool IsInTempChannel(string channelName) =>
            _channels.Exists(t => t.Item2.IsTemp && (t.Item2.Id == channelName || t.Item2.Name == channelName));

        public void LeaveAllTempChannel(string prefix)
        {
            var toLeave = _channels.FindAll(t => t.Item2.IsTemp && t.Item2.Prefix == prefix);
            foreach (var entry in toLeave)
            {
                _channels.Remove(entry);
                var ch = entry.Item2;
                MTMainThreadInvoker.Enqueue(() => OnLeaveChannel?.Invoke(this, ch));
            }
        }

        // Internal fire helpers (called by YouTubeClient)
        internal void FireLogin() =>
            MTMainThreadInvoker.Enqueue(() => OnLogin?.Invoke(this));

        internal void FireJoinChannel(IChatChannel channel)
        {
            if (!_channels.Exists(t => t.Item2.Id == channel.Id))
                _channels.Add((this, channel));
            MTMainThreadInvoker.Enqueue(() => OnJoinChannel?.Invoke(this, channel));
        }

        internal void FireLeaveChannel(IChatChannel channel)
        {
            _channels.RemoveAll(t => t.Item2.Id == channel.Id);
            MTMainThreadInvoker.Enqueue(() => OnLeaveChannel?.Invoke(this, channel));
        }

        internal void FireSystemMessage(string message) =>
            MTMainThreadInvoker.Enqueue(() => OnSystemMessage?.Invoke(this, message));

        internal void FireTextMessage(IChatMessage message) =>
            MTMainThreadInvoker.Enqueue(() => OnTextMessageReceived?.Invoke(this, message));
    }
}