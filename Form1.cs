using System;
using System.Collections.Generic;
using System.Linq;
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

        private static readonly Random rnd = new Random();

        // UI Components
        private Panel sidebar;
        private Panel header;
        private Panel mainContent;
        private Panel cardStats, cardLeague, cardBoard, cardIntel;
        private Label lblWeekInfo, lblTopScorer, lblTopAssister, lblBestPasser, lblTopTackler;
        private DataGridView dgvStandings;
        private ListBox lstMatchLogs;
        private Button btnNextWeek;
        private Label lblChampion;
        private RichTextBox rtbTeamIntel;
        private ComboBox cmbIntelTeam;


        public Form1()
        {
            this.Text = "Football Manager Sim: Moneyball";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = FMColors.PrimaryBg;

            db = new GameDatabase();
            db.InitializeData();

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

            TableLayoutPanel dashboardLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
            dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
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

            // --- TEAM INTEL KARTI (Sağ sütun, 2 satır kaplıyor) ---
            cardIntel = CreateCard("🧠 AI TAKIM İSTİHBARATI", 2, 0);

            Panel pnlSelect = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 5, 0, 5) };
            
            Label lblSelect = new Label { Text = "🔍 Takım Seç: ", Dock = DockStyle.Left, ForeColor = Color.LightGray, AutoSize = true, Font = new Font("Segoe UI", 10), Padding = new Padding(0, 3, 0, 0) };
            pnlSelect.Controls.Add(lblSelect);

            cmbIntelTeam = new ComboBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Cursor = Cursors.Hand
            };
            foreach (var t in db.LeagueTable.OrderBy(t => t.Name))
                cmbIntelTeam.Items.Add(t.Name);
            if (cmbIntelTeam.Items.Count > 0) cmbIntelTeam.SelectedIndex = 0;
            cmbIntelTeam.SelectedIndexChanged += (s, e) => RefreshTeamIntel();
            
            pnlSelect.Controls.Add(cmbIntelTeam);
            cmbIntelTeam.BringToFront(); // Label solda kalır, combobox sağını doldurur
            
            cardIntel.Controls.Add(pnlSelect);
            pnlSelect.BringToFront(); // Title'ın hemen altında görünmesi için

            rtbTeamIntel = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 9f)
            };
            cardIntel.Controls.Add(rtbTeamIntel);

            dashboardLayout.Controls.Add(cardStats, 0, 0);
            dashboardLayout.Controls.Add(cardLeague, 1, 0);
            
            // Maç sonuçları sağda, 2 satır kaplasın
            dashboardLayout.Controls.Add(cardBoard, 2, 0);
            dashboardLayout.SetRowSpan(cardBoard, 2); 
            
            // Team Intel altta, 2 sütun kaplasın
            dashboardLayout.Controls.Add(cardIntel, 0, 1);
            dashboardLayout.SetColumnSpan(cardIntel, 2); 
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
            if (text == "Fikstür") btn.Click += (s, e) => new ScorerBoardForm(db).Show();

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

            RefreshTeamIntel();
        }

        private void RefreshTeamIntel()
        {
            if (cmbIntelTeam == null || cmbIntelTeam.SelectedItem == null) return;
            string teamName = cmbIntelTeam.SelectedItem.ToString()!;
            if (!db.AllTeams.ContainsKey(teamName)) return;

            var team = db.AllTeams[teamName];
            var ai = new TeamAI(team, db);
            string intel = ai.GenerateTeamIntel();

            rtbTeamIntel.Clear();
            rtbTeamIntel.ForeColor = Color.White;

            // Satır satır renkli yazım
            foreach (var line in intel.Split('\n'))
            {
                Color lineColor = Color.White;
                if (line.Contains("═══")) lineColor = Color.FromArgb(170, 70, 255);
                else if (line.Contains("⭐")) lineColor = Color.Gold;
                else if (line.Contains("🔴")) lineColor = Color.Salmon;
                else if (line.Contains("🟢")) lineColor = Color.LimeGreen;
                else if (line.Contains("🟡")) lineColor = Color.Yellow;
                else if (line.Contains("🏆") || line.Contains("🥇")) lineColor = Color.Gold;
                else if (line.Contains("⚠️")) lineColor = Color.Orange;
                else if (line.Contains("📌")) lineColor = Color.FromArgb(100, 200, 255);
                else if (line.Contains("💡")) lineColor = Color.FromArgb(180, 220, 255);
                else if (line.Contains("💰") || line.Contains("💵")) lineColor = Color.FromArgb(100, 200, 100);
                else if (line.Contains("↑")) lineColor = Color.LimeGreen;
                else if (line.Contains("↓")) lineColor = Color.Salmon;

                rtbTeamIntel.SelectionStart = rtbTeamIntel.TextLength;
                rtbTeamIntel.SelectionColor = lineColor;
                rtbTeamIntel.AppendText(line + "\n");
            }
        }

        private async Task PlayNextWeek()
        {
            if (isSeasonOver) { StartNewSeason(); return; }

            btnNextWeek.Enabled = false;

            if (matchWeek == 18 && breakWeek < 1)
            {
                breakWeek++;
                lblWeekInfo.Text = "DEVRE ARASI — TRANSFER PENCERESİ";
                lstMatchLogs.Items.Add("\n=== 🔴 AI TRANSFER PENCERESİ AÇILIYOR ===");

                // Canlı AI Transfer Penceresini aç
                var transferWindow = new AITransferWindow(db, 8);
                transferWindow.ShowDialog();

                lstMatchLogs.Items.Add("=== TRANSFER PENCERESİ KAPANDI ===");
                UpdateDashboardStats();
                UpdateLeagueTable();
            }
            else
            {
                if (breakWeek > 0 && matchWeek == 18) { matchWeek++; breakWeek = 0; } // Devre arasından çık

                lblWeekInfo.Text = $"HAFTA: {matchWeek}";
                lstMatchLogs.Items.Add($"\n=== {matchWeek}. HAFTA MAÇLARI ===");
                var shuffledTeams = db.LeagueTable.OrderBy(a => Guid.NewGuid()).ToList();
                for (int i = 0; i < 18; i += 2) PlayMatch(shuffledTeams[i], shuffledTeams[i + 1]);
                UpdateLeagueTable();
                UpdateDashboardStats();
                matchWeek++;
                if (matchWeek > 34) EndSeason();
            }

            lstMatchLogs.TopIndex = lstMatchLogs.Items.Count - 1;
            btnNextWeek.Enabled = true;
        }

        private void PlayMatch(Team home, Team away)
        {
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

            string homeResult = homeGoals > awayGoals ? "G" : (homeGoals == awayGoals ? "B" : "M");
            string awayResult = awayGoals > homeGoals ? "G" : (awayGoals == homeGoals ? "B" : "M");
            
            home.MatchHistory.Add($"Hafta {matchWeek}: {home.Name} {homeGoals} - {awayGoals} {away.Name} ({homeResult})");
            away.MatchHistory.Add($"Hafta {matchWeek}: {home.Name} {homeGoals} - {awayGoals} {away.Name} ({awayResult})");

            if (homeGoals > awayGoals) { home.Points += 3; home.Won++; away.Lost++; }
            else if (awayGoals > homeGoals) { away.Points += 3; away.Won++; home.Lost++; }
            else { home.Points += 1; away.Points += 1; home.Drawn++; away.Drawn++; }
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

            // Değerlendirme panelini göster
            new SeasonEvaluationForm(db).ShowDialog();
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
                team.MatchHistory.Clear();
            }
            foreach (var player in db.AllPlayers.Values) { player.ResetSeasonStats(); }
            
            // Yeni sezon hedeflerini belirle
            db.CalculateTeamGoals();
            RefreshTeamIntel();

            btnNextWeek.Text = "Sonraki Haftayı Oyna ⚽";
            btnNextWeek.BackColor = Color.FromArgb(0, 160, 0);
            lstMatchLogs.Items.Clear();
            UpdateLeagueTable();
        }

        private void RegisterGoal(Player scorer, List<Player> starting11)
        {
            scorer.Goals++;
            var assister = starting11.Where(p => p.Id != scorer.Id).OrderByDescending(a => a.Passing + rnd.Next(-10, 10)).FirstOrDefault();
            if (assister != null && rnd.Next(0, 100) < 70) assister.Assists++;
        }

        private bool TryScore(List<Player> starting11)
        {
            
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
}
