using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoneyballGame
{
    public partial class Form1 : Form
    {
        private GameDatabase db;
        private int matchWeek = 1;
        private int breakWeek = 0;
        private int currentSeason = 1; // Track season
        private bool isSeasonOver = false;

        private static readonly HttpClient client = new HttpClient();

        // UI Components
        private Panel sidebar;
        private Panel header;
        private Panel mainContent;
        private Panel cardStats, cardLeague, cardBoard, cardTransfers;
        private Label lblWeekInfo, lblTopScorer, lblTopAssister, lblBestPasser, lblTopTackler;
        private DataGridView dgvStandings;
        private ListBox lstMatchLogs, lstTransferLogs;
        private Button btnNextWeek;
        private Label lblChampion;


        public Form1()
        {
            this.Text = "Football Manager Sim: Moneyball";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = FMColors.PrimaryBg;

            db = new GameDatabase();
            db.InitializeData();
            client.Timeout = TimeSpan.FromSeconds(1.0); // Critical: Avoid slow simulation due to network timeouts

            InitializeFMUI();
            UpdateLeagueTable();
            UpdateDashboardStats();
        }

        private void InitializeFMUI()
        {
            this.Controls.Clear();

            // --- ROOT GRID (ABSOLUTE ISOLATION) ---
            TableLayoutPanel rootLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = FMColors.PrimaryBg };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // Fixed Sidebar Width
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Dynamic Content Width
            this.Controls.Add(rootLayout);

            // 1. SIDEBAR (Cell 0,0)
            sidebar = new Panel { Dock = DockStyle.Fill, BackColor = FMColors.SidebarBg };
            rootLayout.Controls.Add(sidebar, 0, 0);
            
            Label lblLogo = new Label { Text = "FM", Dock = DockStyle.Top, Height = 100, ForeColor = FMColors.Accent, Font = new Font("Segoe UI", 36, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            sidebar.Controls.Add(lblLogo);

            string[] navItems = { "Anasayfa", "Kadro", "Taktik", "Transfer", "Finans", "Fikstür" };
            int top = 100;
            foreach (var item in navItems)
            {
                sidebar.Controls.Add(CreateSidebarButton(item, top));
                top += 45;
            }
            // Add a visual separator line
            sidebar.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.FromArgb(50, 50, 70) });

            // 2. CONTENT AREA (Cell 1,0)
            Panel rightContainer = new Panel { Dock = DockStyle.Fill, BackColor = FMColors.PrimaryBg };
            rootLayout.Controls.Add(rightContainer, 1, 0);

            // 3. Header
            header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = FMColors.SecondaryBg };
            rightContainer.Controls.Add(header);

            lblWeekInfo = new Label { Text = "HAFTA: 1", Left = 20, Top = 15, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true };
            header.Controls.Add(lblWeekInfo);

            btnNextWeek = new Button { Text = "Sonraki Haftayı Oyna ⚽", Left = 760, Top = 10, Width = 200, Height = 40, BackColor = Color.FromArgb(0, 160, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnNextWeek.FlatAppearance.BorderSize = 0;
            btnNextWeek.Click += async (s, e) => await PlayNextWeek();
            btnNextWeek.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            header.Controls.Add(btnNextWeek);

            // 4. Dashboard Grid
            mainContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            rightContainer.Controls.Add(mainContent);

            TableLayoutPanel dashboardLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            mainContent.Controls.Add(dashboardLayout);

            // --- CARDS ---
            cardStats = CreateCard("LİG İSTATİSTİKLERİ", 0, 0);
            lblTopScorer = new Label { Text = "Gol Kralı: -", ForeColor = Color.White, Top = 40, Left = 15, AutoSize = true, Font = new Font("Segoe UI", 10) };
            lblTopAssister = new Label { Text = "Asist Kralı: -", ForeColor = Color.White, Top = 70, Left = 15, AutoSize = true, Font = new Font("Segoe UI", 10) };
            lblBestPasser = new Label { Text = "En İyi Pasör: -", ForeColor = Color.White, Top = 100, Left = 15, AutoSize = true, Font = new Font("Segoe UI", 10) };
            lblTopTackler = new Label { Text = "En Çok Top Kapan: -", ForeColor = Color.White, Top = 130, Left = 15, AutoSize = true, Font = new Font("Segoe UI", 10) };
            
            cardStats.Controls.Add(lblTopScorer);
            cardStats.Controls.Add(lblTopAssister);
            cardStats.Controls.Add(lblBestPasser);
            cardStats.Controls.Add(lblTopTackler);

            cardLeague = CreateCard("PUAN DURUMU", 1, 0);
            dgvStandings = new DataGridView 
            { 
                Dock = DockStyle.Fill, BackgroundColor = FMColors.SecondaryBg, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, ForeColor = Color.White,
                GridColor = Color.FromArgb(50, 50, 70), ScrollBars = ScrollBars.Both, AutoGenerateColumns = true 
            };
            dgvStandings.DefaultCellStyle.BackColor = FMColors.SecondaryBg;
            dgvStandings.DefaultCellStyle.ForeColor = Color.White;
            dgvStandings.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 65);
            dgvStandings.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvStandings.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvStandings.ColumnHeadersHeight = 30;
            dgvStandings.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvStandings.EnableHeadersVisualStyles = false;
            cardLeague.Controls.Add(dgvStandings);

            cardBoard = CreateCard("HAFTALIK MAÇ SONUÇLARI", 0, 1);
            lstMatchLogs = new ListBox { Dock = DockStyle.Fill, BackColor = FMColors.SecondaryBg, ForeColor = Color.White, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9) };
            cardBoard.Controls.Add(lstMatchLogs);

            cardTransfers = CreateCard("GELEN/GİDEN TRANSFERLER", 1, 1);
            lstTransferLogs = new ListBox 
            { 
                Dock = DockStyle.Fill, 
                BackColor = FMColors.SecondaryBg, 
                ForeColor = Color.Gold, 
                BorderStyle = BorderStyle.None, 
                Font = new Font("Segoe UI", 9, FontStyle.Italic) 
            };
            cardTransfers.Controls.Add(lstTransferLogs);

            dashboardLayout.Controls.Add(cardStats, 0, 0);
            dashboardLayout.Controls.Add(cardLeague, 1, 0);
            dashboardLayout.Controls.Add(cardBoard, 0, 1);
            dashboardLayout.Controls.Add(cardTransfers, 1, 1);
        }

        private Button CreateSidebarButton(string text, int top)
        {
            Button btn = new Button { Text = "  " + text, Top = top, Width = 180, Height = 45, BackColor = FMColors.SidebarBg, ForeColor = Color.LightGray, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11) };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = FMColors.Accent;
            
            if (text == "Transfer") btn.Click += (s, e) => new TransferForm(db).Show();
            if (text == "Kadro") btn.Click += (s, e) => new LineupForm(db).Show();
            if (text == "Taktik") btn.Click += (s, e) => new TacticsForm(db).Show();
            if (text == "Finans") btn.Click += (s, e) => new FinanceForm(db).Show();
            if (text == "Anasayfa") btn.Click += (s, e) => UpdateDashboardStats();

            return btn;
        }

        private Panel CreateCard(string title, int col, int row)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = FMColors.SecondaryBg, Margin = new Padding(5) };
            Label lbl = new Label { Text = title, Dock = DockStyle.Top, Height = 30, ForeColor = FMColors.Accent, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            p.Controls.Add(lbl);
            return p;
        }

        private void UpdateLeagueTable()
        {
            var sortedTable = db.LeagueTable.OrderByDescending(t => t.Points).ThenByDescending(t => t.GoalDifference).ToList();
            var displayData = sortedTable.Select(t => new { 
                Takım = t.Name, 
                O = (t.Won + t.Drawn + t.Lost), 
                G = t.Won, 
                B = t.Drawn, 
                M = t.Lost, 
                Av = t.GoalDifference, 
                Puan = t.Points 
            }).ToList();

            dgvStandings.DataSource = null; // Reset to force column generation
            dgvStandings.DataSource = displayData;
            
            // Fix header appearance with null checks
            if (dgvStandings.Columns.Count > 0)
            {
                if (dgvStandings.Columns["Takım"] != null) 
                {
                    dgvStandings.Columns["Takım"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    dgvStandings.Columns["Takım"].Width = 140;
                    dgvStandings.Columns["Takım"].DisplayIndex = 0;
                }
                if (dgvStandings.Columns["O"] != null) dgvStandings.Columns["O"].Width = 35;
                if (dgvStandings.Columns["G"] != null) dgvStandings.Columns["G"].Width = 35;
                if (dgvStandings.Columns["B"] != null) dgvStandings.Columns["B"].Width = 35;
                if (dgvStandings.Columns["M"] != null) dgvStandings.Columns["M"].Width = 35;
                if (dgvStandings.Columns["Av"] != null) dgvStandings.Columns["Av"].Width = 45;
                
                if (dgvStandings.Columns["Puan"] != null)
                {
                    dgvStandings.Columns["Puan"].Width = 55;
                    dgvStandings.Columns["Puan"].DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                    dgvStandings.Columns["Puan"].DefaultCellStyle.ForeColor = Color.Gold;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void UpdateDashboardStats()
        {
            var allPlayers = db.AllPlayers.Values.ToList();
            if (allPlayers.Count == 0) return;

            var topScorer = allPlayers.OrderByDescending(p => p.Goals).FirstOrDefault();
            var topAssister = allPlayers.OrderByDescending(p => p.Assists).FirstOrDefault();
            var bestPasser = allPlayers.Where(p => p.Position == "OS").OrderByDescending(p => p.PassAccuracy).FirstOrDefault();
            var topTackler = allPlayers.Where(p => p.Position == "DF").OrderByDescending(p => p.SeasonTacklesWon).FirstOrDefault();

            if (topScorer != null) lblTopScorer.Text = $"⚽ Gol Kralı: {topScorer.Name} ({topScorer.Goals})";
            if (topAssister != null) lblTopAssister.Text = $"👟 Asist Kralı: {topAssister.Name} ({topAssister.Assists})";
            if (bestPasser != null) lblBestPasser.Text = $"🎯 En İyi Pasör (OS): {bestPasser.Name} (%{bestPasser.PassAccuracy:F1})";
            if (topTackler != null) lblTopTackler.Text = $"🛡️ En Çok Top Kapan (DF): {topTackler.Name} ({topTackler.SeasonTacklesWon})";
        }

        private async Task PlayNextWeek()
        {
            if (isSeasonOver) { StartNewSeason(); return; }

            btnNextWeek.Enabled = false;

            if (matchWeek == 18 && breakWeek < 4)
            {
                breakWeek++;
                lblWeekInfo.Text = $"DEVRE ARASI: {breakWeek}. HAFTA";
                lstMatchLogs.Items.Add($"\n=== TRANSFER HAFTASI ({breakWeek}/4) ===");
                for (int i = 0; i < 3; i++) await TryRandomTransfer();
                if (breakWeek == 4) lstMatchLogs.Items.Add("=== TRANSFER KAPANDI ===");
            }
            else
            {
                lblWeekInfo.Text = $"HAFTA: {matchWeek}";
                lstMatchLogs.Items.Add($"\n=== {matchWeek}. HAFTA MAÇLARI ===");
                await TryRandomTransfer();
                var shuffledTeams = db.LeagueTable.OrderBy(a => Guid.NewGuid()).ToList();
                for (int i = 0; i < 18; i += 2) PlayMatch(shuffledTeams[i], shuffledTeams[i + 1]);
                UpdateLeagueTable();
                UpdateDashboardStats(); // Refresh stats each week
                matchWeek++;
                if (matchWeek > 34) EndSeason();
            }

            // lstMatchLogs was removed, so we stop trying to access it if it's null
            // (I'll re-add a small version of it)
            lstMatchLogs.TopIndex = lstMatchLogs.Items.Count - 1;
            btnNextWeek.Enabled = true;
        }

        private async Task TryRandomTransfer()
        {
            Random rnd = new Random();
            Team t1 = db.LeagueTable[rnd.Next(18)];
            Team t2 = db.LeagueTable[rnd.Next(18)];
            if (t1.Name != t2.Name)
            {
                Player worst = t1.Roster.OrderBy(p => p.Physical).FirstOrDefault();
                Player target = t2.Roster.OrderByDescending(p => p.Physical).FirstOrDefault();
                if (worst != null && target != null) await ProcessAITransfer(t1, t2, worst, target);
            }
        }

        private void PlayMatch(Team home, Team away)
        {
            Random rnd = new Random();
            var home11 = home.GetStartingEleven();
            var away11 = away.GetStartingEleven();

            int homeAttack = home11.Sum(p => p.Passing + p.Technique) / 2 + rnd.Next(10, 50);
            int homeDefense = home11.Sum(p => p.Physical + p.Stamina) / 2 + rnd.Next(10, 50);
            int awayAttack = away11.Sum(p => p.Passing + p.Technique) / 2 + rnd.Next(10, 50);
            int awayDefense = away11.Sum(p => p.Physical + p.Stamina) / 2 + rnd.Next(10, 50);

            int homeGoals = 0, awayGoals = 0;
            
            // Simulate Stats (Passes & Tackles)
            foreach (var p in home11.Concat(away11))
            {
                p.SeasonPassesAttempted += rnd.Next(20, 50);
                p.SeasonPassesCompleted += (int)(p.SeasonPassesAttempted * (p.Passing / 120.0 + rnd.NextDouble() * 0.2));
                p.SeasonTacklesAttempted += rnd.Next(5, 15);
                p.SeasonTacklesWon += (int)(p.SeasonTacklesAttempted * (p.Tackling / 25.0 + rnd.NextDouble() * 0.3));
                
                // Bounds check
                if (p.SeasonPassesCompleted > p.SeasonPassesAttempted) p.SeasonPassesCompleted = p.SeasonPassesAttempted;
                if (p.SeasonTacklesWon > p.SeasonTacklesAttempted) p.SeasonTacklesWon = p.SeasonTacklesAttempted;
            }

            for (int minute = 1; minute <= 90; minute += 5)
            {
                // Slightly higher base chance to trigger a shot (15% -> 18%)
                if (homeAttack > awayDefense + rnd.Next(-30, 30) && rnd.Next(0, 100) < 18) { if (TryScore(home11)) homeGoals++; }
                else if (awayAttack > homeDefense + rnd.Next(-30, 30) && rnd.Next(0, 100) > 82) { if (TryScore(away11)) awayGoals++; }
            }

            lstMatchLogs.Items.Add($"⚽ {home.Name} {homeGoals} - {awayGoals} {away.Name}");
            home.GoalsFor += homeGoals; home.GoalsAgainst += awayGoals;
            away.GoalsFor += awayGoals; away.GoalsAgainst += homeGoals;

            if (homeGoals > awayGoals) { home.Points += 3; home.Won++; away.Lost++; }
            else if (awayGoals > homeGoals) { away.Points += 3; away.Won++; home.Lost++; }
            else { home.Points += 1; away.Points += 1; home.Drawn++; away.Drawn++; }
        }


        private async Task ProcessAITransfer(Team offering, Team target, Player myP, Player theirP)
        {
            Random rnd = new Random();
            var tradeOffer = new { offering_team = offering.Name, target_team = target.Name, my_player = myP, their_player = theirP, money_offered = 0 };
            var content = new StringContent(JsonSerializer.Serialize(tradeOffer), Encoding.UTF8, "application/json");
            // Try AI Decision with internal fallback
            bool accepted = false;
            int cost = (myP.Physical + myP.Passing) * 40000; // Calculated market value
            
            try {
                var response = await client.PostAsync("http://localhost:5000/transfer", content);
                if (response.IsSuccessStatusCode) {
                    AiResponse aiReply = JsonSerializer.Deserialize<AiResponse>(await response.Content.ReadAsStringAsync());
                    if (aiReply.status == "ACCEPT") accepted = true;
                    if (aiReply.status == "COUNTER" && aiReply.counter_money_needed > 0) { cost = aiReply.counter_money_needed; accepted = true; }
                }
            } catch { /* Server down, use internal logic */ }

            // Internal Logic Fallback (If server is down or uncertain)
            if (!accepted)
            {
                // Simple logic: if target needs players and has budget, 30% chance to accept if price is fair
                if (target.Budget >= cost && target.Roster.Count < 25) {
                    if (rnd.Next(100) < 30) accepted = true;
                }
            }

            if (accepted && offering.Budget >= cost) {
                offering.Budget -= cost;
                target.Budget += cost;
                offering.CurrentSeasonSpent += cost;
                target.CurrentSeasonEarned += cost;

                offering.RemovePlayer(myP); offering.AddPlayer(theirP);
                target.RemovePlayer(theirP); target.AddPlayer(myP);
                
                string log = $"{myP.Name} -> {target.Name} ({cost:N0} €)";
                lstTransferLogs.Items.Insert(0, log);
                if (lstTransferLogs.Items.Count > 30) lstTransferLogs.Items.RemoveAt(30);
            }
        }

        private void EndSeason()
        {
            isSeasonOver = true;
            btnNextWeek.Text = "Yeni Sezona Hazırlan 🏆";
            btnNextWeek.BackColor = Color.FromArgb(200, 150, 0);
            var champion = db.LeagueTable.OrderByDescending(t => t.Points).First();
            lstMatchLogs.Items.Add($"\nŞAMPİYON: {champion.Name}!");
            
            // Archive Finances
            foreach (var team in db.LeagueTable)
            {
                team.FinanceHistory.Add(new SeasonFinance {
                    SeasonNumber = currentSeason,
                    StartBudget = team.InitialBudget,
                    Spent = team.CurrentSeasonSpent,
                    Earned = team.CurrentSeasonEarned
                });
            }

            foreach (var p in db.AllPlayers.Values) p.SimulateDevelopment();
        }

        private void StartNewSeason()
        {
            currentSeason++;
            matchWeek = 1; breakWeek = 0; isSeasonOver = false;
            foreach (var team in db.LeagueTable) { 
                team.Points = 0; team.Won = 0; team.Drawn = 0; team.Lost = 0; team.GoalsFor = 0; team.GoalsAgainst = 0; 
                team.InitialBudget = team.Budget; // Set new season's start budget
                team.CurrentSeasonSpent = 0;
                team.CurrentSeasonEarned = 0;
            }
            foreach (var player in db.AllPlayers.Values) { player.Goals = 0; player.Assists = 0; }
            btnNextWeek.Text = "Sonraki Haftayı Oyna ⚽";
            btnNextWeek.BackColor = Color.FromArgb(0, 160, 0);
            lstMatchLogs.Items.Clear();
            UpdateLeagueTable();
        }

        private void RegisterGoal(Player scorer, List<Player> starting11)
        {
            scorer.Goals++;
            var assister = starting11.Where(p => p.Id != scorer.Id).OrderByDescending(a => a.Passing + new Random().Next(-10, 10)).FirstOrDefault();
            if (assister != null && new Random().Next(0, 100) < 70) assister.Assists++;
        }

        private bool TryScore(List<Player> starting11)
        {
            Random rnd = new Random();
            
            // Weighted selection: Forwards are 15x more likely to be the one taking the shot than Defenders
            var shotWeightedList = starting11.Select(p => new { 
                Player = p, 
                Weight = (p.Position == "FV" ? 25.0 : (p.Position == "OS" ? 5.0 : 1.0)) * (p.Finishing / 10.0 + 1)
            }).ToList();

            double totalWeight = shotWeightedList.Sum(x => x.Weight);
            double roll = rnd.NextDouble() * totalWeight;
            double cumulative = 0;
            Player scorer = starting11.First();

            foreach (var item in shotWeightedList) {
                cumulative += item.Weight;
                if (roll <= cumulative) { scorer = item.Player; break; }
            }

            int finishSkill = rnd.Next(0, 100) < 25 ? scorer.Heading : scorer.Finishing;
            
            // Lowered difficulty threshold (120 -> 95) to allow top forwards to reach 20-30 goals
            if (rnd.Next(0, 95) < finishSkill) { RegisterGoal(scorer, starting11); return true; }
            return false;
        }
    }

    public static class FMColors
    {
        public static Color PrimaryBg = Color.FromArgb(30, 30, 45);
        public static Color SecondaryBg = Color.FromArgb(40, 40, 60);
        public static Color SidebarBg = Color.FromArgb(20, 22, 35);
        public static Color Accent = Color.FromArgb(170, 70, 255); // Purple
    }

    public class AiResponse { public string status { get; set; } public int counter_money_needed { get; set; } public string message { get; set; } }
}
