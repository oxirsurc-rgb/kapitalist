using UnityEngine;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 3.5: LLM entegrasyonu yapılandırması.
    /// PlayerPrefs veya ScriptableObject ile yönetilir.
    /// </summary>
    public static class LLMConfig
    {
        public static bool IsEnabled => PlayerPrefs.GetInt("LLM_Enabled", 0) == 1;
        public static string ApiKey => PlayerPrefs.GetString("LLM_ApiKey", "");
        public static string ModelName => PlayerPrefs.GetString("LLM_Model", "gpt-4o-mini");
        public static string BaseUrl => PlayerPrefs.GetString("LLM_BaseUrl", "https://api.openai.com/v1");

        public static void Enable(string apiKey, string model = "gpt-4o-mini")
        {
            PlayerPrefs.SetInt("LLM_Enabled", 1);
            PlayerPrefs.SetString("LLM_ApiKey", apiKey);
            PlayerPrefs.SetString("LLM_Model", model);
            PlayerPrefs.Save();
        }

        public static void Disable()
        {
            PlayerPrefs.SetInt("LLM_Enabled", 0);
            PlayerPrefs.Save();
        }
    }
}