using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MoneyballGame
{
    public class TransferForm : Form
    {
        private GameDatabase _db;
        private DataGridView dgvAllPlayers;
        private TextBox txtSearch;
        private Button btnTransfer;

        public TransferForm(GameDatabase db)
        {
            _db = db;
            this.Text = "Transfer Pazarı";
            this.Size = new Size(1000, 650);
            this.BackColor = Color.FromArgb(30, 30, 45); // FM Dark
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeUI();
            LoadPlayers();
        }

        private void InitializeUI()
        {
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(40, 40, 60) };
            this.Controls.Add(pnlHeader);

            txtSearch = new TextBox { Left = 20, Top = 20, Width = 250, BackColor = Color.FromArgb(20, 20, 35), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += (s, e) => LoadPlayers(txtSearch.Text);
            pnlHeader.Controls.Add(txtSearch);

            btnTransfer = new Button { Text = "Transfer Teklifi Yap", Left = 280, Top = 15, Width = 150, Height = 30, BackColor = Color.FromArgb(170, 70, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnTransfer.Click += (s, e) => PerformTransfer();
            pnlHeader.Controls.Add(btnTransfer);

            dgvAllPlayers = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White, GridColor = Color.Gray, BorderStyle = BorderStyle.None, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ScrollBars = ScrollBars.Both };
            dgvAllPlayers.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 60);
            dgvAllPlayers.DefaultCellStyle.ForeColor = Color.White;
            dgvAllPlayers.EnableHeadersVisualStyles = false;
            dgvAllPlayers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 20, 35);
            dgvAllPlayers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAllPlayers.CellDoubleClick += (s, e) => OpenPlayerProfile();
            this.Controls.Add(dgvAllPlayers);
        }

        private void OpenPlayerProfile()
        {
            if (dgvAllPlayers.SelectedRows.Count == 0) return;
            int id = (int)dgvAllPlayers.SelectedRows[0].Cells["ID"].Value;
            if (_db.AllPlayers.TryGetValue(id, out var player))
            {
                new PlayerProfileForm(player).ShowDialog();
            }
        }

        private void LoadPlayers(string filter = "")
        {
            var players = _db.AllPlayers.Values
                .Where(p => string.IsNullOrEmpty(filter) || p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .Select(p => new {
                    ID = p.Id,
                    İsim = p.Name,
                    Takım = _db.FindTeam(p.Id),
                    Yaş = p.Age,
                    Yetenek = (p.Passing + p.Physical + p.Finishing + p.Heading + p.Technique) / 5,
                    Bitiricilik = p.Finishing,
                    Kafa = p.Heading,
                    Teknik = p.Technique,
                    Girişkenlik = p.Stamina,
                    Potansiyel = p.Potential,
                    Değer = p.Value.ToString("C0")
                }).ToList();

            dgvAllPlayers.DataSource = players;
        }

        private void PerformTransfer()
        {
            if (dgvAllPlayers.SelectedRows.Count == 0) return;

            int playerId = (int)dgvAllPlayers.SelectedRows[0].Cells["ID"].Value;
            var player = _db.AllPlayers[playerId];
            string currentTeamName = _db.FindTeam(playerId);

            using (Form prompt = new Form())
            {
                prompt.Width = 300; prompt.Height = 150; prompt.Text = "Hangi Takıma?";
                prompt.StartPosition = FormStartPosition.CenterParent;
                Label textLabel = new Label() { Left = 20, Top = 10, Text = "Takım İsmi:" };
                ComboBox comboBox = new ComboBox() { Left = 20, Top = 30, Width = 240 };
                foreach (var team in _db.AllTeams.Keys) comboBox.Items.Add(team);
                Button confirmation = new Button() { Text = "Tamam", Left = 180, Width = 80, Top = 70, DialogResult = DialogResult.OK };
                prompt.Controls.Add(comboBox); prompt.Controls.Add(textLabel); prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    string newTeamName = comboBox.SelectedItem?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(newTeamName) && newTeamName != currentTeamName)
                    {
                        var seller = _db.AllTeams.ContainsKey(currentTeamName) ? _db.AllTeams[currentTeamName] : null;
                        var buyer = _db.AllTeams[newTeamName];
                        int price = (player.Passing + player.Physical) * 50000;

                        if (buyer.Budget < price) { 
                            MessageBox.Show($"Alıcı takımın ({newTeamName}) bütçesi yetersiz! Gerekli: {price:N0} €"); 
                            return; 
                        }

                        if (seller != null) {
                            seller.RemovePlayer(player);
                            seller.Budget += price;
                            seller.CurrentSeasonEarned += price;
                        }

                        buyer.AddPlayer(player);
                        buyer.Budget -= price;
                        buyer.CurrentSeasonSpent += price;

                        MessageBox.Show($"{player.Name}, {price:N0} € karşılığında {newTeamName} takımına transfer oldu!");
                        LoadPlayers(txtSearch.Text);
                    }
                }
            }
        }
    }
}
