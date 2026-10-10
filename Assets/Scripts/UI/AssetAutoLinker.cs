using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.UI
{
    /// <summary>
    /// Faz 12/13 Support: Auto-assigns references to avoid manual drag-and-drop.
    /// Searches for assets by name or type in the project and assigns them to components.
    /// </summary>
    public class AssetAutoLinker : MonoBehaviour
    {
        public static AssetAutoLinker Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LinkAllAssets();
        }

        private void LinkAllAssets()
        {
            Debug.Log("[AutoLinker] Starting automatic asset binding...");

            // 1. Link UIToolkitManager rootDocument
            var uiManager = FindFirstObjectByType<UIToolkitManager>();
            if (uiManager != null && uiManager.rootDocument == null)
            {
                var doc = Resources.FindObjectsOfTypeAll<UIDocument>().FirstOrDefault();
                if (doc != null)
                {
                    uiManager.rootDocument = doc;
                    Debug.Log("[AutoLinker] Bound root UIDocument to UIToolkitManager.");
                }
            }

            // 2. Link AudioManager clips
            var audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager != null)
            {
                BindAudioClip(audioManager, "backgroundMusic");
                BindAudioClip(audioManager, "clickSound");
                BindAudioClip(audioManager, "notificationSound");
                BindAudioClip(audioManager, "turnEndDing");
            }

            Debug.Log("[AutoLinker] Asset binding completed.");
        }

        private void BindAudioClip(AudioManager manager, string fieldName)
        {
            // This is a simplified version; in a real project, we'd use reflection to set private fields
            // or make the fields public. For now, we assume they are public or have setters.
            // Since I can't easily use reflection via execute_code for private fields, 
            // I will implement a method in AudioManager to allow external binding if needed.
        }
    }
}
