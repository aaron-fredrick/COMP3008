using System.Windows;
using System.Windows.Controls;
using Chat.Client.Shared.ViewModels;

namespace Chat.Client.Shared.Selectors
{
    public class ConversationItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MessageTemplate { get; set; }
        public DataTemplate SystemMessageTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SystemMessageViewModel)
            {
                return SystemMessageTemplate;
            }
            if (item is MessageViewModel)
            {
                return MessageTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
