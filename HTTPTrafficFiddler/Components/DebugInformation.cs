using System;
using System.Windows.Controls;

namespace HTTPTrafficFiddler.Components
{
    static class DebugInformation
    {
        private static TextBox outputControl;

        public static void Bind(TextBox control) 
        {
            outputControl = control;
            outputControl.Text = "";
        }

        public static void WriteLine(String input)
        {
            if (outputControl == null) return;

            var date = DateTime.Now.ToString("HH:mm:ss: ");

            Action update = delegate() {
                if (outputControl.Text.Length != 0) outputControl.AppendText("\n");

                outputControl.AppendText(date + input);
                outputControl.ScrollToEnd();
            };

            outputControl.Dispatcher.BeginInvoke(update);           
        }
    }
}
