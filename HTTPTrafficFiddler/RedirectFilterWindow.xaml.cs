using System;
using System.Text.RegularExpressions;
using System.Windows;

using HTTPTrafficFiddler.Filters;

namespace HTTPTrafficFiddler
{
	public partial class RedirectFilterWindow : Window
	{
        private RedirectFilter filter;
        private bool loadedFilterState;

        public RedirectFilterWindow()
		{
			InitializeComponent();

            // set new filters to be enabled by default
            loadedFilterState = true;

            FilterName.Focus();
		}

        public RedirectFilterWindow(RedirectFilter currentFilter) : this()
        {
            LoadFilter(currentFilter);
        }

        public RedirectFilter GetFilter()
        {
            return filter;
        }

        private void LoadFilter(RedirectFilter currentFilter)
        {
            loadedFilterState = currentFilter.Enabled;

            TextTitle.Text = "EDIT REDIRECT FILTER";

            FilterName.Text = currentFilter.Name;
            RedirectString.Text = currentFilter.RedirectString;
            RedirectTarget.Text = currentFilter.RedirectTarget;

            RedirectType.SelectedIndex = (int)currentFilter.RedirectType;          
        }
       
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            // check filter name
            if (FilterName.Text.Equals(String.Empty))
            {
                MessageBox.Show("Please enter a filter name.",
                    "Empty field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // check redirect string
            if (RedirectString.Text.Equals(String.Empty))
            {
                MessageBox.Show("Please enter a redirect url/keywords/regex.",
                    "Empty field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var targetURL = RedirectTarget.Text;

            // check redirect string - validate regex, prevent loops
            if((RedirectFilterType)RedirectType.SelectedIndex == RedirectFilterType.Regex) {
                Regex regex;
                
                try
                {
                    regex = new Regex(RedirectString.Text);
                } 
                catch 
                {
                    MessageBox.Show("Please enter a valid regular expression.",
                        "Validation error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (regex.IsMatch(targetURL))
                {
                    MessageBox.Show("Current regular expression matches the target URL. Please change your regular expression or target URL to prevent redirection loops.",
                        "Possible redirection loop detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            // check redirect string - prevent loops from bad keyword matching
            else if ((RedirectFilterType)RedirectType.SelectedIndex == RedirectFilterType.Keywords)
            {
                var keywords = RedirectString.Text.Split(',');

                foreach (var keyword in keywords)
                {
                    if (targetURL.Contains(keyword))
                    {
                        MessageBox.Show("One of the entered keywords matches the target URL. Please change your keywords or target URL to prevent redirection loops.",
                            "Possible redirection loop detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            // check target URL
            if (RedirectTarget.Text.Equals(String.Empty))
            {
                MessageBox.Show("Please enter target URL.",
                    "Validation error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
                
            filter = new RedirectFilter();
            filter.Enabled = loadedFilterState;

            filter.Name = FilterName.Text;
            filter.RedirectType = (RedirectFilterType)RedirectType.SelectedIndex;
            filter.RedirectString = RedirectString.Text;
            filter.RedirectTarget = RedirectTarget.Text;

            DialogResult = true;

            Close();
        }
    }
}