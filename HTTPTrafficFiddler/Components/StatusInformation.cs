using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace HTTPTrafficFiddler.Components
{
    class StatusInformation
    {
        private static Color normalColor = Color.FromRgb(0, 52, 75);
        private static Color enabledColor = Color.FromRgb(35, 150, 35);

        private static TextBlock outputControl;

        public static void Bind(TextBlock control)
        {
            outputControl = control;

            outputControl.Foreground = new SolidColorBrush(normalColor);
            outputControl.Text = "Status: traffic fiddler stopped!";
        }

        public static void ChangeStatus(String text, bool enabled)
        {
            if (outputControl == null) return;

            Action update = delegate()
            {
                outputControl.Foreground = new SolidColorBrush(enabled ? enabledColor : normalColor);
                outputControl.Text = text;
            };

            outputControl.Dispatcher.BeginInvoke(update);
        }
    }
}
