using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MahApps.Metro.Controls;

namespace BoardGameStats
{
    /// <summary>
    /// Interaction logic for PlayerWindow.xaml
    /// </summary>
    public partial class PlayerWindow : MetroWindow
    {
        public Dictionary<string, int> BestGameDict { get; set; }
        public Dictionary<string, int> WorstGameDict { get; set; }
        public Dictionary<string, int> PlayedDictionary { get; set; }
        public Dictionary<string, double> WinPercentageDict { get; set; }
        public Dictionary<string, double> LossPercentageDict { get; set; }

        private bool TimesPlayedFilter(object item)
        {
            int textAsNum;
            Dictionary<string, int> filteredDict = new Dictionary<string, int>();
            if (Int32.TryParse(LimitComboBox.Text, out textAsNum))
            {
                foreach (KeyValuePair<string, int> entry in PlayedDictionary)
                {
                    if (entry.Value >= textAsNum)
                    {
                        filteredDict.Add(entry.Key, entry.Value);
                    }
                }

                if (filteredDict != null)
                {
                    foreach (KeyValuePair<string, int> entry in filteredDict)
                    {
                        if (BestGameComboBox.Text == "wins.")
                        {
                            return filteredDict.Keys.Contains(((KeyValuePair<string, int>)item).Key);     
                        }
                        else if (BestGameComboBox.Text == "win percentage.")
                        {
                            return filteredDict.Keys.Contains(((KeyValuePair<string, string>)item).Key);
                        }
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        public PlayerWindow(Player selectedPlayer, List<GameEvent> GamesPlayed)
        {
            InitializeComponent();

            string packUri = "pack://application:,,,/BoardGameStats;component/Resources/Images/";
            if (selectedPlayer.Wins >= 5 && selectedPlayer.Wins < 10)
            {
                packUri += "knight.png";
            }
            else if (selectedPlayer.Wins >= 10 && selectedPlayer.Wins < 15)
            {
                packUri += "bishop.png";
            }
            else if (selectedPlayer.Wins >= 15 && selectedPlayer.Wins < 25)
            {
                packUri += "rook.png";
            }
            else if (selectedPlayer.Wins >= 25 && selectedPlayer.Wins < 50)
            {
                packUri += "queen.png";
            }
            else if (selectedPlayer.Wins > 50)
            {
                packUri += "king.png";
            }
            else
            {
                packUri += "pawn.png";
            }

            PlayerImage.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
            PlayerName.Text = selectedPlayer.Name;
            PlayerWins.Text = selectedPlayer.Wins.ToString();
            PlayerLosses.Text = selectedPlayer.Losses.ToString();
            PlayerGamesPlayed.Text = selectedPlayer.GamesPlayed.ToString();
            PlayerAveragePlacement.Text = selectedPlayer.AveragePlacement.ToString();
            PlayerWinPercentage.Text = selectedPlayer.WinPercentage.ToString("P");

            BestGameComboBox.SelectedIndex = 0;
            LimitComboBox.SelectedIndex = 2;
            Dictionary<string, int> winDictionary = new Dictionary<string, int>();
            Dictionary<string, int> lossDictionary = new Dictionary<string, int>();
            PlayedDictionary = new Dictionary<string, int>();

            foreach (GameEvent gameEvent in GamesPlayed)
            {
                if (gameEvent.Participants.Exists(player => player.Name == selectedPlayer.Name))
                {
                    if (!PlayedDictionary.ContainsKey(gameEvent.Name))
                    {
                        PlayedDictionary.Add(gameEvent.Name, 1);
                    }
                    else
                    {
                        PlayedDictionary[gameEvent.Name]++;
                    }
                }

                if (gameEvent.Winners != null)
                {
                    if (gameEvent.Winners.Exists(player => player.Name == selectedPlayer.Name))
                    {
                        if (!winDictionary.ContainsKey(gameEvent.Name))
                        {
                            winDictionary.Add(gameEvent.Name, 1);
                        }
                        else
                        {
                            winDictionary[gameEvent.Name]++;
                        }
                    }
                }

                if (gameEvent.Losers != null)
                {
                    if (gameEvent.Losers.Exists(player => player.Name == selectedPlayer.Name))
                    {
                        if (!lossDictionary.ContainsKey(gameEvent.Name))
                        {
                            lossDictionary.Add(gameEvent.Name, 1);
                        }
                        else
                        {
                            lossDictionary[gameEvent.Name]++;
                        }
                    }
                }

                WinPercentageDict = new Dictionary<string, double>();
                LossPercentageDict = new Dictionary<string, double>();

                foreach (KeyValuePair<string, int> gamePlayed in PlayedDictionary)
                {
                    int gameWins = 0;
                    int gameLosses = 0;
                    int gamePlayedNum = PlayedDictionary[gamePlayed.Key];
                    double gameWinPercentage = 0.0;
                    double gameLossPercentage = 0.0;

                    if (winDictionary.ContainsKey(gamePlayed.Key))
                    {
                        gameWins = winDictionary[gamePlayed.Key];
                    }
                    else
                    {
                        if (lossDictionary.ContainsKey(gamePlayed.Key))
                        {
                            gameLosses = lossDictionary[gamePlayed.Key];
                        }
                        
                    }

                    gameWinPercentage = Math.Round((double)gameWins / (double)gamePlayedNum, 4);
                    WinPercentageDict.Add(gamePlayed.Key, gameWinPercentage);
                    gameLossPercentage = Math.Round((double)gameLosses / (double)gamePlayedNum, 4);
                    LossPercentageDict.Add(gamePlayed.Key, gameLossPercentage);
                }
            }

            CalculateGames(winDictionary, lossDictionary);

            CollectionView bestView = (CollectionView)CollectionViewSource.GetDefaultView(BestGame.ItemsSource);
            CollectionView worstView = (CollectionView)CollectionViewSource.GetDefaultView(WorstGame.ItemsSource);
            bestView.Filter = TimesPlayedFilter;
            worstView.Filter = TimesPlayedFilter;
        }

        private void BestGameSort_Changed(object sender, EventArgs e)
        {
            if (BestGameComboBox.Text == "wins.")
            {
                if (BestGameDict.Count() != 0)
                {
                    BestGame.ItemsSource = BestGameDict;
                }
                else
                {
                    BestGame.ItemsSource = null;
                }

                if (WorstGameDict.Count() != 0)
                {
                    WorstGame.ItemsSource = WorstGameDict;
                }
                else
                {
                    WorstGame.ItemsSource = null;
                }
            }
            else if (BestGameComboBox.Text == "win percentage.")
            {
                Dictionary<string, string> WinDict = new Dictionary<string, string>();
                Dictionary<string, string> LossDict = new Dictionary<string, string>();

                foreach (KeyValuePair<string, double> entry in WinPercentageDict)
                {
                    string key = entry.Key;
                    string value = entry.Value.ToString("P");
                    
                    WinDict.Add(key, value);
                }

                foreach (KeyValuePair<string, double> entry in LossPercentageDict)
                {
                    string key = entry.Key;
                    double valueNum = entry.Value;
                    valueNum = 1 - valueNum;
                    string value = valueNum.ToString("P");
                    LossDict.Add(key, value);
                }

                BestGame.ItemsSource = WinDict;
                WorstGame.ItemsSource = LossDict;
            }

            RefreshView();
        }

        private void LimitComboBox_Changed(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void CalculateGames(Dictionary<string, int> winDictionary, Dictionary<string,int> lossDictionary)
        {
            int max = 0;
            BestGameDict = new Dictionary<string, int>();
            WorstGameDict = new Dictionary<string, int>();

            if (winDictionary.Count != 0)
            {
                max = winDictionary.Values.Max();
                BestGameDict = winDictionary;
                foreach (KeyValuePair<string, int> entry in winDictionary.Where(pair => (pair.Value < max)).ToList())
                {
                    BestGameDict.Remove(entry.Key);
                }
            }

            if (lossDictionary.Count != 0)
            {
                max = lossDictionary.Values.Max();
                WorstGameDict = lossDictionary;
                foreach (KeyValuePair<string, int> entry in lossDictionary.Where(pair => (pair.Value < max)).ToList())
                {
                    WorstGameDict.Remove(entry.Key);
                }
            }

            double maxPercentage = 0.0;
            maxPercentage = WinPercentageDict.Values.Max();
            foreach (KeyValuePair<string, double> entry in WinPercentageDict.Where(pair => (pair.Value < maxPercentage)).ToList())
            {
                WinPercentageDict.Remove(entry.Key);
            }

            maxPercentage = LossPercentageDict.Values.Max();
            foreach (KeyValuePair<string, double> entry in LossPercentageDict.Where(pair => (pair.Value < maxPercentage)).ToList())
            {
                LossPercentageDict.Remove(entry.Key);
            }

            if (BestGameDict.Count() != 0)
            {
                BestGame.ItemsSource = BestGameDict;
            }
            else
            {
                BestGame.ItemsSource = new List<string> { "-" };
            }

            if (WorstGameDict.Count() != 0)
            {
                WorstGame.ItemsSource = WorstGameDict;
            }
            else
            {
                WorstGame.ItemsSource = new List<string> { "-" };
            }
        }

        private void RefreshView()
        {
            CollectionView bestView = (CollectionView)CollectionViewSource.GetDefaultView(BestGame.ItemsSource);
            CollectionView worstView = (CollectionView)CollectionViewSource.GetDefaultView(WorstGame.ItemsSource);

            if (bestView != null)
            {
                bestView.Filter = null;
                bestView.Filter = TimesPlayedFilter;
            }
            
            if (worstView != null)
            {
                worstView.Filter = null;
                worstView.Filter = TimesPlayedFilter;
            }
            
            if (BestGame.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(BestGame.ItemsSource).Refresh();
            }

            if (WorstGame.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(WorstGame.ItemsSource).Refresh();
            }
        }
    }
}
