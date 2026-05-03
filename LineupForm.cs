using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MoneyballGame
{
    public class LineupForm : Form
    {
        private GameDatabase _db;
        private ComboBox cmbTeams;
        private DataGridView dgvLineup;

        public LineupForm(GameDatabase db)
        {
            _db = db;
            this.Text = "Takım Kadroları";
            this.Size = new Size(900, 600);
            this.BackColor = Color.FromArgb(30, 30, 45);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeUI();
        }

        private void InitializeUI()
        {
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(40, 40, 60) };
            this.Controls.Add(pnlHeader);

            cmbTeams = new ComboBox { Left = 20, Top = 20, Width = 200, BackColor = Color.FromArgb(20, 20, 35), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            foreach (var teamName in _db.AllTeams.Keys) cmbTeams.Items.Add(teamName);
            cmbTeams.SelectedIndexChanged += (s, e) => LoadLineup(cmbTeams.SelectedItem?.ToString());
            pnlHeader.Controls.Add(cmbTeams);

            dgvLineup = new DataGridView 
            { 
                Dock = DockStyle.Fill, 
                BackgroundColor = Color.FromArgb(30, 30, 45), 
                ForeColor = Color.White, 
                GridColor = Color.FromArgb(50, 50, 70), 
                BorderStyle = BorderStyle.None, 
                RowHeadersVisible = false, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, 
                AllowUserToAddRows = false, 
                ReadOnly = true, 
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, 
                ScrollBars = ScrollBars.Both,
                EnableHeadersVisualStyles = false,
                ColumnHeadersVisible = true, // Force headers
                ColumnHeadersHeight = 35
            };
            dgvLineup.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 60);
            dgvLineup.DefaultCellStyle.ForeColor = Color.White;
            dgvLineup.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 20, 35);
            dgvLineup.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gold; // Highlight headers
            dgvLineup.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvLineup.CellDoubleClick += (s, e) => OpenPlayerProfile();
            this.Controls.Add(dgvLineup);
        }

        private void OpenPlayerProfile()
        {
            if (dgvLineup.SelectedRows.Count == 0) return;
            int id = (int)dgvLineup.SelectedRows[0].Cells["ID"].Value;
            if (_db.AllPlayers.TryGetValue(id, out var player))
            {
                new PlayerProfileForm(player).ShowDialog();
            }
        }

        private void LoadLineup(string teamName)
        {
            if (!_db.AllTeams.ContainsKey(teamName)) return;
            var team = _db.AllTeams[teamName];

            var lineup = team.Roster
                .OrderByDescending(p => p.Passing + p.Physical)
                .Select(p => new {
                    ID = p.Id,
                    İsim = p.Name,
                    Mevki = p.Position,
                    Güç = (p.Passing + p.Physical + p.Technique) / 3,
                    Yaş = p.Age,
                    Değer = p.Value.ToString("N0") + " €"
                }).ToList();

            dgvLineup.DataSource = lineup;
        }
    }
}
