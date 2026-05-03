using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MoneyballGame
{
    public class FinanceForm : Form
    {
        private GameDatabase _db;
        private DataGridView dgvFinance;
        private ComboBox cmbSeason;

        public FinanceForm(GameDatabase db)
        {
            _db = db;
            this.Text = "Lig Finansal Durum Raporu";
            this.Size = new Size(1100, 700);
            this.BackColor = FMColors.PrimaryBg;
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeUI();
            LoadCurrentSeasonData();
        }

        private void InitializeUI()
        {
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(40, 40, 60) };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label { Text = "LİG TASARRUF VE HARCAMA RAPORU", Left = 20, Top = 20, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold) };
            pnlHeader.Controls.Add(lblTitle);

            // Season Selector (Optional for now, but ready)
            Label lblSeason = new Label { Text = "Sezon Seç:", Left = 700, Top = 25, ForeColor = Color.White, AutoSize = true };
            cmbSeason = new ComboBox { Left = 780, Top = 22, Width = 150, BackColor = FMColors.SecondaryBg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbSeason.Items.Add("Mevcut Sezon (Canlı)");
            cmbSeason.SelectedIndex = 0;
            pnlHeader.Controls.Add(lblSeason);
            pnlHeader.Controls.Add(cmbSeason);

            dgvFinance = new DataGridView 
            { 
                Dock = DockStyle.Fill, 
                BackgroundColor = FMColors.PrimaryBg, 
                BorderStyle = BorderStyle.None, 
                RowHeadersVisible = false, 
                AllowUserToAddRows = false, 
                ReadOnly = true, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, 
                ForeColor = Color.White, 
                GridColor = Color.FromArgb(50, 50, 70),
                ColumnHeadersHeight = 40,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvFinance.DefaultCellStyle.BackColor = FMColors.SecondaryBg;
            dgvFinance.DefaultCellStyle.ForeColor = Color.White;
            dgvFinance.ColumnHeadersDefaultCellStyle.BackColor = FMColors.SidebarBg;
            dgvFinance.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gold;
            dgvFinance.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            this.Controls.Add(dgvFinance);
        }

        private void LoadCurrentSeasonData()
        {
            var data = _db.LeagueTable.Select(t => new {
                Takım = t.Name,
                Bütçe = $"{t.Budget:N0} €",
                SezonBaşı = $"{t.InitialBudget:N0} €",
                Harcanan = $"{t.CurrentSeasonSpent:N0} €",
                Kazanılan = $"{t.CurrentSeasonEarned:N0} €",
                Net = $"{(t.CurrentSeasonEarned - t.CurrentSeasonSpent):N0} €"
            }).OrderByDescending(x => x.Bütçe).ToList();

            dgvFinance.DataSource = data;

            // Highlight net positive/negative
            dgvFinance.DataBindingComplete += (s, e) => {
                foreach (DataGridViewRow row in dgvFinance.Rows) {
                    var netStr = row.Cells["Net"].Value.ToString()?.Replace(".", "").Replace("€", "").Trim();
                    if (long.TryParse(netStr, out long net)) {
                        row.Cells["Net"].Style.ForeColor = net >= 0 ? Color.LimeGreen : Color.Salmon;
                    }
                }
            };
        }
    }
}
