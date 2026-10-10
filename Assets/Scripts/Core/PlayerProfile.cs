namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// EK-22: Oyuncu profil verileri — isim, parti adı, ideoloji.
    /// Save/load ile saklanır.
    /// </summary>
    public static class PlayerProfile
    {
        public static string PlayerName { get; set; } = "Başkan";
        public static string PartyName { get; set; } = "Sizin Partiniz";
        public static string PartySlogan { get; set; } = "";
        public static bool IsCustomParty { get; set; } = false;

        public static void Reset()
        {
            PlayerName = "Başkan";
            PartyName = "Sizin Partiniz";
            PartySlogan = "";
            IsCustomParty = false;
        }
    }
}