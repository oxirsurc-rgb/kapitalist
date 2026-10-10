using UnityEngine;
using System.Collections.Generic;
using DemocracySim.Engine.Data;

namespace DemocracySim.Engine.UI
{
    /// <summary>
    /// Faz 16: Mod Settings Panel.
    /// Modların listelendiği ve açılıp kapatılabildiği arayüz.
    /// </summary>
    public class ModSettingsPanel : MonoBehaviour
    {
        public GameObject panelInstance;
        public Transform contentParent;
        public GameObject modEntryPrefab;

        private void Start()
        {
            RefreshModList();
        }

        public void RefreshModList()
        {
            if (contentParent == null) return;

            // Temizle
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }

            // Modları listele
            foreach (var mod in ModManager.LoadedMods)
            {
                var entry = Instantiate(modEntryPrefab, contentParent);
                
                // UI Elementlerini bul (basit isimle)
                var nameTxt = entry.transform.Find("ModName")?.GetComponent<TMPro.TextMeshProUGUI>();
                var toggle = entry.transform.Find("Toggle")?.GetComponent<UnityEngine.UI.Toggle>();

                if (nameTxt != null) nameTxt.text = mod.Name;
                if (toggle != null)
                {
                    toggle.isOn = mod.Enabled;
                    toggle.onValueChanged.AddListener((val) => {
                        ModManager.SetModEnabled(mod.Id, val);
                        Debug.Log($"[Mod] {mod.Name} durumu değiştirildi: {val}");
                    });
                }
            }
        }

        public void TogglePanel(bool show)
        {
            if (panelInstance != null) panelInstance.SetActive(show);
        }
    }
}
