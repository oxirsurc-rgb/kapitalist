using System;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Yerel LLM (LLMUnity) için ince sarmalayıcı.
    /// R-FIX: (1) hazır olma = modelin gerçekten başlamış olması, (2) tek seferlik tamamlanma geri çağrısı
    /// (eski kod akış parçalarıyla onComplete'i defalarca çağırıyordu), (3) hata/zaman aşımında ham metne dönüş,
    /// (4) sohbet geçmişi büyümesin diye addToHistory=false.
    /// </summary>
    public static class LLMClient
    {
        const int TimeoutMs = 30000;   // 15s → 30s (mobil CPU'da yavaş)
        static LLMAgent _agent;

        public enum LLMState { NoAgent, Loading, Ready, Failed }

        private static bool _initialized = false;

public static void Initialize()
{
    // EK-FIX: LLM sadece bir kez başlatılsın (her ülke seçiminde tekrar başlatma = bellek sızıntısı + çökme)
    if (_initialized)
    {
        Debug.Log($"[LLM] Zaten başlatıldı, atlanıyor. Durum: {State}");
        return;
    }
    
    _agent = UnityEngine.Object.FindFirstObjectByType<LLMAgent>(FindObjectsInactive.Include);
    _initialized = true;
    Debug.Log($"[LLM] Durum: {State}");
}

        public static LLMState State
        {
            get
            {
                if (_agent == null) return LLMState.NoAgent;
                var llm = _agent.llm;
                if (llm == null) return LLMState.NoAgent;
                if (llm.failed) return LLMState.Failed;
                return llm.started ? LLMState.Ready : LLMState.Loading;
            }
        }

        public static bool IsReady => State == LLMState.Ready;

        public static async void EmbellishText(string rawText, Action<string> onComplete)
        {
            if (!IsReady) { onComplete?.Invoke(rawText); return; }

            string prompt = "Aşağıdaki metni daha akıcı ve doğal hale getir. " +
                            "Anlamı değiştirme, yeni bilgi ekleme, tek cümleyle yaz. Türkçe yaz.\n\n" +
                            $"Metin: {rawText}\n\nSüslenmiş metin:";
            onComplete?.Invoke(await Ask(prompt) ?? rawText);
        }

        public static async void ExplainDecision(string decisionContext, Action<string> onComplete)
        {
            if (!IsReady) { onComplete?.Invoke(decisionContext); return; }

            string prompt = "Bir siyasi simülasyon oyununda AI'ın verdiği kararı " +
                            "kısa ve doğal bir dille açıkla. Türkçe yaz.\n\n" +
                            $"Bağlam: {decisionContext}\n\nAçıklama:";
            onComplete?.Invoke(await Ask(prompt) ?? decisionContext);
        }

        public static void TestLLM()
        {
            Debug.Log($"[LLM Test] Durum: {State}");
            if (!IsReady) return;
            EmbellishText("Bu bir test metnidir.", r => Debug.Log($"[LLM Test] Yanıt: {r}"));
        }

        // Başarılıysa temizlenmiş yanıt, aksi halde null (çağıran ham metne döner).
        static async Task<string> Ask(string prompt)
        {
            try
            {
                var chat = _agent.Chat(prompt, null, null, false);   // callback=null: akış parçası yok, geçmiş tutulmaz
                var done = await Task.WhenAny(chat, Task.Delay(TimeoutMs));
                if (done != chat) { Debug.LogWarning("[LLM] Zaman aşımı, ham metin kullanılıyor."); return null; }
                string r = chat.Result?.Trim();
                return string.IsNullOrEmpty(r) ? null : r;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLM] Hata: {ex.Message}");
                return null;
            }
        }
    }
}
