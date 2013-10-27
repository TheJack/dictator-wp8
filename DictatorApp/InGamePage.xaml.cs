using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Windows.Media;
using Windows.Phone.Speech.Synthesis;
using System.Diagnostics;
using System.Windows.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace DictatorApp
{
    public partial class InGamePage : PhoneApplicationPage
    {
        Round currentRound = null;
        BackgroundWorker worker;
        static readonly SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green);
        static readonly SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);
        readonly object syncObj = new object();
        static Brush defaultBrush;

        public InGamePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationService.RemoveBackEntry();

            defaultBrush = this.TimerTextBlock.Foreground;
            NetworkManager.GameEnded += NetworkManager_GameEnded;
            OpponentsScoreControl.ItemsSource = GameManager.Stats;
            MyScoreTextBlock.DataContext = GameManager.MyStats;
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerAsync();
        }

        void NetworkManager_GameEnded(object sender, EventArgs e)
        {
            NetworkManager.GameEnded -= NetworkManager_GameEnded;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/EndGamePage.xaml", UriKind.Relative));
            });
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            worker.CancelAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!worker.CancellationPending)
            {
                if (currentRound == null)
                {
                    lock (syncObj)
                    {
                        currentRound = GameManager.GetNextRound();
                    }
                    if (currentRound != null)
                    {
                        currentRound.StartedAt = DateTime.Now;
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            this.AnswerTextBox.Text = "";
                            this.AnswerTextBox.IsEnabled = true;
                            this.RoundTextBlock.Text = "Round " + currentRound.RoundNumber;
                        });
                        SpeekCurrentRound();
                    }
                }
                if (currentRound != null)
                {
                    if (currentRound.EndedAt > DateTime.MinValue)
                    {
                        if ((DateTime.Now - currentRound.EndedAt) > TimeSpan.FromSeconds(1))
                        {
                            lock (syncObj)
                            {
                                currentRound = null;
                            }
                        }
                    }
                    else if (currentRound.StartedAt.AddSeconds(currentRound.Timeout) < DateTime.Now)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            TimerTextBlock.Text = "00:00";
                        });
                        lock (syncObj)
                        {
                            currentRound.EndedAt = DateTime.Now;
                        }
                        //TimerTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        TimeSpan timeLeft = currentRound.StartedAt.AddSeconds(currentRound.Timeout) - DateTime.Now;
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            TimerTextBlock.Text = String.Format("{0:D2}:{1:D2}", timeLeft.Minutes, timeLeft.Seconds);
                            if (timeLeft.Seconds < 5 && !currentRound.Answered)
                            {
                                this.TimerTextBlock.Foreground = redBrush;
                            }
                            else if (!currentRound.Answered)
                            {
                                this.TimerTextBlock.Foreground = defaultBrush;
                            }
                        });
                    }

                }

                System.Threading.Thread.Sleep(100);
            }
        }

        private async void SpeekCurrentRound()
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();
            await synth.SpeakTextAsync(currentRound.Word);
            synth.Dispose();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            lock (syncObj)
            {
                if (currentRound != null && currentRound.EndedAt == DateTime.MinValue && !currentRound.Answered)
                {
                    GameManager.Answer(currentRound.RoundNumber, this.AnswerTextBox.Text);
                    currentRound.Answered = true;
                    this.AnswerTextBox.IsEnabled = false;
                    this.TimerTextBlock.Foreground = greenBrush;
                }
            }
        }

        bool isTyping = false;
        BackgroundWorker typingWorker;
        private void AnswerTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!isTyping)
            {
                NetworkManager.Send("update_typing_state,1");
                typingWorker = new BackgroundWorker();
                isTyping = true;
            }
            else
            {
                typingWorker.CancelAsync();
                typingWorker = new BackgroundWorker();
            }

            typingWorker.WorkerSupportsCancellation = true;
            typingWorker.DoWork += (s, a) =>
            {
                BackgroundWorker sndr = (BackgroundWorker)s;
                Thread.Sleep(1500);
                if (!sndr.CancellationPending)
                {
                    isTyping = false;
                    NetworkManager.Send("update_typing_state,0");
                }
            };
            typingWorker.RunWorkerAsync();
        }
    }



    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;
            var isVisible = (bool)value;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visiblity = (Visibility)value;
            return visiblity == Visibility.Visible;
        }
    }

}