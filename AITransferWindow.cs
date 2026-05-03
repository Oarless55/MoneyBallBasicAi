using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoneyballGame
{
    public class AITransferWindow : Form
    {
        private GameDatabase _db;
        private RichTextBox rtbLiveFeed;
        private RichTextBox rtbAnalysis;
        private ComboBox cmbTeamAnalysis;
        private Label lblRoundInfo;
        private Label lblCompletedCount;
        private Label lblRejectedCount;
        private Button btnNextRound;
        private Button btnAutoPlay;
        private Button btnClose;

        private int _currentRound = 0;
        private int _totalRounds;
        private int _completedTransfers = 0;
        private int _rejectedTransfers = 0;
        private int _freeAgentSignings = 0;
        private bool _autoPlaying = false;
        private Dictionary<string, TeamAI> _teamAIs = new();
        private Dictionary<string, int> _teamTransferCounts = new();

        public AITransferWindow(GameDatabase db, int totalRounds = 16)
        {
            _db = db;
            _totalRounds = totalRounds;

            // Her takıma bir AI menajer ata
            foreach (var team in _db.LeagueTable)
            {
                _teamAIs[team.Name] = new TeamAI(team, _db);
                _teamTransferCounts[team.Name] = 0;
            }

            this.Text = "🔴 CANLI TRANSFER MERKEZİ";
            this.Size = new Size(1300, 850);
            this.BackColor = Color.FromArgb(18, 18, 30);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            InitializeUI();
            AppendHeader();
        }

        private void InitializeUI()
        {
            // === HEADER ===
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(140, 20, 20)
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "📡 CANLI TRANSFER MERKEZİ",
                Left = 20, Top = 10, AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold)
            };
            pnlHeader.Controls.Add(lblTitle);

            lblRoundInfo = new Label
            {
                Text = $"Tur: 0 / {_totalRounds}",
                Left = 20, Top = 45, AutoSize = true,
                ForeColor = Color.FromArgb(255, 200, 200),
                Font = new Font("Segoe UI", 10)
            };
            pnlHeader.Controls.Add(lblRoundInfo);

            // Buttons
            btnNextRound = new Button
            {
                Text = "▶ Sonraki Tur",
                Left = 700, Top = 15, Width = 150, Height = 40,
                BackColor = Color.FromArgb(0, 140, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnNextRound.FlatAppearance.BorderSize = 0;
            btnNextRound.Click += async (s, e) => await PlayOneRound();
            pnlHeader.Controls.Add(btnNextRound);

            btnAutoPlay = new Button
            {
                Text = "⏩ Otomatik Oynat",
                Left = 860, Top = 15, Width = 170, Height = 40,
                BackColor = Color.FromArgb(170, 70, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnAutoPlay.FlatAppearance.BorderSize = 0;
            btnAutoPlay.Click += async (s, e) => await AutoPlayAll();
            pnlHeader.Controls.Add(btnAutoPlay);

            btnClose = new Button
            {
                Text = "✕ Kapat",
                Left = 1040, Top = 15, Width = 120, Height = 40,
                BackColor = Color.FromArgb(80, 20, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { _autoPlaying = false; this.Close(); };
            pnlHeader.Controls.Add(btnClose);

            // === MAIN LAYOUT ===
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(18, 18, 30)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            this.Controls.Add(mainLayout);

            // === LEFT: TAKIM ANALİZİ ===
            Panel pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 42),
                Padding = new Padding(10),
                Margin = new Padding(5)
            };

            Label lblAnalysisTitle = new Label
            {
                Text = "📊 TAKIM ANALİZİ",
                Dock = DockStyle.Top, Height = 30,
                ForeColor = Color.FromArgb(170, 70, 255),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            pnlLeft.Controls.Add(lblAnalysisTitle);

            cmbTeamAnalysis = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(35, 35, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var team in _db.LeagueTable.OrderBy(t => t.Name))
                cmbTeamAnalysis.Items.Add(team.Name);
            cmbTeamAnalysis.SelectedIndexChanged += (s, e) => ShowTeamAnalysis();
            pnlLeft.Controls.Add(cmbTeamAnalysis);

            rtbAnalysis = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f)
            };
            pnlLeft.Controls.Add(rtbAnalysis);

            // Summary panel at bottom of left
            Panel pnlSummary = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(20, 20, 35),
                Padding = new Padding(10)
            };

            Label lblSummaryTitle = new Label
            {
                Text = "TRANSFER ÖZETİ",
                Dock = DockStyle.Top, Height = 20,
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnlSummary.Controls.Add(lblSummaryTitle);

            lblCompletedCount = new Label
            {
                Text = "✅ Tamamlanan: 0",
                Left = 10, Top = 30, AutoSize = true,
                ForeColor = Color.LimeGreen,
                Font = new Font("Segoe UI", 10)
            };
            pnlSummary.Controls.Add(lblCompletedCount);

            lblRejectedCount = new Label
            {
                Text = "❌ Reddedilen: 0",
                Left = 10, Top = 55, AutoSize = true,
                ForeColor = Color.Salmon,
                Font = new Font("Segoe UI", 10)
            };
            pnlSummary.Controls.Add(lblRejectedCount);

            pnlLeft.Controls.Add(pnlSummary);

            mainLayout.Controls.Add(pnlLeft, 0, 0);

            // === RIGHT: CANLI AKIŞ ===
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 22, 38),
                Padding = new Padding(10),
                Margin = new Padding(5)
            };

            Label lblFeedTitle = new Label
            {
                Text = "📡 CANLI MÜZAKERE AKIŞI",
                Dock = DockStyle.Top, Height = 30,
                ForeColor = Color.FromArgb(255, 180, 0),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            pnlRight.Controls.Add(lblFeedTitle);

            rtbLiveFeed = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 28),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f)
            };
            pnlRight.Controls.Add(rtbLiveFeed);

            mainLayout.Controls.Add(pnlRight, 1, 0);

            if (cmbTeamAnalysis.Items.Count > 0) cmbTeamAnalysis.SelectedIndex = 0;
        }

        // --- CANLI AKIŞA YAZDIRMA ---
        private void AppendFeed(string text, Color color)
        {
            if (InvokeRequired) { Invoke(() => AppendFeed(text, color)); return; }
            rtbLiveFeed.SelectionStart = rtbLiveFeed.TextLength;
            rtbLiveFeed.SelectionColor = color;
            rtbLiveFeed.AppendText(text + "\n");
            rtbLiveFeed.ScrollToCaret();
        }

        private void AppendSeparator()
        {
            AppendFeed("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", Color.FromArgb(60, 60, 80));
        }

        private void AppendHeader()
        {
            AppendFeed("╔══════════════════════════════════════════════════╗", Color.FromArgb(140, 20, 20));
            AppendFeed("║        🔴 TRANSFER PENCERESİ AÇILDI 🔴          ║", Color.FromArgb(255, 80, 80));
            AppendFeed("║  AI Menajerler analiz yapıyor ve teklif hazırlıyor  ║", Color.FromArgb(200, 200, 200));
            AppendFeed("╚══════════════════════════════════════════════════╝", Color.FromArgb(140, 20, 20));
            AppendFeed("", Color.White);
        }

        private void UpdateSummary()
        {
            if (InvokeRequired) { Invoke(UpdateSummary); return; }
            lblCompletedCount.Text = $"✅ Transfer: {_completedTransfers} | 🌍 Serbest: {_freeAgentSignings}";
            lblRejectedCount.Text = $"❌ Reddedilen: {_rejectedTransfers}";
            lblRoundInfo.Text = $"Tur: {_currentRound} / {_totalRounds} | Havuz: {_db.FreeAgents.Count} oyuncu";
        }

        private void ShowTeamAnalysis()
        {
            if (cmbTeamAnalysis.SelectedItem == null) return;
            string teamName = cmbTeamAnalysis.SelectedItem.ToString()!;
            if (!_teamAIs.ContainsKey(teamName)) return;

            var ai = _teamAIs[teamName];
            var analysis = ai.Analyze();

            rtbAnalysis.Clear();
            rtbAnalysis.SelectionColor = Color.Gold;
            rtbAnalysis.AppendText($"━━━ {teamName} ━━━\n\n");
            rtbAnalysis.SelectionColor = Color.White;
            rtbAnalysis.AppendText(analysis.AnalysisReport);

            // Oyuncu listesi
            rtbAnalysis.SelectionColor = Color.FromArgb(170, 70, 255);
            rtbAnalysis.AppendText("\n━━━ KADRO ━━━\n");

            var team = _db.AllTeams[teamName];
            foreach (var pos in new[] { "KL", "DF", "OS", "FV" })
            {
                var players = team.Roster
                    .Where(p => p.Position == pos)
                    .OrderByDescending(p => TeamAI.GetOverallRating(p));

                foreach (var p in players)
                {
                    double rating = TeamAI.GetOverallRating(p);
                    Color ratingColor = rating >= 70 ? Color.LimeGreen :
                                        rating >= 50 ? Color.Yellow : Color.Salmon;

                    rtbAnalysis.SelectionColor = Color.Gray;
                    rtbAnalysis.AppendText($"  {pos} ");
                    rtbAnalysis.SelectionColor = Color.White;
                    rtbAnalysis.AppendText($"{p.Name}");
                    rtbAnalysis.SelectionColor = ratingColor;
                    rtbAnalysis.AppendText($" [{rating:F0}]");
                    rtbAnalysis.SelectionColor = Color.Gray;
                    rtbAnalysis.AppendText($" {p.Age}y {TeamAI.CalculateMarketValue(p):N0}€\n");
                }
            }
        }

        // --- TEK TUR OYNA ---
        private async Task PlayOneRound()
        {
            if (_currentRound >= _totalRounds)
            {
                AppendFeed("\n🏁 TRANSFER PENCERESİ KAPANDI!", Color.FromArgb(255, 80, 80));
                btnNextRound.Enabled = false;
                btnAutoPlay.Enabled = false;
                return;
            }

            btnNextRound.Enabled = false;
            btnAutoPlay.Enabled = false;
            _currentRound++;
            UpdateSummary();

            AppendFeed($"\n🔔 ══ TUR {_currentRound} / {_totalRounds} ══", Color.FromArgb(255, 200, 0));
            await Task.Delay(200);

            // Tüm takımlar sırayla işlem yapar — az transfer yapanlar önce
            var orderedTeams = _db.LeagueTable
                .OrderBy(t => _teamTransferCounts.GetValueOrDefault(t.Name, 0))
                .ThenBy(_ => Guid.NewGuid())
                .ToList();

            foreach (var team in orderedTeams)
            {
                var ai = _teamAIs[team.Name];
                int teamTransfers = _teamTransferCounts.GetValueOrDefault(team.Name, 0);

                // 3+ transfer yapmış takımlar bu turda %60 ihtimalle pas geçer
                if (teamTransfers >= 3 && new Random().Next(100) < 60) continue;

                // %40 ihtimalle serbest oyuncu, %60 lig içi transfer
                bool tryFreeAgent = new Random().Next(100) < 40;

                if (tryFreeAgent)
                {
                    // --- SERBEST OYUNCU İMZALAMA ---
                    var freeResult = ai.TrySignFreeAgent();
                    if (freeResult != null)
                    {
                        var (player, price, reason) = freeResult.Value;
                        if (player != null && team.Budget >= price)
                        {
                            AppendSeparator();
                            AppendFeed($"🌍 {team.Name} — Serbest Piyasayı Tarıyor...", Color.FromArgb(100, 200, 255));
                            await Task.Delay(300);
                            AppendFeed($"   💬 \"{reason}\"", Color.FromArgb(200, 200, 200));
                            await Task.Delay(400);

                            // İmzala
                            _db.FreeAgents.Remove(player);
                            team.AddPlayer(player);
                            team.Budget -= price;
                            team.CurrentSeasonSpent += price;
                            _freeAgentSignings++;
                            _teamTransferCounts[team.Name] = teamTransfers + 1;
                            UpdateSummary();

                            AppendFeed("", Color.White);
                            AppendFeed($"   ╔══════════════════════════════════════════╗", Color.FromArgb(0, 180, 255));
                            AppendFeed($"   ║  🌍 SERBEST OYUNCU İMZALANDI!         ║", Color.FromArgb(0, 180, 255));
                            AppendFeed($"   ║  {player.Name} → {team.Name,-20}  ║", Color.White);
                            AppendFeed($"   ║  Bonservis: {price:N0} €{new string(' ', Math.Max(0, 22 - price.ToString("N0").Length))}║", Color.Gold);
                            AppendFeed($"   ╚══════════════════════════════════════════╝", Color.FromArgb(0, 180, 255));
                            await Task.Delay(300);
                            continue;
                        }
                    }
                }

                // --- LİG İÇİ TRANSFER ---
                var analysis = ai.Analyze();
                bool hasNeed = analysis.NeedPositions.Count > 0 || teamTransfers < 3;

                if (!hasNeed)
                {
                    // Zaten yeterince transfer yaptıysa ve ihtiyacı yoksa geç
                    continue;
                }

                var proposal = ai.TryGenerateProposal();
                if (proposal == null) continue;

                AppendSeparator();
                AppendFeed($"🏟️ {team.Name} — Lig İçi Transfer Arıyor...", Color.FromArgb(100, 180, 255));
                await Task.Delay(300);
                AppendFeed($"   💬 \"{proposal.Reasoning}\"", Color.FromArgb(200, 200, 200));
                await Task.Delay(400);
                AppendFeed($"   🤝 TEKLİF: {proposal.PlayerWanted.Name} için {proposal.MoneyOffered:N0} € → {proposal.TargetTeam.Name}", Color.FromArgb(255, 220, 100));
                await Task.Delay(500);

                var targetAI = _teamAIs[proposal.TargetTeam.Name];
                var response = targetAI.EvaluateIncomingOffer(proposal);

                AppendFeed($"   🏟️ {proposal.TargetTeam.Name} AI değerlendiriyor...", Color.FromArgb(100, 180, 255));
                await Task.Delay(400);

                if (response.Accepted)
                {
                    await ExecuteTransfer(proposal, response.FinalPrice);
                    _teamTransferCounts[team.Name] = teamTransfers + 1;
                }
                else
                {
                    AppendFeed($"   ❌ {proposal.TargetTeam.Name}: \"{response.Reasoning}\"", Color.FromArgb(255, 100, 100));
                    await Task.Delay(300);

                    if (response.CounterOffer > 0)
                    {
                        AppendFeed($"   🔄 Karşı Teklif: {response.CounterOffer:N0} €", Color.FromArgb(255, 180, 50));
                        await Task.Delay(400);

                        bool acceptsCounter = ai.WillAcceptCounter(proposal, response.CounterOffer);
                        if (acceptsCounter)
                        {
                            AppendFeed($"   💬 {team.Name}: \"Tamam, kabul ediyoruz.\"", Color.FromArgb(200, 200, 200));
                            await Task.Delay(200);
                            await ExecuteTransfer(proposal, response.CounterOffer);
                            _teamTransferCounts[team.Name] = teamTransfers + 1;
                        }
                        else
                        {
                            AppendFeed($"   🚫 {team.Name}: \"Fiyat çok yüksek, vazgeçiyoruz.\"", Color.FromArgb(180, 80, 80));
                            _rejectedTransfers++;
                            UpdateSummary();
                        }
                    }
                    else
                    {
                        _rejectedTransfers++;
                        UpdateSummary();
                    }
                }
                await Task.Delay(200);
            }

            btnNextRound.Enabled = _currentRound < _totalRounds;
            btnAutoPlay.Enabled = _currentRound < _totalRounds;
            ShowTeamAnalysis();
        }

        // --- TRANSFER UYGULA ---
        private async Task ExecuteTransfer(TransferProposal proposal, int price)
        {
            var player = proposal.PlayerWanted;
            var buyer = proposal.OfferingTeam;
            var seller = proposal.TargetTeam;

            // Budget check
            if (buyer.Budget < price)
            {
                AppendFeed($"   ⚠️ {buyer.Name} bütçesi yetersiz! Transfer iptal.", Color.FromArgb(200, 200, 100));
                _rejectedTransfers++;
                UpdateSummary();
                return;
            }

            // Execute
            seller.RemovePlayer(player);
            buyer.AddPlayer(player);
            buyer.Budget -= price;
            seller.Budget += price;
            buyer.CurrentSeasonSpent += price;
            seller.CurrentSeasonEarned += price;

            _completedTransfers++;
            UpdateSummary();

            AppendFeed("", Color.White);
            AppendFeed($"   ╔══════════════════════════════════════════╗", Color.LimeGreen);
            AppendFeed($"   ║  ✅ TRANSFER TAMAMLANDI!                 ║", Color.LimeGreen);
            AppendFeed($"   ║  {player.Name} → {buyer.Name,-20}  ║", Color.White);
            AppendFeed($"   ║  Bonservis: {price:N0} €{new string(' ', Math.Max(0, 22 - price.ToString("N0").Length))}║", Color.Gold);
            AppendFeed($"   ╚══════════════════════════════════════════╝", Color.LimeGreen);
            AppendFeed("", Color.White);

            await Task.Delay(400);
        }

        // --- OTOMATİK OYNAT ---
        private async Task AutoPlayAll()
        {
            _autoPlaying = true;
            btnAutoPlay.Text = "⏸ Devam Ediyor...";
            btnAutoPlay.Enabled = false;
            btnNextRound.Enabled = false;

            while (_currentRound < _totalRounds && _autoPlaying)
            {
                await PlayOneRound();
                await Task.Delay(500);
            }

            _autoPlaying = false;
            if (_currentRound >= _totalRounds)
            {
                AppendFeed("\n╔══════════════════════════════════════════════════╗", Color.Gold);
                AppendFeed("║      🏁 TRANSFER PENCERESİ KAPANDI 🏁            ║", Color.Gold);
                AppendFeed($"║  Lig İçi: {_completedTransfers}  |  Serbest: {_freeAgentSignings}  |  Red: {_rejectedTransfers,-5} ║", Color.White);
                AppendFeed("╚══════════════════════════════════════════════════╝", Color.Gold);

                // Takım bazında özet
                AppendFeed("\n📊 TAKİM BAZLI TRANSFER ÖZETİ:", Color.FromArgb(170, 70, 255));
                foreach (var kvp in _teamTransferCounts.OrderByDescending(x => x.Value))
                {
                    string bar = new string('█', kvp.Value);
                    Color c = kvp.Value >= 3 ? Color.LimeGreen : (kvp.Value >= 1 ? Color.Yellow : Color.Salmon);
                    AppendFeed($"  {kvp.Key,-20} {bar} ({kvp.Value} transfer)", c);
                }
            }
        }
    }
}
