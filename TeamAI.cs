using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoneyballGame
{
    // --- Analiz ve Teklif Modelleri ---
    public class TeamAnalysis
    {
        public string TeamName { get; set; } = "";
        public Dictionary<string, double> PositionAverages { get; set; } = new();
        public Dictionary<string, int> PositionCounts { get; set; } = new();
        public string WeakestPosition { get; set; } = "";
        public string StrongestPosition { get; set; } = "";
        public List<Player> SurplusPlayers { get; set; } = new();
        public List<string> NeedPositions { get; set; } = new();
        public string AnalysisReport { get; set; } = "";
    }

    public class TransferProposal
    {
        public Team OfferingTeam { get; set; } = null!;
        public Team TargetTeam { get; set; } = null!;
        public Player PlayerWanted { get; set; } = null!;
        public int MoneyOffered { get; set; }
        public string Reasoning { get; set; } = "";
    }

    public class TransferResponse
    {
        public bool Accepted { get; set; }
        public int CounterOffer { get; set; }
        public int FinalPrice { get; set; }
        public string Reasoning { get; set; } = "";
    }

    // --- AI Menajer Beyni ---
    public class TeamAI
    {
        private static readonly Random _rnd = new Random();
        private readonly Team _team;
        private readonly GameDatabase _db;

        // Minimum rahat kadro sayıları
        private static readonly Dictionary<string, int> IdealCounts = new()
        {
            { "KL", 2 }, { "DF", 5 }, { "OS", 6 }, { "FV", 4 }
        };

        public TeamAI(Team team, GameDatabase db)
        {
            _team = team;
            _db = db;
        }

        public static string TranslatePosition(string pos) => pos switch
        {
            "KL" => "Kaleci",
            "DF" => "Defans",
            "OS" => "Orta Saha",
            "FV" => "Forvet",
            _ => pos
        };

        public static double GetOverallRating(Player p)
        {
            return (p.Passing + p.Physical + p.Finishing + p.Heading + p.Technique + p.Stamina) / 6.0;
        }

        // Üstel piyasa değeri: 60→~3M, 70→~8M, 80→~20M, 90→~50M
        public static int CalculateMarketValue(Player p)
        {
            double overall = GetOverallRating(p);
            double baseValue = 10135.0 * Math.Exp(0.0948 * overall);
            double ageFactor = p.Age < 24 ? 1.4 : (p.Age < 28 ? 1.2 : (p.Age < 31 ? 1.0 : (p.Age < 33 ? 0.7 : 0.4)));
            double potentialFactor = p.Potential > 85 ? 1.3 : (p.Potential > 70 ? 1.1 : 1.0);
            return Math.Max(200000, (int)(baseValue * ageFactor * potentialFactor));
        }

        // --- TAM TAKIM ANALİZİ ---
        public TeamAnalysis Analyze()
        {
            var analysis = new TeamAnalysis { TeamName = _team.Name };

            foreach (var pos in new[] { "KL", "DF", "OS", "FV" })
            {
                var players = _team.Roster.Where(p => p.Position == pos).ToList();
                analysis.PositionCounts[pos] = players.Count;
                analysis.PositionAverages[pos] = players.Count > 0
                    ? players.Average(p => GetOverallRating(p))
                    : 0;

                int ideal = IdealCounts.GetValueOrDefault(pos, 2);

                // İhtiyaç tespiti 1: Yeterli oyuncu yok
                if (players.Count < ideal)
                    analysis.NeedPositions.Add(pos);

                // İhtiyaç tespiti 2: Kalite lig ortalamasının altında
                if (players.Count > 0 && !analysis.NeedPositions.Contains(pos))
                {
                    var leaguePlayers = _db.AllPlayers.Values.Where(p => p.Position == pos).ToList();
                    if (leaguePlayers.Count > 0)
                    {
                        double leagueAvg = leaguePlayers.Average(p => GetOverallRating(p));
                        if (analysis.PositionAverages[pos] < leagueAvg * 0.85)
                            analysis.NeedPositions.Add(pos);
                    }
                }

                // Fazlalık tespiti: İdealden fazla oyuncu varsa en zayıfı satılık
                if (players.Count > ideal)
                {
                    var weakest = players.OrderBy(p => GetOverallRating(p)).First();
                    analysis.SurplusPlayers.Add(weakest);
                }
            }

            var withPlayers = analysis.PositionAverages
                .Where(x => analysis.PositionCounts[x.Key] > 0).ToList();

            if (withPlayers.Count > 0)
            {
                analysis.WeakestPosition = withPlayers.OrderBy(x => x.Value).First().Key;
                analysis.StrongestPosition = withPlayers.OrderByDescending(x => x.Value).First().Key;
            }

            analysis.AnalysisReport = BuildReport(analysis);
            return analysis;
        }

        private string BuildReport(TeamAnalysis a)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"💰 Bütçe: {_team.Budget:N0} €  |  👥 Kadro: {_team.Roster.Count}");
            foreach (var pos in new[] { "KL", "DF", "OS", "FV" })
            {
                string icon = a.NeedPositions.Contains(pos) ? "🔴" : "✅";
                sb.AppendLine($"  {icon} {TranslatePosition(pos)}: {a.PositionCounts.GetValueOrDefault(pos, 0)} oyuncu (Ort: {a.PositionAverages.GetValueOrDefault(pos, 0):F0})");
            }
            if (a.NeedPositions.Count > 0)
                sb.AppendLine($"📋 İhtiyaç: {string.Join(", ", a.NeedPositions.Select(TranslatePosition))}");
            if (a.SurplusPlayers.Count > 0)
                sb.AppendLine($"🏷️ Satılık: {string.Join(", ", a.SurplusPlayers.Select(p => p.Name))}");
            return sb.ToString();
        }

        // --- TAM AI İSTİHBARAT RAPORU ---
        public string GenerateTeamIntel()
        {
            var analysis = Analyze();
            var sb = new StringBuilder();

            // 1. Takım Gücü & Lig Hedefi
            double teamAvg = _team.Roster.Count > 0
                ? _team.Roster.Average(p => GetOverallRating(p)) : 0;

            var leagueAvgs = _db.LeagueTable
                .Select(t => new { t.Name, Avg = t.Roster.Count > 0 ? t.Roster.Average(p => GetOverallRating(p)) : 0 })
                .OrderByDescending(x => x.Avg).ToList();
            int teamRank = leagueAvgs.FindIndex(x => x.Name == _team.Name) + 1;

            string seasonGoal;
            string goalIcon;
            if (teamRank <= 3) { seasonGoal = "🏆 Şampiyonluk"; goalIcon = "🥇"; }
            else if (teamRank <= 6) { seasonGoal = "🎯 Avrupa kupası"; goalIcon = "🥈"; }
            else if (teamRank <= 12) { seasonGoal = "📊 Üst sıralara tırmanmak"; goalIcon = "📈"; }
            else { seasonGoal = "⚠️ Ligde kalmak"; goalIcon = "🛡️"; }

            sb.AppendLine($"═══ {goalIcon} AI MENAJER HEDEFİ ═══");
            sb.AppendLine($"  {seasonGoal}");
            sb.AppendLine($"  Kadro Gücü: {teamAvg:F1} (Lig sırası: {teamRank}/18)");
            sb.AppendLine();

            // 2. Vazgeçilemez Oyuncular (top 3 overall)
            var topPlayers = _team.Roster
                .OrderByDescending(p => GetOverallRating(p))
                .Take(3).ToList();

            sb.AppendLine("═══ ⭐ VAZGEÇİLEMEZ OYUNCULAR ═══");
            foreach (var p in topPlayers)
            {
                double r = GetOverallRating(p);
                int mv = CalculateMarketValue(p);
                sb.AppendLine($"  ⭐ {p.Name} ({TranslatePosition(p.Position)})");
                sb.AppendLine($"     Güç: {r:F0} | Yaş: {p.Age} | Değer: {mv:N0}€");
            }
            sb.AppendLine();

            // 3. Pozisyon Analizi — Eksikler
            sb.AppendLine("═══ 📋 POZİSYON ANALİZİ ═══");
            foreach (var pos in new[] { "KL", "DF", "OS", "FV" })
            {
                int count = analysis.PositionCounts.GetValueOrDefault(pos, 0);
                double avg = analysis.PositionAverages.GetValueOrDefault(pos, 0);
                int ideal = IdealCounts.GetValueOrDefault(pos, 2);
                string status = count < ideal ? "🔴 EKSİK" : (count > ideal ? "🟢 FAZLA" : "🟡 YETERL İ");

                var leaguePos = _db.AllPlayers.Values.Where(p => p.Position == pos).ToList();
                double leagueAvg = leaguePos.Count > 0 ? leaguePos.Average(p => GetOverallRating(p)) : 0;
                string quality = avg >= leagueAvg * 1.1 ? "↑ Lig üstü" : (avg >= leagueAvg * 0.9 ? "→ Ortalama" : "↓ Lig altı");

                sb.AppendLine($"  {TranslatePosition(pos)}: {count} kişi ({status}) — Ort: {avg:F0} ({quality})");
            }
            sb.AppendLine();

            // 4. Yaş Profili
            double avgAge = _team.Roster.Count > 0 ? _team.Roster.Average(p => p.Age) : 0;
            int youngCount = _team.Roster.Count(p => p.Age < 23);
            int veteranCount = _team.Roster.Count(p => p.Age >= 30);
            string ageProfile = avgAge < 24 ? "🌱 Genç ve gelişen" : (avgAge < 27 ? "💪 Prime dönem" : (avgAge < 30 ? "⚖️ Dengeli" : "👴 Yaşlanıyor"));

            sb.AppendLine("═══ 📊 KADRO PROFİLİ ═══");
            sb.AppendLine($"  Yaş Ort: {avgAge:F1} — {ageProfile}");
            sb.AppendLine($"  Genç (23↓): {youngCount} | Veteran (30↑): {veteranCount}");
            sb.AppendLine($"  Toplam Kadro: {_team.Roster.Count} oyuncu");
            sb.AppendLine();

            // 5. Bütçe Değerlendirmesi
            double budgetRatio = _team.InitialBudget > 0 ? (double)_team.Budget / _team.InitialBudget : 0;
            string budgetStatus = budgetRatio > 0.7 ? "💰 Güçlü" : (budgetRatio > 0.3 ? "💵 Orta" : "⚠️ Kısıtlı");
            sb.AppendLine("═══ 💰 BÜTÇE DURUMU ═══");
            sb.AppendLine($"  Kalan: {_team.Budget:N0}€ ({budgetStatus})");
            sb.AppendLine($"  Harcanan: {_team.CurrentSeasonSpent:N0}€ | Kazanılan: {_team.CurrentSeasonEarned:N0}€");
            sb.AppendLine();

            // 6. AI Stratejisi
            sb.AppendLine("═══ 🧠 AI STRATEJİSİ ═══");
            if (analysis.NeedPositions.Count > 0)
                sb.AppendLine($"  📌 Öncelik: {string.Join(", ", analysis.NeedPositions.Select(TranslatePosition))} takviyesi");
            else
                sb.AppendLine("  ✅ Kadro dengeli, kalite yükseltme aranacak");

            if (analysis.SurplusPlayers.Count > 0)
                sb.AppendLine($"  🏷️ Satılık: {string.Join(", ", analysis.SurplusPlayers.Select(p => $"{p.Name} ({GetOverallRating(p):F0})"))}");

            if (budgetRatio > 0.5 && analysis.NeedPositions.Count > 0)
                sb.AppendLine("  💡 Yeterli bütçe var, agresif transfer planlanıyor");
            else if (budgetRatio < 0.3)
                sb.AppendLine("  💡 Bütçe kısıtlı, serbest oyuncu piyasası taranacak");

            return sb.ToString();
        }

        // --- TEKLİF ÜRETME: Herhangi bir oyuncuya teklif verilebilir ---
        public TransferProposal? TryGenerateProposal()
        {
            var analysis = Analyze();
            if (analysis.NeedPositions.Count == 0)
            {
                if (!string.IsNullOrEmpty(analysis.WeakestPosition))
                    analysis.NeedPositions.Add(analysis.WeakestPosition);
                else
                    return null;
            }

            var shuffledNeeds = analysis.NeedPositions.OrderBy(_ => _rnd.Next()).ToList();

            foreach (string needPos in shuffledNeeds)
            {
                // Ligteki tüm oyuncuları bu mevkide tara (en iyiler dahil)
                var allCandidates = _db.AllPlayers.Values
                    .Where(p => p.Position == needPos && !_team.Roster.Contains(p))
                    .OrderByDescending(p => GetOverallRating(p))
                    .ToList();

                if (allCandidates.Count == 0) continue;

                // Ağırlıklı seçim: %40 en iyiler (top 25%), %60 orta-üstü
                int topTier = Math.Max(1, allCandidates.Count / 4);
                Player target;
                if (_rnd.Next(100) < 40)
                    target = allCandidates[_rnd.Next(Math.Min(topTier, allCandidates.Count))]; // Yıldız oyuncu
                else
                    target = allCandidates[_rnd.Next(Math.Min(topTier * 3, allCandidates.Count))]; // Orta-üst

                // Bizim ortalamamızdan düşükse atla
                double ourAvg = analysis.PositionAverages.GetValueOrDefault(needPos, 0);
                if (ourAvg > 0 && GetOverallRating(target) < ourAvg * 0.8) continue;

                int marketValue = CalculateMarketValue(target);
                string targetTeamName = _db.FindTeam(target.Id);
                if (targetTeamName == "Serbest" || !_db.AllTeams.ContainsKey(targetTeamName)) continue;
                var targetTeam = _db.AllTeams[targetTeamName];
                if (targetTeam.Name == _team.Name) continue;

                // Yıldız oyuncuya daha yüksek teklif ver
                double offerMultiplier = GetOverallRating(target) >= 70 ? (0.95 + _rnd.NextDouble() * 0.2) : (0.85 + _rnd.NextDouble() * 0.15);
                int offerPrice = (int)(marketValue * offerMultiplier);

                if (_team.Budget < offerPrice) continue;

                return new TransferProposal
                {
                    OfferingTeam = _team,
                    TargetTeam = targetTeam,
                    PlayerWanted = target,
                    MoneyOffered = offerPrice,
                    Reasoning = $"{TranslatePosition(needPos)} mevkiinde takviye aranıyor. " +
                                $"Mevcut ort: {ourAvg:F0}, " +
                                $"hedef: {target.Name} (Güç: {GetOverallRating(target):F0}, Yaş: {target.Age}, Değer: {marketValue:N0}€)"
                };
            }
            return null;
        }

        // --- GELEN TEKLİFİ DEĞERLENDİRME (Takım AI kendi karar verir) ---
        public TransferResponse EvaluateIncomingOffer(TransferProposal proposal)
        {
            var player = proposal.PlayerWanted;
            int marketValue = CalculateMarketValue(player);
            int posCount = _team.Roster.Count(p => p.Position == player.Position);
            int idealCount = IdealCounts.GetValueOrDefault(player.Position, 2);
            double playerRating = GetOverallRating(player);

            // Mevkideki sıralama: oyuncu takımın en iyisi mi?
            var posPlayers = _team.Roster.Where(p => p.Position == player.Position)
                .OrderByDescending(p => GetOverallRating(p)).ToList();
            int rankInPos = posPlayers.FindIndex(p => p.Id == player.Id); // 0 = en iyi

            // === MUTLAK RED: Satarsak minimum kadronun altına düşeriz ===
            if (posCount <= idealCount - 1)
            {
                return new TransferResponse
                {
                    Accepted = false, CounterOffer = 0,
                    Reasoning = $"{TranslatePosition(player.Position)} mevkiinde sadece {posCount} oyuncumuz var. Kesinlikle satmıyoruz."
                };
            }

            // === YILDIZ OYUNCU: Mevkinin en iyisi veya 2.si → çok yüksek prim ===
            if (rankInPos <= 1 && posCount <= idealCount)
            {
                double starPremium = 2.0 + (playerRating / 100.0); // 80 güç → 2.8x prim
                int starPrice = (int)(marketValue * starPremium);
                if (proposal.MoneyOffered >= starPrice)
                {
                    return new TransferResponse
                    {
                        Accepted = true, FinalPrice = proposal.MoneyOffered,
                        Reasoning = $"Yıldız oyuncumuz ama {proposal.MoneyOffered:N0} € çok iyi bir teklif. Kabul!"
                    };
                }
                return new TransferResponse
                {
                    Accepted = false, CounterOffer = starPrice,
                    Reasoning = $"{player.Name} yıldız oyuncumuz! En az {starPrice:N0} € istiyoruz (piyasa: {marketValue:N0} €)."
                };
            }

            // === KRİTİK OYUNCU: Kadro sınırında → yüksek prim ===
            if (posCount <= idealCount)
            {
                double keyPremium = 1.4 + (playerRating / 200.0);
                int keyPrice = (int)(marketValue * keyPremium);
                if (proposal.MoneyOffered >= keyPrice)
                {
                    return new TransferResponse
                    {
                        Accepted = true, FinalPrice = proposal.MoneyOffered,
                        Reasoning = $"Kadroyu zayıflatır ama {proposal.MoneyOffered:N0} € iyi fiyat. Kabul."
                    };
                }
                return new TransferResponse
                {
                    Accepted = false, CounterOffer = keyPrice,
                    Reasoning = $"Kadromuz bu mevkide sınırda. {player.Name} için en az {keyPrice:N0} € gerekli."
                };
            }

            // === FAZLALIK OYUNCU: Piyasa değerine yakın teklifleri kabul et ===
            if (proposal.MoneyOffered >= marketValue * 0.85)
            {
                return new TransferResponse
                {
                    Accepted = true, FinalPrice = proposal.MoneyOffered,
                    Reasoning = $"Teklif kabul! {player.Name} fazlalık, {proposal.MoneyOffered:N0} € uygun fiyat."
                };
            }

            // Teklif düşük
            int counter = (int)(marketValue * 0.95);
            return new TransferResponse
            {
                Accepted = false, CounterOffer = counter,
                Reasoning = $"Teklif düşük. {player.Name} en az {counter:N0} € eder."
            };
        }

        // --- KARŞI TEKLİFE YANIT ---
        public bool WillAcceptCounter(TransferProposal original, int counterPrice)
        {
            if (_team.Budget < counterPrice) return false;
            int marketValue = CalculateMarketValue(original.PlayerWanted);
            double rating = GetOverallRating(original.PlayerWanted);
            int maxWilling = rating >= 70 ? (int)(marketValue * 1.8) : (int)(marketValue * 1.3);
            return counterPrice <= maxWilling && _rnd.Next(100) < 60;
        }

        // --- SERBEST OYUNCU TRANSFER ---
        public (Player? player, int price, string reason)? TrySignFreeAgent()
        {
            if (_db.FreeAgents.Count == 0) return null;

            var analysis = Analyze();
            var needPositions = analysis.NeedPositions.Count > 0
                ? analysis.NeedPositions
                : new List<string> { analysis.WeakestPosition };

            foreach (var needPos in needPositions.OrderBy(_ => _rnd.Next()))
            {
                if (string.IsNullOrEmpty(needPos)) continue;

                var candidates = _db.FreeAgents
                    .Where(p => p.Position == needPos)
                    .OrderByDescending(p => GetOverallRating(p))
                    .ToList();

                if (candidates.Count == 0) continue;

                // En iyi %50'lik dilimden seç
                var target = candidates[_rnd.Next(Math.Min(candidates.Count, Math.Max(1, candidates.Count / 2)))];
                double targetRating = GetOverallRating(target);
                double ourAvg = analysis.PositionAverages.GetValueOrDefault(needPos, 0);

                // Ortalamamızı yükseltecekse veya sayı eksikse al
                if (targetRating < ourAvg * 0.7 && analysis.PositionCounts.GetValueOrDefault(needPos, 0) >= IdealCounts.GetValueOrDefault(needPos, 2))
                    continue;

                int price = CalculateMarketValue(target);
                if (_team.Budget < price) continue;

                string reason = $"{TranslatePosition(needPos)} takviyesi — " +
                               $"{target.Name} (Güç: {targetRating:F0}, Yaş: {target.Age}, Fiyat: {price:N0}€)";

                return (target, price, reason);
            }
            return null;
        }
    }
}
