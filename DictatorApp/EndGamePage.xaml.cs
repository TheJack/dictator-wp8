using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace DictatorApp
{
    public partial class EndGame : PhoneApplicationPage
    {
        public EndGame()
        {
            InitializeComponent();
        }

        class comparer : IComparer<PlayerStats>
        {

            public int Compare(PlayerStats x, PlayerStats y)
            {
                return y.Score.CompareTo(x.Score);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            PlayerStats[] stats = GameManager.Stats.ToArray();
            Array.Sort(stats, new comparer());
            for (int i = 0; i < stats.Length; i++)
            {
                stats[i].Place = i + 1;
            }

            this.OpponentsScoreControl.ItemsSource = stats;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }
    }
}