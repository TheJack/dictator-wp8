using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace DictatorApp
{
    public partial class PlayPage : PhoneApplicationPage
    {
        public PlayPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains("username"))
            {
                UserNameTextBox.Text = settings["username"] + "";
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                PlayButton.IsEnabled = false;
            }
            else
            {
                PlayButton.IsEnabled = true;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                ErrorTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                WaitingBar.Visibility = System.Windows.Visibility.Visible;
                WaitingBar.IsIndeterminate = true;
                WaitingBar.IsEnabled = true;
                PlayButton.IsEnabled = false;
                IsolatedStorageSettings.ApplicationSettings["username"] = UserNameTextBox.Text;
                NetworkManager.GameStarted += NetworkManager_GameStarted;
                NetworkManager.ConnectError += NetworkManager_ConnectError;
                NetworkManager.InitializeSession(UserNameTextBox.Text);
            }
        }

        void NetworkManager_ConnectError(object sender, MessageEventArgs e)
        {
            NetworkManager.ConnectError -= NetworkManager_ConnectError;
            NetworkManager.Disconnect();
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.ErrorTextBlock.Text = "Error connecting to server: " + e.Message;
                this.ErrorTextBlock.Visibility = System.Windows.Visibility.Visible;
                this.WaitingBar.Visibility = System.Windows.Visibility.Collapsed;
                this.PlayButton.IsEnabled = true;
            });
        }

        void NetworkManager_GameStarted(object sender, GameStartedEventArgs e)
        {
            NetworkManager.GameStarted -= NetworkManager_GameStarted;
            GameManager.StartNewGame(e.Players);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NavigationService.Navigate(new Uri("/InGamePage.xaml", UriKind.Relative));
                });
        }
    }
}