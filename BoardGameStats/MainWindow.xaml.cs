using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.ComponentModel;

using Google.GData.Spreadsheets;
using MahApps.Metro.Controls;

namespace BoardGameStats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public List<Player> Players { get; set; }
        public WorksheetFeed Games { get; set; }
        public List<GameEvent> GamesPlayed { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayLoginScreen();
        }

        private void DisplayLoginScreen()
        {
            LoginWindow loginWindow = new LoginWindow();

            loginWindow.Owner = this;
            loginWindow.ShowDialog();
            if (loginWindow.DialogResult.HasValue && loginWindow.DialogResult.Value)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.DoWork += (o, ea) =>
                    {
                        GetSpreadsheet(loginWindow.service);
                        Dispatcher.Invoke((Action)(() => PlayerDataGrid.ItemsSource = Players));
                    };
                worker.RunWorkerCompleted += (o, ea) =>
                    {
                        LoadingIndicator.IsBusy = false;

                        foreach (WorksheetEntry worksheet in Games.Entries)
                        {
                            Button gameButton = new Button();
                            string worksheetName = worksheet.Title.Text;
                            gameButton.Content = worksheetName;
                            worksheetName = worksheetName.Replace(" ", string.Empty)
                                                         .Replace("!", string.Empty)
                                                         .Replace("'", string.Empty);
                            gameButton.Name = worksheetName + "Button";
                            gameButton.Width = 250;
                            gameButton.Click += gameButton_Click;
                            GamePanel.Children.Add(gameButton);
                        }
                    };

                LoadingIndicator.IsBusy = true;
                worker.RunWorkerAsync();
            }
            else
            {
                Environment.Exit(1);
            }
        }

        private void GetSpreadsheet(SpreadsheetsService service)
        {
            SpreadsheetQuery query = new SpreadsheetQuery();
            SpreadsheetFeed feed = service.Query(query);
            SpreadsheetEntry spreadsheet = null;

            foreach (SpreadsheetEntry entry in feed.Entries)
            {
                if (!entry.Title.Text.Equals("Boardgame Stats"))
                {
                    continue;
                }

                spreadsheet = entry;
            }

            if (spreadsheet == null)
            {
                MessageBox.Show("You don't appear to have access to the proper spreadsheet.");
                return;
            }

            WorksheetFeed wsFeed = spreadsheet.Worksheets;
            Games = wsFeed;

            GetGameDateName(service, spreadsheet, wsFeed);
        }

        private void GetPlayers(SpreadsheetsService service, SpreadsheetEntry entry, WorksheetFeed wsFeed)
        {
            int cellID = 0;
            Players = new List<Player>();

            List<CellFeed> cellFeeds = DoCellQuery(service, wsFeed, 2, 2, 2);

            for (int i = 0; i < cellFeeds.Count; i++)
            {
                CellFeed currentCellFeed = cellFeeds[i];

                foreach (CellEntry cell in currentCellFeed.Entries)
                {
                    string playerList = cell.InputValue;
                    string[] playerNames = playerList.Split(',').Select(sValue => sValue.Trim()).ToArray();

                    foreach (string playerName in playerNames)
                    {
                        if (!Players.Exists(player => player.Name == playerName))
                        {
                            Players.Add(new Player() { Name = playerName });
                        }

                        Player myPlayer = new Player();
                        myPlayer = Players.Find(player => player.Name == playerName);
                        myPlayer.GamesPlayed += 1;

                        foreach (GameEvent gameEvent in GamesPlayed)
                        {
                            if (gameEvent.Name == wsFeed.Entries[i].Title.Text && gameEvent.ID == cellID)
                            {
                                if (gameEvent.Participants == null)
                                {
                                    gameEvent.Participants = new List<Player>();
                                }

                                gameEvent.Participants.Add(myPlayer);
                            }
                        }
                    }

                    cellID++;
                }
            }

            GetWins(service, entry, wsFeed, Players);
        }

        private void GetWins(SpreadsheetsService service, SpreadsheetEntry entry, WorksheetFeed wsFeed, List<Player> Players)
        {
            int cellID = 0;
            List<CellFeed> cellFeeds = DoCellQuery(service, wsFeed, 2, 3, 3);

            for (int i = 0; i < cellFeeds.Count; i++)
            {
                CellFeed currentCellFeed = cellFeeds[i];

                foreach (CellEntry cell in currentCellFeed.Entries)
                {
                    string playerList = cell.InputValue;
                    string[] playerNames = playerList.Split(',').Select(sValue => sValue.Trim()).ToArray();

                    foreach (string playerName in playerNames)
                    {
                        string placement = null;
                        string playerNameTrimmed = null;

                        if (playerName.Contains("("))
                        {
                            placement = Regex.Match(playerName, @"\(([^)]*)\)").Groups[1].Value;
                            playerNameTrimmed = playerName.Remove(playerName.Length - 4);
                        }
                        else
                        {
                            playerNameTrimmed = playerName;
                        }

                        if (Players.Exists(player => player.Name == playerNameTrimmed))
                        {
                            Player myPlayer = new Player();
                            myPlayer = Players.Find(player => player.Name == playerNameTrimmed);
                            myPlayer.Wins += 1;

                            if (placement != null)
                            {
                                if (myPlayer.Placement == null)
                                {
                                    myPlayer.Placement = new List<int>();
                                }

                                myPlayer.Placement.Add(Int32.Parse(placement));
                            }

                            foreach (GameEvent gameEvent in GamesPlayed)
                            {
                                if (gameEvent.Name == wsFeed.Entries[i].Title.Text && gameEvent.ID == cellID)
                                {
                                    if (gameEvent.Winners == null)
                                    {
                                        gameEvent.Winners = new List<Player>();
                                    }

                                    gameEvent.Winners.Add(myPlayer);
                                }
                            }
                        }
                    }

                    cellID++;
                }
            }

            foreach (Player player in Players)
            {
                player.WinPercentage = player.GetWinPercentage();
            }

            GetLosses(service, entry, wsFeed, Players);
        }
        
        private void GetLosses(SpreadsheetsService service, SpreadsheetEntry entry, WorksheetFeed wsFeed, List<Player> Players)
        {
            int cellID = 0;
            List<CellFeed> cellFeeds = DoCellQuery(service, wsFeed, 2, 4, 4);

            for (int i = 0; i < cellFeeds.Count; i++)
            {
                CellFeed currentCellFeed = cellFeeds[i];

                foreach (CellEntry cell in currentCellFeed.Entries)
                {
                    string playerList = cell.InputValue;
                    string[] playerNames = playerList.Split(',').Select(sValue => sValue.Trim()).ToArray();

                    foreach (string playerName in playerNames)
                    {
                        string placement = null;
                        string playerNameTrimmed = null;

                        if (playerName.Contains("("))
                        {
                            placement = Regex.Match(playerName, @"\(([^)]*)\)").Groups[1].Value;
                            playerNameTrimmed = playerName.Remove(playerName.Length - 4);
                        }
                        else
                        {
                            playerNameTrimmed = playerName;
                        }

                        if (Players.Exists(player => player.Name == playerNameTrimmed))
                        {
                            Player myPlayer = new Player();
                            myPlayer = Players.Find(player => player.Name == playerNameTrimmed);
                            myPlayer.Losses += 1;

                            if (placement != null)
                            {
                                if (myPlayer.Placement == null)
                                {
                                    myPlayer.Placement = new List<int>();
                                }

                                myPlayer.Placement.Add(Int32.Parse(placement));
                            }

                            foreach (GameEvent gameEvent in GamesPlayed)
                            {
                                if (gameEvent.Name == wsFeed.Entries[i].Title.Text && gameEvent.ID == cellID)
                                {
                                    if (gameEvent.Losers == null)
                                    {
                                        gameEvent.Losers = new List<Player>();
                                    }

                                    gameEvent.Losers.Add(myPlayer);
                                }
                            }
                        }
                    }

                    cellID++;
                }
            }

            foreach (Player player in Players)
            {
                player.AveragePlacement = player.GetAveragePlacement();
            }
        }

        private void GetGameDateName(SpreadsheetsService service, SpreadsheetEntry entry, WorksheetFeed wsFeed)
        {
            GamesPlayed = new List<GameEvent>();
            List<CellFeed> cellFeeds = DoCellQuery(service, wsFeed, 2, 1, 1);

            int gameID = 0;

            for (int i = 0; i < cellFeeds.Count; i++)
            {
                CellFeed currentCellFeed = cellFeeds[i];

                foreach (CellEntry cell in currentCellFeed.Entries)
                {
                    GameEvent newGameEvent = new GameEvent();
                    string dateTimeString = cell.InputValue;
                    DateTime dateTime = DateTime.Parse(dateTimeString);
                    newGameEvent.Date = dateTime;
                    newGameEvent.Name = wsFeed.Entries[i].Title.Text;
                    newGameEvent.ID = gameID;
                    GamesPlayed.Add(newGameEvent);
                    gameID++;
                }
            }

            GetPlayers(service, entry, wsFeed);
        }

        private List<CellFeed> DoCellQuery(SpreadsheetsService service, WorksheetFeed wsFeed, uint minRow, uint minColumn, uint maxColumn)
        {
            List<CellFeed> cellFeeds = new List<CellFeed>();
            int i = 0;

            foreach (WorksheetEntry worksheet in wsFeed.Entries)
            {
                CellQuery cellQuery = new CellQuery(worksheet.CellFeedLink);
                cellQuery.MinimumRow = minRow;
                cellQuery.MinimumColumn = minColumn;
                cellQuery.MaximumColumn = maxColumn;
                CellFeed cellFeed = service.Query(cellQuery);

                cellFeeds.Insert(i, cellFeed);
                i++;
            }

            return cellFeeds;
        }

        private void gameButton_Click(object sender, RoutedEventArgs e)
        {
            string windowName = ((sender as Button).Content).ToString();
            GameWindow gameWindow = new GameWindow();
            
            gameWindow.Title = windowName;
            string windowNameTrimmed = windowName.Replace(" ", string.Empty)
                                                 .Replace("!", string.Empty)
                                                 .Replace("'", string.Empty);
            gameWindow.Name = windowNameTrimmed;

            List<GameEvent> relevantGames = new List<GameEvent>();
            List<Player> winnerList = new List<Player>();
            List<Player> loserList = new List<Player>();
            List<Player> masterList = new List<Player>();
            List<Player> failureList = new List<Player>();

            foreach (GameEvent gameEvent in GamesPlayed)
            {
                if (gameEvent.Name == gameWindow.Title)
                {
                    relevantGames.Add(gameEvent);
                }
            }

            foreach (GameEvent gameEvent in relevantGames)
            {
                if (gameEvent.Winners != null)
                {
                    foreach (Player player in gameEvent.Winners)
                    {
                        winnerList.Add(player);
                    }
                }
                
                if (gameEvent.Losers != null)
                {
                    foreach (Player player in gameEvent.Losers)
                    {
                        loserList.Add(player);
                    }
                }
            }

            ILookup<Player, Player> grouped = winnerList.ToLookup(x => x);
            int maxRepetitions;

            if (grouped.Count != 0)
            {
                maxRepetitions = grouped.Max(x => x.Count());
                masterList = grouped.Where(x => x.Count() == maxRepetitions).Select(x => x.Key).ToList();
            }

            grouped = loserList.ToLookup(x => x);
            if (grouped.Count != 0)
            {
                maxRepetitions = grouped.Max(x => x.Count());
                failureList = grouped.Where(x => x.Count() == maxRepetitions).Select(x => x.Key).ToList();
            }

            gameWindow.GameDataGrid.ItemsSource = relevantGames;
            gameWindow.MasterListBox.ItemsSource = masterList;
            gameWindow.FailureListBox.ItemsSource = failureList;

            gameWindow.Show();
        }
    }

    public class GameEvent
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public List<Player> Participants { get; set; }
        public List<Player> Winners { get; set; }
        public List<Player> Losers { get; set; }       
    }

    public class Player
    {
        public string Name { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int GamesPlayed { get; set; }
        public double WinPercentage { get; set; }
        public List<int> Placement { get; set; }
        public string AveragePlacement { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }

        public double GetWinPercentage()
        {
            return Math.Round((double)Wins / (double)GamesPlayed, 4);
        }

        public string GetAveragePlacement()
        {
            string averagePlacement;
            int total = 0;
            int numPlacements = 0;
            
            if (Placement != null)
            {
                foreach (int placement in Placement)
                {
                    total += placement;
                    numPlacements++;
                }
            }

            averagePlacement = Math.Round((double)total / (double)numPlacements, 2).ToString();

            if (averagePlacement == "NaN")
            {
                averagePlacement = "-";
            }

            return averagePlacement;
        }
    }
}
