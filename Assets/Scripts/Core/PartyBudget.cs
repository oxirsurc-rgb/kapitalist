using System;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Parti fonu yöneticisi. Siyasi sermayeden bağımsız.
    /// Muhalefetteyken aktiftir, iktidardayken de birikir ama pasif kalır.
    /// </summary>
    public class PartyBudget
    {
        // Bütçe
        public float Fund { get; set; } = 100f;
        public float MaxFund { get; set; } = 500f;

        // Tur başına gelir/gider
        public float PassiveIncome { get; set; } = 6f;    // Aidat + küçük bağışlar
        public float PassiveExpense { get; set; } = 4f;   // Ofis, personel, maaşlar
        public float NetIncome => PassiveIncome - PassiveExpense;

        // Bağış kampanyası sabitleri
        public const float DonationLegitCost = 5f;
        public const float DonationAmount = 40f;
        public const int DonationCooldown = 2;   // tur
        private int _donationCooldownLeft = 0;

        // Devlet yardımı
        public const float SubsidyPerSeat = 0.5f;
        public const int MinSeatsForSubsidy = 10;

        public bool CanCollectDonation => _donationCooldownLeft <= 0;

        /// <summary>Her tur çağrılır. Fon otomatik güncellenir.</summary>
        public void ProcessTurn(SimulationEngine e)
        {
            // Fon doğal büyüme (sadece muhalefetteyken tam etki)
            float incomeMult = e.CurrentRole == SimulationEngine.PlayerRole.Opposition ? 1f : 0.5f;
            Fund = Math.Clamp(Fund + NetIncome * incomeMult, 0f, MaxFund);

            // Cooldown
            if (_donationCooldownLeft > 0) _donationCooldownLeft--;

            // Sıfıra düşerse uyarı
            if (Fund <= 0f && e.CurrentRole == SimulationEngine.PlayerRole.Opposition)
            {
                SimLogger.Log("[Parti Bütçesi] Parti fonu tükendi! Kampanya ve operasyonlar sekteye uğrayacak.", SimLogger.LogLevel.Warning);
            }
        }

        /// <summary>Bağış kampanyası başlat.</summary>
        public string CollectDonation(SimulationEngine e)
        {
            if (e.CurrentRole != SimulationEngine.PlayerRole.Opposition)
                return "Bağış kampanyası sadece muhalefetteyken yapılabilir.";

            if (!CanCollectDonation)
                return $"Bağış kampanyası için {_donationCooldownLeft} tur beklemelisin.";

            e.Legitimacy.AdjustLegitimacy(-DonationLegitCost);
            Fund = Math.Min(MaxFund, Fund + DonationAmount);
            _donationCooldownLeft = DonationCooldown;

            return $"Bağış kampanyası: +{DonationAmount:F0} parti fonu, -{DonationLegitCost:F0} meşruiyet. " +
                   $"Yeni fon: {Fund:F0}/{MaxFund:F0}";
        }

        /// <summary>Devlet yardımı al (sandalye başına).</summary>
        public string ApplyStateSubsidy(SimulationEngine e)
        {
            int playerSeats = e.Elections.LastParties
                .Where(p => p.Id == "player")
                .Sum(p => p.Seats);

            if (playerSeats < MinSeatsForSubsidy)
                return $"Devlet yardımı için en az {MinSeatsForSubsidy} sandalye gerekir (şu an: {playerSeats}).";

            float subsidy = playerSeats * SubsidyPerSeat;
            Fund = Math.Min(MaxFund, Fund + subsidy);
            return $"Devlet yardımı alındı: +{subsidy:F0} fon (toplam {playerSeats} sandalye x {SubsidyPerSeat:F1}).";
        }

        /// <summary>Fondan harcama yap. Yetersizse false döner.</summary>
        public bool Spend(float amount)
        {
            if (Fund < amount) return false;
            Fund -= amount;
            return true;
        }

        /// <summary>Rapor (UI için).</summary>
        public string GetStatusText()
        {
            return $"Parti Fonu: {Fund:F0}/{MaxFund:F0}   |   Net: {(NetIncome >= 0 ? "+" : "")}{NetIncome:F0}/tur";
        }
    }
}