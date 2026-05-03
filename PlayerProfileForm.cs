using System;
using System.Drawing;
using System.Windows.Forms;

namespace MoneyballGame
{
    public class PlayerProfileForm : Form
    {
        private Player _player;

        public PlayerProfileForm(Player player)
        {
            _player = player;
            this.Text = $"{player.Name} - Oyuncu Profili";
            this.Size = new Size(1100, 750);
            this.BackColor = FMColors.PrimaryBg;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScroll = true; // Fix for small screens

            InitializeUI();
        }

        private void InitializeUI()
        {
            // 1. Header Banner
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = Color.FromArgb(20, 40, 80) };
            this.Controls.Add(pnlHeader);

            Label lblName = new Label { Text = _player.Name.ToUpper(), Left = 150, Top = 20, AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold) };
            pnlHeader.Controls.Add(lblName);

            Label lblDetails = new Label { Text = $"{_player.Age} Yaşında  |  Değer: {_player.Value:N0} €  |  Maaş: {_player.Wage:N0} €", Left = 155, Top = 70, AutoSize = true, ForeColor = Color.LightGray, Font = new Font("Segoe UI", 12) };
            pnlHeader.Controls.Add(lblDetails);

            // 2. Main Layout
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); // Pitch / General
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); // Technical
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); // Mental
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25)); // Physical
            this.Controls.Add(layout);

            // Pitch Map Placeholder
            Panel pnlPitch = new Panel { Dock = DockStyle.Fill, BackColor = FMColors.SecondaryBg, Margin = new Padding(5) };
            Label lblPitch = new Label { Text = "POSİSYON", Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, ForeColor = FMColors.Accent, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pnlPitch.Controls.Add(lblPitch);
            // Simple Pitch visualization
            Panel pitchDraw = new Panel { Top = 40, Left = 20, Width = 220, Height = 350, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.DarkGreen };
            pnlPitch.Controls.Add(pitchDraw);
            layout.Controls.Add(pnlPitch, 0, 0);

            // Attributes
            layout.Controls.Add(CreateAttributeSection("TEKNİK", new[] {
                ("Bitiricilik", _player.Finishing), ("Kafa Vuruşu", _player.Heading), ("Teknik", _player.Technique),
                ("Pas", _player.Passing), ("Korner", _player.Corners), ("Orta Yapma", _player.Crossing),
                ("Top Sürme", _player.Dribbling), ("İlk Temas", _player.FirstTouch), ("Serbest Vuruş", _player.FreeKick),
                ("Uzaktan Şut", _player.LongShots), ("Markaj", _player.Marking), ("Penaltı", _player.Penalty),
                ("Top Kapma", _player.Tackling)
            }), 1, 0);

            layout.Controls.Add(CreateAttributeSection("ZİHİNSEL", new[] {
                ("Agresiflik", _player.Aggression), ("Antisipasyon", _player.Anticipation), ("Cesaret", _player.Bravery),
                ("Soğukkanlılık", _player.Composure), ("Konsantrasyon", _player.Concentration), ("Karar Verme", _player.Decisions),
                ("Kararlılık", _player.Determination), ("Özel Yetenek", _player.Flair), ("Liderlik", _player.Leadership),
                ("Topsuz Alan", _player.OffTheBall), ("Pozisyon Alma", _player.Positioning), ("Takım Oyunu", _player.Teamwork),
                ("Vizyon", _player.Vision), ("Çalışma Oranı", _player.WorkRate)
            }), 2, 0);

            layout.Controls.Add(CreateAttributeSection("FİZİKSEL", new[] {
                ("Hızlanma", _player.Acceleration), ("Çeviklik", _player.Agility), ("Denge", _player.Balance),
                ("Zıplama", _player.Jumping), ("Hız", _player.Pace), ("Güç", _player.Strength),
                ("Dayanıklılık", _player.Stamina)
            }), 3, 0);
        }

        private Panel CreateAttributeSection(string title, (string Name, int Value)[] attrs)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = FMColors.SecondaryBg, Margin = new Padding(5) };
            Label lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter, ForeColor = FMColors.Accent, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            p.Controls.Add(lblTitle);

            int y = 40;
            foreach (var attr in attrs)
            {
                Label lblName = new Label { Text = attr.Name, Left = 10, Top = y, Width = 140, ForeColor = Color.LightGray, Font = new Font("Segoe UI", 9) };
                Label lblVal = new Label { Text = attr.Value.ToString(), Left = 160, Top = y, Width = 30, TextAlign = ContentAlignment.MiddleRight, ForeColor = GetAttrColor(attr.Value), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
                p.Controls.Add(lblName);
                p.Controls.Add(lblVal);
                y += 25;
            }
            return p;
        }

        private Color GetAttrColor(int val)
        {
            if (val >= 15) return Color.FromArgb(255, 100, 0); // Orange
            if (val >= 10) return Color.FromArgb(0, 200, 100); // Green
            return Color.LightGray;
        }
    }
}
