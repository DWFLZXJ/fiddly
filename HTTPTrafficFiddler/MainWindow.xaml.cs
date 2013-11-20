using System;
using System.Windows;
using System.Reflection;

using HTTPTrafficFiddler.Interfaces;
using HTTPTrafficFiddler.Classes;
using HTTPTrafficFiddler.Components;
using HTTPTrafficFiddler.Filters;

using SharpPcap;

namespace HTTPTrafficFiddler
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // check for WinPcap
            if (Pcap.Version.Contains("pcap is not installed"))
            {
                MessageBox.Show("Please install the latest version of WinPcap (http://www.winpcap.org/).", "WinPcap", 
                    MessageBoxButton.OK, MessageBoxImage.Error);

                Close();
                return;
            }

            var app = Assembly.GetEntryAssembly().GetName();

            // display current assembly version in windows' title
            Title += String.Format(" {0}.{1}", app.Version.Major, app.Version.Minor);

            // bind debugging information to GUI control 
            DebugInformation.Bind(DebugInfoBox);

            // bind status information to GUI control
            StatusInformation.Bind(CurrentStatus);            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // load network devices
            ComboInterfaces.ItemsSource = NetworkInterfaceList.Instance.GetList();
            ComboInterfaces.SelectedItem = NetworkInterfaceList.Instance.GetBestInterface();

            DebugInformation.WriteLine("Finished reading network interfaces.");

            // prepare PacketDispatcher, add default filters
            PacketDispatcher.Create();
            AddDefaultFilters();

            // connect filter list to GUI
            GridFilters.ItemsSource = PacketDispatcher.Instance.GetPacketFilters();
        }

        private void AddDefaultFilters()
        {
            // keyword "nil, programerski, izziv" -> "http://www.nil.si/"
            var filterK = new RedirectFilter("NIL", true);

            filterK.RedirectType = RedirectFilterType.Keywords;
            filterK.RedirectString = "nil,programerski,izziv";
            filterK.RedirectTarget = "http://www.nil.si";

            // keyword "uni-lj" -> "http://www.fri.uni-lj.si/"
            var filterF = new RedirectFilter("FRI", true);

            filterF.RedirectType = RedirectFilterType.Keywords;
            filterF.RedirectString = "uni-lj";
            filterF.RedirectTarget = "http://www.fri.uni-lj.si";

            // url "http://www.nlb.si/" -> "http://www.cert.si" 
            var filterU = new RedirectFilter("NLB", false);

            filterU.RedirectType = RedirectFilterType.URL;
            filterU.RedirectString = "http://www.nlb.si/";
            filterU.RedirectTarget = "http://www.cert.si";

            // regex "http://(www.)?google.[a-z]{2,3}/" -> "http://www.najdi.si" 
            var filterR = new RedirectFilter("Google", false);

            filterR.RedirectType = RedirectFilterType.Regex;
            filterR.RedirectString = "http://(www.)?google.[a-z]{2,3}/";
            filterR.RedirectTarget = "http://www.najdi.si";

            PacketDispatcher.Instance.AddPacketFilter(filterK);
            PacketDispatcher.Instance.AddPacketFilter(filterF);
            PacketDispatcher.Instance.AddPacketFilter(filterU);
            PacketDispatcher.Instance.AddPacketFilter(filterR);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (PacketDispatcher.Instance != null && PacketDispatcher.Instance.IsStarted())
            {
                PacketDispatcher.Instance.Stop();
            }
        }

        private void ButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!PacketDispatcher.Instance.IsStarted())
            {
                if (!PacketDispatcher.Instance.Bind((NetworkInterface)ComboInterfaces.SelectedItem))
                {
                    MessageBox.Show("Please select an interface with a valid gateway address.", "Interface error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                PacketDispatcher.Instance.Start();

                ButtonStartStop.Content = "Stop";
                StatusInformation.ChangeStatus("Status: traffic fiddler started!", true);
            }
            else
            {
                PacketDispatcher.Instance.Stop();

                ButtonStartStop.Content = "Start";
                StatusInformation.ChangeStatus("Status: traffic fiddler stopped!", false);
            }
        }

        private void RButtonAddRedirectFilter_Click(object sender, RoutedEventArgs e)
        {
            var filterWindow = new RedirectFilterWindow();

            if ((bool)filterWindow.ShowDialog())
            {
                PacketDispatcher.Instance.AddPacketFilter(filterWindow.GetFilter());
            }
        }

        private void FilterEdit_Click(object sender, RoutedEventArgs e)
        {
            var filter = (IPacketFilter)GridFilters.SelectedItem;
            filter.EditFilter();
        }

        private void FilterRemove_Click(object sender, RoutedEventArgs e)
        {
            var filter = (IPacketFilter)GridFilters.SelectedItem;
            PacketDispatcher.Instance.RemovePacketFilter(filter);
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }        
    }
}
