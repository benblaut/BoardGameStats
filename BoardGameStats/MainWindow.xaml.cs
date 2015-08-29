using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
                    };

                LoadingIndicator.IsBusy = true;
                worker.RunWorkerAsync();
            }
            else
            {
                System.Environment.Exit(1);
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
                System.Windows.MessageBox.Show("You don't appear to have access to the proper spreadsheet.");
                return;
            }

            WorksheetFeed wsFeed = spreadsheet.Worksheets;

            GetPlayers(service, spreadsheet, wsFeed);
        }

        private void GetPlayers(SpreadsheetsService service, SpreadsheetEntry spreadsheet, WorksheetFeed wsFeed)
        {
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
                    }
                }
            }

            GetWins(service, spreadsheet, wsFeed, Players);
        }

        private void GetWins(SpreadsheetsService service, SpreadsheetEntry spreadsheet, WorksheetFeed wsFeed, List<Player> Players)
        {
            List<CellFeed> cellFeeds = DoCellQuery(service, wsFeed, 2, 3, 3);

            for (int i =0; i<cellFeeds.Count; i++)
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
                        }
                    }
                }
            }

            foreach (Player player in Players)
            {
                player.WinPercentage = player.GetWinPercentage();
            }

            GetLosses(service, spreadsheet, wsFeed, Players);
        }
        
        private void GetLosses(SpreadsheetsService service, SpreadsheetEntry spreadsheet, WorksheetFeed wsFeed, List<Player> Players)
        {
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
                        }
                    }
                }
            }

            foreach (Player player in Players)
            {
                player.AveragePlacement = player.GetAveragePlacement();
            }
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
                i += 1;
            }

            return cellFeeds;
        }
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
