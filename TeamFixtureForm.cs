using System;
using System.Drawing;
using System.Windows.Forms;

namespace MoneyballGame
{
    public class TeamFixtureForm : Form
    {
        public TeamFixtureForm(Team team)
        {
            this.Text = $"📅 {team.Name} - Sezon Fikstürü";
            this.Size = new Size(500, 600);
            this.BackColor = FMColors.PrimaryBg;
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblTitle = new Label 
            { 
                Text = $"{team.Name} Maçları", 
                Dock = DockStyle.Top, 
                Height = 50, 
                ForeColor = Color.Gold, 
                Font = new Font("Segoe UI", 16, FontStyle.Bold), 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            this.Controls.Add(lblTitle);

            ListBox lstMatches = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = FMColors.SecondaryBg,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                ItemHeight = 25
            };

            foreach (var match in team.MatchHistory)
            {
                lstMatches.Items.Add(match);
            }

            // DrawItem event to color code G/B/M (Win/Draw/Loss)
            lstMatches.DrawMode = DrawMode.OwnerDrawFixed;
            lstMatches.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                e.DrawBackground();

                string text = lstMatches.Items[e.Index].ToString()!;
                Color textColor = Color.White;

                if (text.EndsWith("(G)")) textColor = Color.LimeGreen;
                else if (text.EndsWith("(M)")) textColor = Color.Salmon;
                else if (text.EndsWith("(B)")) textColor = Color.Yellow;

                using (Brush brush = new SolidBrush(textColor))
                {
                    e.Graphics.DrawString(text, e.Font, brush, e.Bounds, StringFormat.GenericDefault);
                }
                e.DrawFocusRectangle();
            };

            this.Controls.Add(lstMatches);
            lstMatches.BringToFront();
        }
    }
}
