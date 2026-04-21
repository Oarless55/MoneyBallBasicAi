using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MoneyballGame
{
    public class ScorerBoardForm : Form
    {
        private GameDatabase _db;
        private DataGridView dgvScorers;
        private DataGridView dgvAssisters;

        public ScorerBoardForm(GameDatabase db)
        {
            _db = db;
            this.Text = "Lig İstatistikleri";
            this.Size = new Size(1000, 600);
            this.BackColor = Color.FromArgb(30, 30, 45);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeUI();
            LoadStats();
        }

        private void InitializeUI()
        {
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            this.Controls.Add(layout);

            Panel pnlScorers = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            pnlScorers.Controls.Add(new Label { Text = "GOL KRALLIĞI", ForeColor = Color.FromArgb(170, 70, 255), Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
            dgvScorers = CreateStyledDgv();
            pnlScorers.Controls.Add(dgvScorers);
            layout.Controls.Add(pnlScorers, 0, 0);

            Panel pnlAssisters = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            pnlAssisters.Controls.Add(new Label { Text = "ASİST KRALLIĞI", ForeColor = Color.FromArgb(170, 70, 255), Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
            dgvAssisters = CreateStyledDgv();
            pnlAssisters.Controls.Add(dgvAssisters);
            layout.Controls.Add(pnlAssisters, 1, 0);
        }

        private DataGridView CreateStyledDgv()
        {
            DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, GridColor = Color.Gray, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 60);
            dgv.DefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 20, 35);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            return dgv;
        }

        private void LoadStats()
        {
            var topScorers = _db.AllPlayers.Values
                .Where(p => p.Goals > 0)
                .OrderByDescending(p => p.Goals)
                .Take(20)
                .Select(p => new { Oyuncu = p.Name, Takım = FindTeam(p.Id), Gol = p.Goals })
                .ToList();

            dgvScorers.DataSource = topScorers;

            var topAssisters = _db.AllPlayers.Values
                .Where(p => p.Assists > 0)
                .OrderByDescending(p => p.Assists)
                .Take(20)
                .Select(p => new { Oyuncu = p.Name, Takım = FindTeam(p.Id), Asist = p.Assists })
                .ToList();

            dgvAssisters.DataSource = topAssisters;
        }

        private string FindTeam(int playerId)
        {
            foreach (var team in _db.AllTeams.Values)
            {
                if (team.Roster.Any(p => p.Id == playerId)) return team.Name;
            }
            return "Serbest";
        }
    }
}
