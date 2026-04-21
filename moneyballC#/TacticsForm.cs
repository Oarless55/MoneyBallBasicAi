using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MoneyballGame
{
    public partial class TacticsForm : Form
    {
        private GameDatabase db;
        private ComboBox cmbTeams;
        private Panel pitchPanel;
        private DataGridView dgvSubs;
        private List<PlayerSlot> playerSlots = new List<PlayerSlot>();

        public TacticsForm(GameDatabase database)
        {
            this.db = database;
            this.Text = "Takım Taktiği & Diziliş";
            this.Size = new Size(1100, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = FMColors.PrimaryBg;

            InitializeUI();
            LoadTeams();
        }

        private void InitializeUI()
        {
            // Team Selector
            cmbTeams = new ComboBox { Left = 20, Top = 20, Width = 250, BackColor = FMColors.SecondaryBg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12) };
            cmbTeams.SelectedIndexChanged += (s, e) => LoadTactics(cmbTeams.SelectedItem.ToString());
            this.Controls.Add(cmbTeams);

            Label lblTitle = new Label { Text = "SAHA DİZİLİŞİ (4-4-2)", Left = 300, Top = 20, ForeColor = FMColors.Accent, Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblTitle);

            // Pitch Panel
            pitchPanel = new Panel { Left = 20, Top = 70, Width = 600, Height = 670, BackColor = Color.FromArgb(40, 70, 40), BorderStyle = BorderStyle.FixedSingle };
            pitchPanel.Paint += PitchPanel_Paint;
            this.Controls.Add(pitchPanel);

            // Subs Section
            Panel subsPanel = new Panel { Left = 640, Top = 70, Width = 420, Height = 670, BackColor = FMColors.SecondaryBg, Padding = new Padding(10) };
            this.Controls.Add(subsPanel);

            Label lblSubs = new Label { Text = "YEDEKLER & KADRO", Dock = DockStyle.Top, Height = 30, ForeColor = Color.Gold, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            subsPanel.Controls.Add(lblSubs);

            dgvSubs = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = FMColors.SecondaryBg, BorderStyle = BorderStyle.None, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ForeColor = Color.White, ReadOnly = true, AllowUserToAddRows = false, AutoGenerateColumns = true };
            dgvSubs.DefaultCellStyle.BackColor = FMColors.SecondaryBg;
            dgvSubs.DefaultCellStyle.ForeColor = Color.White;
            dgvSubs.ColumnHeadersDefaultCellStyle.BackColor = FMColors.SidebarBg;
            dgvSubs.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSubs.EnableHeadersVisualStyles = false;
            subsPanel.Controls.Add(dgvSubs);

            // Define 11 Slots for 4-4-2
            DefineSlots();
        }

        private void DefineSlots()
        {
            // Coordinates based on 600x670 pitch
            // GK
            AddSlot("KL", 265, 580);
            // DEF
            AddSlot("DF", 50, 480); // LB
            AddSlot("DF", 190, 500); // CB1
            AddSlot("DF", 340, 500); // CB2
            AddSlot("DF", 480, 480); // RB
            // MID
            AddSlot("OS", 50, 280); // LM
            AddSlot("OS", 190, 300); // CM1
            AddSlot("OS", 340, 300); // CM2
            AddSlot("OS", 480, 280); // RM
            // FW
            AddSlot("FV", 160, 100); // ST1
            AddSlot("FV", 370, 100); // ST2
        }

        private void AddSlot(string pos, int x, int y)
        {
            PlayerSlot slot = new PlayerSlot { PositionType = pos, Location = new Point(x, y) };
            playerSlots.Add(slot);
            
            Panel p = new Panel { Width = 70, Height = 80, Left = x, Top = y, BackColor = Color.Transparent };
            slot.UIContainer = p;

            Label lblIcon = new Label { Text = "\ud83d\udc55", Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 20), ForeColor = Color.White };
            Label lblName = new Label { Text = "Boş", Dock = DockStyle.Bottom, Height = 40, TextAlign = ContentAlignment.TopCenter, ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            
            p.Controls.Add(lblIcon);
            p.Controls.Add(lblName);
            slot.UIName = lblName;

            pitchPanel.Controls.Add(p);
        }

        private void PitchPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen whitePen = new Pen(Color.White, 2);

            // Pitch Borders
            g.DrawRectangle(whitePen, 10, 10, pitchPanel.Width - 20, pitchPanel.Height - 20);
            // Center Line
            g.DrawLine(whitePen, 10, pitchPanel.Height / 2, pitchPanel.Width - 10, pitchPanel.Height / 2);
            // Center Circle
            g.DrawEllipse(whitePen, pitchPanel.Width / 2 - 50, pitchPanel.Height / 2 - 50, 100, 100);
            // Penalty Box (Bottom)
            g.DrawRectangle(whitePen, pitchPanel.Width / 2 - 150, pitchPanel.Height - 110, 300, 100);
            // Penalty Box (Top)
            g.DrawRectangle(whitePen, pitchPanel.Width / 2 - 150, 10, 300, 100);
        }

        private void LoadTeams()
        {
            cmbTeams.Items.Clear();
            foreach (var team in db.LeagueTable) cmbTeams.Items.Add(team.Name);
            if (cmbTeams.Items.Count > 0) cmbTeams.SelectedIndex = 0;
        }

        private void LoadTactics(string teamName)
        {
            if (!db.AllTeams.ContainsKey(teamName)) return;
            var team = db.AllTeams[teamName];
            var roster = team.Roster.ToList();

            // Clear slots
            foreach (var s in playerSlots) s.UIName.Text = "Boş";

            // Simple Auto-Fill for 4-4-2
            var kl = roster.Where(p => p.Position == "KL").OrderByDescending(p => p.Passing + p.Physical).ToList();
            var df = roster.Where(p => p.Position == "DF").OrderByDescending(p => p.Passing + p.Physical).ToList();
            var os = roster.Where(p => p.Position == "OS").OrderByDescending(p => p.Passing + p.Physical).ToList();
            var fv = roster.Where(p => p.Position == "FV").OrderByDescending(p => p.Passing + p.Physical).ToList();

            HashSet<int> startingIds = new HashSet<int>();

            // Map KL
            if (kl.Count > 0) { playerSlots[0].UIName.Text = kl[0].Name; startingIds.Add(kl[0].Id); }
            // Map DF
            for (int i = 0; i < 4 && i < df.Count; i++) { playerSlots[i + 1].UIName.Text = df[i].Name; startingIds.Add(df[i].Id); }
            // Map OS
            for (int i = 0; i < 4 && i < os.Count; i++) { playerSlots[i + 5].UIName.Text = os[i].Name; startingIds.Add(os[i].Id); }
            // Map FV
            for (int i = 0; i < 2 && i < fv.Count; i++) { playerSlots[i + 9].UIName.Text = fv[i].Name; startingIds.Add(fv[i].Id); }

            // Subs (Everyone not in starting 11)
            var subs = roster.Where(p => !startingIds.Contains(p.Id)).Select(p => new {
                İsim = p.Name,
                Mevki = p.Position,
                Güç = (p.Passing + p.Physical) / 2,
                Yaş = p.Age
            }).ToList();

            dgvSubs.DataSource = subs;
        }

        private class PlayerSlot
        {
            public string PositionType { get; set; }
            public Point Location { get; set; }
            public Panel UIContainer { get; set; }
            public Label UIName { get; set; }
        }
    }
}
