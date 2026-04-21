from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI()

# --- 1. SINIFLAR (Yapay Zeka Mantığı) ---
class Player:
    def __init__(self, name, passing, physical, wage, value):
        self.name = name
        self.passing = passing
        self.physical = physical
        self.wage = wage
        self.value = value

class TeamAI:
    def __init__(self, team_name, budget, philosophy_weights):
        self.team_name = team_name
        self.budget = budget
        self.weights = philosophy_weights 

    def evaluate_player(self, player):
        # Yetenek Puanı
        score = (player.passing * self.weights.get('passing', 0)) + \
                (player.physical * self.weights.get('physical', 0))
        # Moneyball Formülü (Fiyat/Performans)
        utility = (score / (player.wage + player.value)) * 100000 
        return utility

    def receive_offer(self, offering_team_name, my_player, their_player, money_offered=0):
        my_player_utility = self.evaluate_player(my_player)
        their_player_utility = self.evaluate_player(their_player)
        
        # Paranın sistemdeki fayda karşılığı
        money_utility = (money_offered / 1000000) * 5  
        total_incoming_utility = their_player_utility + money_utility

        # Karar Ağacı
        if total_incoming_utility >= my_player_utility * 1.05:
            return "ACCEPT", 0, f"{their_player.name} sistemime çok uygun, teklifi kabul ediyorum."
        elif total_incoming_utility >= my_player_utility * 0.70:
            utility_deficit = (my_player_utility * 1.05) - total_incoming_utility
            extra_money_needed = round((utility_deficit / 5) * 1000000)
            return "COUNTER", extra_money_needed, f"Teklif fena değil ama oyuncunun değerini karşılamıyor. {extra_money_needed}€ eklersen anlaşırız."
        else:
            return "REJECT", 0, "Bu teklif Moneyball prensiplerime tamamen aykırı."

# --- 2. AJANLARIN (TAKIMLARIN) VERİTABANI ---
# Python hafızasında takımları ve oyun felsefelerini tutuyoruz.
ai_teams = {
    "Pas Spor": TeamAI("Pas Spor", budget=5000000, philosophy_weights={'passing': 0.8, 'physical': 0.2}),
    "Güç İdman Yurdu": TeamAI("Güç İdman Yurdu", budget=3000000, philosophy_weights={'passing': 0.2, 'physical': 0.8})
}

# --- 3. API MODELLERİ (C#'tan Gelecek Verinin Taslağı) ---
class PlayerModel(BaseModel):
    name: str
    passing: int
    physical: int
    wage: int
    value: int

class TradeOfferModel(BaseModel):
    offering_team: str
    target_team: str
    my_player: PlayerModel
    their_player: PlayerModel
    money_offered: int

# --- 4. API UÇ NOKTASI (Endpoint) ---
@app.post("/evaluate_trade")
def evaluate_trade(offer: TradeOfferModel):
    print(f"\n[GELEN İSTEK] {offer.offering_team}, {offer.target_team} takımına teklif yaptı.")
    
    if offer.target_team not in ai_teams:
        return {"status": "ERROR", "counter_money_needed": 0, "message": "Hedef takım bulunamadı."}
        
    # Teklif yapılan takımı buluyoruz
    target_ai = ai_teams[offer.target_team]

    # C#'tan gelen JSON verisini, bizim değerlendirme yapabileceğimiz Player nesnelerine çeviriyoruz
    my_player_obj = Player(
        offer.my_player.name, offer.my_player.passing, 
        offer.my_player.physical, offer.my_player.wage, offer.my_player.value
    )
    their_player_obj = Player(
        offer.their_player.name, offer.their_player.passing, 
        offer.their_player.physical, offer.their_player.wage, offer.their_player.value
    )

    # Menajere kararı sorduğumuz an
    status, counter_money, message = target_ai.receive_offer(
        offering_team_name=offer.offering_team,
        my_player=my_player_obj,
        their_player=their_player_obj,
        money_offered=offer.money_offered
    )

    # Çıkan sonucu C# motoruna yolluyoruz
    return {
        "status": status,
        "counter_money_needed": counter_money,
        "message": message
    }