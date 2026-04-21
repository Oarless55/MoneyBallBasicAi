using System;
using System.Collections.Generic;
using System.Linq;

namespace MoneyballGame
{
    public class SeasonFinance
    {
        public int SeasonNumber { get; set; }
        public int StartBudget { get; set; }
        public int Spent { get; set; }
        public int Earned { get; set; }
        public int EndBudget => StartBudget - Spent + Earned;
    }

    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; } // KL, DF, OS, FV
        public int Passing { get; set; }
        public int Physical { get; set; }
        public int Wage { get; set; }
        public int Value { get; set; }
        public int Age { get; set; }
        public int Potential { get; set; }
        public int Goals { get; set; } = 0;
        public int Assists { get; set; } = 0;
        public int SeasonPassesAttempted { get; set; } = 0;
        public int SeasonPassesCompleted { get; set; } = 0;
        public int SeasonTacklesAttempted { get; set; } = 0;
        public int SeasonTacklesWon { get; set; } = 0;
        public double PassAccuracy => SeasonPassesAttempted > 0 ? (double)SeasonPassesCompleted / SeasonPassesAttempted * 100 : 0;

        // NEW: Advanced Attributes
        public int Finishing { get; set; }
        public int Heading { get; set; }
        public int Technique { get; set; }
        public int Stamina { get; set; }

        // --- FM-Style Attributes ---
        // Technical
        public int Corners { get; set; }
        public int Crossing { get; set; }
        public int Dribbling { get; set; }
        public int FirstTouch { get; set; }
        public int FreeKick { get; set; }
        public int LongShots { get; set; }
        public int Marking { get; set; }
        public int Penalty { get; set; }
        public int Tackling { get; set; }

        // Mental
        public int Aggression { get; set; }
        public int Anticipation { get; set; }
        public int Bravery { get; set; }
        public int Composure { get; set; }
        public int Concentration { get; set; }
        public int Decisions { get; set; }
        public int Determination { get; set; }
        public int Flair { get; set; }
        public int Leadership { get; set; }
        public int OffTheBall { get; set; }
        public int Positioning { get; set; }
        public int Teamwork { get; set; }
        public int Vision { get; set; }
        public int WorkRate { get; set; }

        // Physical
        public int Acceleration { get; set; }
        public int Agility { get; set; }
        public int Balance { get; set; }
        public int Jumping { get; set; }
        public int Pace { get; set; }
        public int Strength { get; set; }

        public void SimulateDevelopment()
        {
            Random rnd = new Random();
            int avgSkill = (Passing + Physical + Finishing + Heading + Technique) / 5;

            if (Age < 28 && avgSkill < Potential)
            {
                Passing += rnd.Next(0, 3);
                Physical += rnd.Next(0, 2);
                Finishing += rnd.Next(0, 3);
                Heading += rnd.Next(0, 2);
                Technique += rnd.Next(0, 3);
                Value += 250000 + (rnd.Next(0, 5) * 50000);
            }
            else if (Age >= 32)
            {
                Physical -= rnd.Next(1, 4);
                Stamina -= rnd.Next(1, 4);
                Finishing -= rnd.Next(0, 2);
                Value -= 150000;
            }
            Age++;
        }
    }

    public class Team
    {
        public string Name { get; set; }
        public int Budget { get; set; }
        public HashSet<Player> Roster { get; set; } = new HashSet<Player>();

        public int Points { get; set; } = 0;
        public int Won { get; set; } = 0;
        public int Drawn { get; set; } = 0;
        public int Lost { get; set; } = 0;
        public int GoalsFor { get; set; } = 0;
        public int GoalsAgainst { get; set; } = 0;
        public int GoalDifference => GoalsFor - GoalsAgainst;

        // Finance Tracking
        public List<SeasonFinance> FinanceHistory { get; set; } = new List<SeasonFinance>();
        public int CurrentSeasonSpent { get; set; } = 0;
        public int CurrentSeasonEarned { get; set; } = 0;
        public int InitialBudget { get; set; }

        public Team(string name, int budget)
        {
            Name = name;
            Budget = budget;
        }

        public void AddPlayer(Player player) { Roster.Add(player); }
        public void RemovePlayer(Player player) { Roster.Remove(player); }

        // --- YENİ: İLK 11 SEÇİM MANTIĞI ---
        public List<Player> GetStartingEleven()
        {
            // Kadrodaki oyuncuları yeteneklerine (Pas + Fizik) göre büyükten küçüğe sırala ve en iyi 11'i sahaya sür
            return Roster.OrderByDescending(p => p.Passing + p.Physical).Take(11).ToList();
        }
    }

    public class GameDatabase
    {
        public Dictionary<int, Player> AllPlayers { get; set; } = new Dictionary<int, Player>();
        public Dictionary<string, Team> AllTeams { get; set; } = new Dictionary<string, Team>();
        public List<Team> LeagueTable { get; set; } = new List<Team>();

        private static readonly string[] FirstNames = {
            "Ahmet", "Mehmet", "Mustafa", "Can", "Ali", "Burak", "Emre", "Arda", "Okan", "Serkan",
            "Hakan", "Yusuf", "Mert", "Onur", "Gökhan", "Selçuk", "Volkan", "Cenk", "Oğuz", "Berat",
            "Enes", "Fatih", "Kerem", "Uğur", "Tolga", "İsmail", "Deniz", "Eren", "Kaan", "Bora"
        };

        private static readonly string[] LastNames = {
            "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Yıldız", "Özdemir", "Arslan", "Doğan", "Kılıç",
            "Aslan", "Bulut", "Çetin", "Korkmaz", "Polat", "Güneş", "Aydın", "Öztürk", "Sarı", "Yıldırım",
            "Aksoy", "Özcan", "Ünal", "Güler", "Yavuz", "Şen", "Akyüz", "Eraslan", "Karakaş", "Toprak"
        };

        public void InitializeData()
        {
            // 18 Takımlı Ligin İsimleri
            string[] teamNames = {
                "Pas Spor", "Güç İdman", "Kuzey Yıldızı", "Merkez City", "Ankara Gücü",
                "İzmir Altın", "Bursa Timsah", "Trabzon Fırtına", "Antalya Akdeniz", "Konya Kartal",
                "Kayseri Zirve", "Samsun Kırmızı", "Adana Güney", "Gaziantep Şahin", "Sivas Yiğido",
                "Eskişehir Es", "Göztepe Yalı", "Kasımpaşa Semt"
            };

            Random rnd = new Random();
            int playerId = 1;

            // --- YENİ: OTOMATİK KADRO ÜRETİMİ ---
            foreach (var tName in teamNames)
            {
                Team newTeam = new Team(tName, rnd.Next(10000000, 30000000)); // Rastgele bütçe
                newTeam.InitialBudget = newTeam.Budget;

                // Her takıma 18 oyuncu (Regen) oluşturuyoruz
                for (int i = 0; i < 18; i++)
                {
                    string randomName = $"{FirstNames[rnd.Next(FirstNames.Length)]} {LastNames[rnd.Next(LastNames.Length)]}";
                    
                    Player p = new Player();
                    p.Id = playerId++;
                    p.Name = randomName;
                    p.Passing = rnd.Next(30, 95);
                    p.Physical = rnd.Next(30, 95);
                    p.Finishing = rnd.Next(20, 90);
                    p.Heading = rnd.Next(20, 90);
                    p.Technique = rnd.Next(30, 90);
                    p.Stamina = rnd.Next(40, 95);

                    // Assign Position
                    if (i == 0) p.Position = "KL";
                    else if (i < 6) p.Position = "DF";
                    else if (i < 13) p.Position = "OS";
                    else p.Position = "FV";

                    // Randomize FM Attributes
                    p.Corners = rnd.Next(1, 20); p.Crossing = rnd.Next(1, 20); p.Dribbling = rnd.Next(1, 20);
                    p.FirstTouch = rnd.Next(1, 20); p.FreeKick = rnd.Next(1, 20); p.LongShots = rnd.Next(1, 20);
                    p.Marking = rnd.Next(1, 20); p.Penalty = rnd.Next(1, 20); p.Tackling = rnd.Next(1, 20);
                    
                    p.Aggression = rnd.Next(1, 20); p.Anticipation = rnd.Next(1, 20); p.Bravery = rnd.Next(1, 20);
                    p.Composure = rnd.Next(1, 20); p.Concentration = rnd.Next(1, 20); p.Decisions = rnd.Next(1, 20);
                    p.Determination = rnd.Next(1, 20); p.Flair = rnd.Next(1, 20); p.Leadership = rnd.Next(1, 20);
                    p.OffTheBall = rnd.Next(1, 20); p.Positioning = rnd.Next(1, 20); p.Teamwork = rnd.Next(1, 20);
                    p.Vision = rnd.Next(1, 20); p.WorkRate = rnd.Next(1, 20);

                    p.Acceleration = rnd.Next(1, 20); p.Agility = rnd.Next(1, 20); p.Balance = rnd.Next(1, 20);
                    p.Jumping = rnd.Next(1, 20); p.Pace = rnd.Next(1, 20); p.Strength = rnd.Next(1, 20);

                    p.Age = rnd.Next(17, 34);
                    p.Wage = rnd.Next(10000, 80000);
                    p.Value = rnd.Next(200000, 5000000);
                    p.Potential = rnd.Next((p.Passing + p.Physical + p.Finishing) / 3, 99);

                    newTeam.AddPlayer(p);
                    AllPlayers.Add(p.Id, p);
                }

                LeagueTable.Add(newTeam);
                AllTeams.Add(newTeam.Name, newTeam);
            }
        }
    }
}