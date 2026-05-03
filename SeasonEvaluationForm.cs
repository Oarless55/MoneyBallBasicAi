using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MoneyballGame
{
    public class SeasonEvaluationForm : Form
    {
        private GameDatabase _db;
        private DataGridView dgvEvaluation;

        public SeasonEvaluationForm(GameDatabase db)
        {
            _db = db;
            
            this.Text = "📊 Sezon Sonu AI Menajer Değerlendirmesi";
            this.Size = new Size(800, 600);
            this.BackColor = FMColors.PrimaryBg;
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblTitle = new Label 
            { 
                Text = "SEZON HEDEFİ DEĞERLENDİRMELERİ", 
                Dock = DockStyle.Top, 
                Height = 60, 
                ForeColor = Color.Gold, 
                Font = new Font("Segoe UI", 18, FontStyle.Bold), 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            this.Controls.Add(lblTitle);

            dgvEvaluation = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = FMColors.SecondaryBg,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ForeColor = Color.White,
                GridColor = Color.FromArgb(50, 50, 70),
                ScrollBars = ScrollBars.Both,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            dgvEvaluation.DefaultCellStyle.BackColor = FMColors.SecondaryBg;
            dgvEvaluation.DefaultCellStyle.ForeColor = Color.White;
            dgvEvaluation.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            dgvEvaluation.RowTemplate.Height = 35;
            dgvEvaluation.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 65);
            dgvEvaluation.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvEvaluation.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dgvEvaluation.ColumnHeadersHeight = 40;
            dgvEvaluation.EnableHeadersVisualStyles = false;
            
            // Çift tıklandığında fikstür sayfasını aç
            dgvEvaluation.CellDoubleClick += (s, e) => 
            {
                if (e.RowIndex >= 0)
                {
                    string teamName = dgvEvaluation.Rows[e.RowIndex].Cells["Takım"].Value.ToString()!;
                    if (_db.AllTeams.ContainsKey(teamName))
                    {
                        var team = _db.AllTeams[teamName];
                        new TeamFixtureForm(team).ShowDialog();
                    }
                }
            };

            this.Controls.Add(dgvEvaluation);
            this.Load += SeasonEvaluationForm_Load;
        }

        private void SeasonEvaluationForm_Load(object sender, EventArgs e)
        {
            var sortedTable = _db.LeagueTable.OrderByDescending(t => t.Points).ThenByDescending(t => t.GoalDifference).ToList();
            
            var displayData = sortedTable.Select((t, index) => 
            {
                int currentRank = index + 1;
                bool isSuccess = currentRank <= t.TargetMaxRank;
                string status = isSuccess ? "✅ BAŞARILI" : "❌ BAŞARISIZ";
                
                return new 
                {
                    Sıra = currentRank,
                    Takım = t.Name,
                    Puan = t.Points,
                    Hedef = t.TargetGoalText,
                    Gerçekleşen = currentRank,
                    Durum = status
                };
            }).ToList();

            dgvEvaluation.DataSource = displayData;

            // Renklendirme
            dgvEvaluation.DataBindingComplete += (s, ev) => 
            {
                foreach (DataGridViewRow row in dgvEvaluation.Rows)
                {
                    string status = row.Cells["Durum"].Value?.ToString() ?? "";
                    if (status.Contains("BAŞARILI"))
                    {
                        row.Cells["Durum"].Style.ForeColor = Color.LimeGreen;
                        row.Cells["Durum"].Style.Font = new Font(dgvEvaluation.Font, FontStyle.Bold);
                    }
                    else
                    {
                        row.Cells["Durum"].Style.ForeColor = Color.Salmon;
                    }
                }
            };
        }
    }
}
