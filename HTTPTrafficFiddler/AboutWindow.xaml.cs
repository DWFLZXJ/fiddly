using System;
using System.Windows;
using System.Reflection;

namespace HTTPTrafficFiddler
{
	public partial class AboutWindow : Window
	{
        public AboutWindow()
		{
			InitializeComponent();

            var app = Assembly.GetEntryAssembly().GetName();

            // display current assembly version
            TextVersion.Text = String.Format("{0}.{1}.{2}", app.Version.Major, app.Version.Minor, app.Version.Build);
		}
	}
}