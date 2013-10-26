using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DictatorApp
{
    class PlayerStats : INotifyPropertyChanged
    {
        private string name;
        private int score;
        private bool typing;

        public PlayerStats(string name, int score)
        {
            this.name = name;
            this.score = score;
            this.typing = false;
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    this.OnPropertyChanged("Name");
                }
            }
        }
        
        public int Score
        {
            get { return score; }
            set
            {
                if (score != value)
                {
                    score = value;
                    this.OnPropertyChanged("Score");
                }
            }
        }

        public bool Typing
        {
            get { return typing; }
            set
            {
                if (typing != value)
                
                {
                    typing = value;
                    this.OnPropertyChanged("Typing");
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => 
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    class Round
    {
        public string Word { get; private set; }
        public int Timeout { get; private set; }
        public int RoundNumber { get; private set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public bool Answered { get; set; }

        public Round(int round, string word, int timeout)
        {   
            this.Word = word;
            this.RoundNumber = round;
            this.Timeout = timeout;
            this.StartedAt = this.EndedAt = DateTime.MinValue;
        }
    }

    static class GameManager
    {
        public static ObservableCollection<PlayerStats> Stats { get; private set; }
        private static Queue<Round> rounds = new Queue<Round>();
        private static readonly object syncObj = new Object();
        public static PlayerStats MyStats { get; private set; }

        static GameManager()
        {
            Stats = new ObservableCollection<PlayerStats>();
            MyStats = new PlayerStats(String.Empty, 0);
        }

        public static void StartNewGame(List<string> players)
        {
            Stats.Clear();
            rounds.Clear();
            MyStats.Score = 0;
            foreach (string player in players)
            {
                Stats.Add(new PlayerStats(player, 0));
            }
        }

        public static void UpdateScores(List<int> scores)
        {
            for (int i = 0; i < scores.Count; i++)
            {
                Stats[i].Score = scores[i];
            }
        }

        public static void UpdateTypingState(List<bool> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                Stats[i].Typing = states[i];
            }
        }

        public static void PushRound(int round, string word, int timeout)
        {
            lock (syncObj)
            {
                rounds.Enqueue(new Round(round, word, timeout));
            }
        }

        public static Round GetNextRound()
        {
            lock (syncObj)
            {
                if (rounds.Count > 0)
                {
                    return rounds.Dequeue();
                }

                return null;
            }
        }

        public static void Answer(int currentRound, string word)
        {
            NetworkManager.Send("answer," + currentRound + "," + word + "\n");
        }
    }
}
