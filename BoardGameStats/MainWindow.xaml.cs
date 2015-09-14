using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
        public List<string> InsultList { get; set; }

        private bool WinsFilter(object item)
        {
            if (string.IsNullOrEmpty(FilterTextBox.Text))
            {
                return true;
            }
            else
            {
                int textAsNum;
                if (Int32.TryParse(FilterTextBox.Text, out textAsNum))
                {
                    return ((item as Player).Wins.CompareTo(textAsNum) != -1);
                }
                else
                {
                    return true;
                }
            }
        }

        private bool LossesFilter(object item)
        {
            if (string.IsNullOrEmpty(FilterTextBox.Text))
            {
                return true;
            }
            else
            {
                int textAsNum;
                if (Int32.TryParse(FilterTextBox.Text, out textAsNum))
                {
                    return ((item as Player).Losses.CompareTo(textAsNum) != -1);
                }
                else
                {
                    return true;
                }
            }
        }

        private bool GamesPlayedFilter(object item)
        {
            if (string.IsNullOrEmpty(FilterTextBox.Text))
            {
                return true;
            }
            else
            {
                int textAsNum;
                if (Int32.TryParse(FilterTextBox.Text, out textAsNum))
                {
                    return ((item as Player).GamesPlayed.CompareTo(textAsNum) != -1);
                }
                else
                {
                    return true;
                }
            }
        }

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

                        FilterComboBox.SelectedIndex = 0;
                        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(PlayerDataGrid.ItemsSource);
                        view.Filter = WinsFilter;

                        GetGamesPlayedList();
                    };

                InitializeInsultList();
                Random random = new Random();
                int randomNum = random.Next(InsultList.Count);
                LoadingIndicator.IsBusy = true;
                LoadingIndicator.BusyContent = InsultList[randomNum];
                worker.RunWorkerAsync();
            }
            else
            {
                Environment.Exit(1);
            }
        }

        private void InitializeInsultList()
        {
            InsultList = new List<string>();

            InsultList.Add("Fetching statistics, calm your tits...");
            InsultList.Add("Fetching statistics, cool your jets...");
            InsultList.Add("Fetching statistics, if you can't stand the heat, get out of the street...");
            InsultList.Add("Fetching statistics, u fuckin wot m8?...");
            InsultList.Add("Fetching statistics, go masturbate or something...");
            InsultList.Add("Fetching statistics, bitch...");
            InsultList.Add("Fetching statistics, smoke weed every day...");
            InsultList.Add("Fetching statistics, 360 no scope faggot...");
            InsultList.Add("Fetching statistics, please w...RANDY ORTON OUTTA NOWHERE!!! RKO! RKO!");
            InsultList.Add("Fetching statistics, <insert flavor text>...");
            InsultList.Add("Fetching statistics, John Cena was here...");
            InsultList.Add("Fetching statistics, bird up...");
        }

        private void FilterTextBox_Changed(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(PlayerDataGrid.ItemsSource).Refresh();
        }

        private void ComboBox_Changed(object sender, EventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(PlayerDataGrid.ItemsSource);

            if (FilterComboBox.Text == "wins.")
            {
                view.Filter = null;
                view.Filter = WinsFilter;
            }
            else if (FilterComboBox.Text == "losses.")
            {
                view.Filter = null;
                view.Filter = LossesFilter;
            }
            else if (FilterComboBox.Text == "games played.")
            {
                view.Filter = null;
                view.Filter = GamesPlayedFilter;
            }

            if (PlayerDataGrid.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(PlayerDataGrid.ItemsSource).Refresh();
            }
        }

        private void GetGamesPlayedList()
        {
            Dictionary<string, int> gamesPlayedList = new Dictionary<string, int>();

            foreach (WorksheetEntry worksheet in Games.Entries)
            {
                gamesPlayedList.Add(worksheet.Title.Text, 0);
            }

            foreach(GameEvent game in GamesPlayed)
            {
                gamesPlayedList[game.Name]++;
            }

            gamesPlayedList = gamesPlayedList.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            MostPlayedDataGrid.ItemsSource = gamesPlayedList;
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

        private void PlayerCellClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;

            if (row == null)
            {
                return;
            }

            Player selectedPlayer = (Player)PlayerDataGrid.CurrentCell.Item;
            string windowName = selectedPlayer.Name;
            PlayerWindow playerWindow = new PlayerWindow();

            string windowNameTrimmed = windowName.Replace(" ", string.Empty);

            playerWindow.Title = windowName;
            playerWindow.Name = windowNameTrimmed;

            string packUri = "pack://application:,,,/BoardGameStats;component/Resources/";
            if (selectedPlayer.Wins >= 5 && selectedPlayer.Wins < 10)
            {
                packUri += "rook.png";
            }
            else if (selectedPlayer.Wins >= 10 && selectedPlayer.Wins < 15)
            {
                packUri += "knight.png";
            }
            else if (selectedPlayer.Wins >= 15 && selectedPlayer.Wins < 25)
            {
                packUri += "bishop.png";
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
            
            playerWindow.PlayerImage.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
            playerWindow.PlayerName.Text = selectedPlayer.Name;
            playerWindow.PlayerWins.Text = selectedPlayer.Wins.ToString();
            playerWindow.PlayerLosses.Text = selectedPlayer.Losses.ToString();
            playerWindow.PlayerGamesPlayed.Text = selectedPlayer.GamesPlayed.ToString();
            playerWindow.PlayerAveragePlacement.Text = selectedPlayer.AveragePlacement.ToString();
            playerWindow.PlayerWinPercentage.Text = selectedPlayer.WinPercentage.ToString("P");
            playerWindow.Show();
        }

        private void GameCellClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;

            if (row == null)
            {
                return;
            }

            KeyValuePair<string, int> selectedItem = (KeyValuePair<string, int>)MostPlayedDataGrid.CurrentCell.Item;
            string windowName = selectedItem.Key;
            GameWindow gameWindow = new GameWindow();

            gameWindow.Title = windowName;
            string windowNameTrimmed = windowName.Replace(" ", string.Empty)
                                                 .Replace("!", string.Empty)
                                                 .Replace("'", string.Empty)
                                                 .Replace(":", string.Empty)
                                                 .Replace("7", "Seven");
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

namespace Behaviors
{
    public static class ComboBoxWidthFromItemsBehavior
    {
        public static readonly DependencyProperty ComboBoxWidthFromItemsProperty =
            DependencyProperty.RegisterAttached
            (
                "ComboBoxWidthFromItems",
                typeof(bool),
                typeof(ComboBoxWidthFromItemsBehavior),
                new UIPropertyMetadata(false, OnComboBoxWidthFromItemsPropertyChanged)
            );
        public static bool GetComboBoxWidthFromItems(DependencyObject obj)
        {
            return (bool)obj.GetValue(ComboBoxWidthFromItemsProperty);
        }
        public static void SetComboBoxWidthFromItems(DependencyObject obj, bool value)
        {
            obj.SetValue(ComboBoxWidthFromItemsProperty, value);
        }
        private static void OnComboBoxWidthFromItemsPropertyChanged(DependencyObject dpo,
                                                                    DependencyPropertyChangedEventArgs e)
        {
            ComboBox comboBox = dpo as ComboBox;
            if (comboBox != null)
            {
                if ((bool)e.NewValue == true)
                {
                    comboBox.Loaded += OnComboBoxLoaded;
                }
                else
                {
                    comboBox.Loaded -= OnComboBoxLoaded;
                }
            }
        }
        private static void OnComboBoxLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Action action = () => { comboBox.SetWidthFromItems(); };
            comboBox.Dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
        }
    }

    public static class ComboBoxExtensionMethods
    {
        public static void SetWidthFromItems(this ComboBox comboBox)
        {
            double comboBoxWidth = 19;// comboBox.DesiredSize.Width;

            // Create the peer and provider to expand the comboBox in code behind. 
            ComboBoxAutomationPeer peer = new ComboBoxAutomationPeer(comboBox);
            IExpandCollapseProvider provider = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse);
            EventHandler eventHandler = null;
            eventHandler = new EventHandler(delegate
            {
                if (comboBox.IsDropDownOpen &&
                    comboBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    double width = 0;
                    foreach (var item in comboBox.Items)
                    {
                        ComboBoxItem comboBoxItem = comboBox.ItemContainerGenerator.ContainerFromItem(item) as ComboBoxItem;
                        comboBoxItem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        if (comboBoxItem.DesiredSize.Width > width)
                        {
                            width = comboBoxItem.DesiredSize.Width;
                        }
                    }
                    comboBox.Width = comboBoxWidth + width + 10;
                    // Remove the event handler. 
                    comboBox.ItemContainerGenerator.StatusChanged -= eventHandler;
                    comboBox.DropDownOpened -= eventHandler;
                    provider.Collapse();
                }
            });
            comboBox.ItemContainerGenerator.StatusChanged += eventHandler;
            comboBox.DropDownOpened += eventHandler;
            // Expand the comboBox to generate all its ComboBoxItem's. 
            provider.Expand();
        }
    }
}
