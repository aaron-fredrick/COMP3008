using System;

namespace Chat.Client.Shared.ViewModels
{
    public class SystemMessageViewModel : ConversationItemViewModel
    {
        private readonly DateTime _utcTimestamp;

        public string Text { get; }

        public override DateTime Timestamp => _utcTimestamp.ToLocalTime();

        public SystemMessageViewModel(string text, DateTime utcTimestamp)
        {
            Text = text;
            _utcTimestamp = utcTimestamp;
        }
    }
}
