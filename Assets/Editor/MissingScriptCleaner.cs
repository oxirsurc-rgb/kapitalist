#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemocracySim.EditorTools
{
    [InitializeOnLoad]
    public static class MissingScriptCleaner
    {
        static MissingScriptCleaner()
        {
            EditorApplication.delayCall += RunAutoCheck;
        }

        private static void RunAutoCheck()
        {
            CleanLoadedScenes(false);
        }

        [MenuItem("DemocracySim/Kayıp Scriptleri Temizle (Aktif Sahne)")]
        public static void CleanActiveSceneMenu()
        {
            var scene = SceneManager.GetActiveScene();
            int removed = CleanScene(scene);
            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[MissingScriptCleaner] '{scene.name}' sahnesinde {removed} adet kayıp script temizlendi ve kaydedildi.");
            }
            else
            {
                Debug.Log($"[MissingScriptCleaner] '{scene.name}' sahnesinde kayıp script bulunamadı.");
            }
        }

        [MenuItem("DemocracySim/Tüm Sahnelerdeki Kayıp Scriptleri Tara ve Temizle")]
        public static void CleanAllScenesInProject()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            int grandTotal = 0;

            string currentActivePath = SceneManager.GetActiveScene().path;

            foreach (var guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity")) continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int removed = CleanScene(scene);
                if (removed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[MissingScriptCleaner] '{path}' sahnesinde {removed} adet kayıp script temizlendi ve kaydedildi.");
                    grandTotal += removed;
                }
            }

            if (!string.IsNullOrEmpty(currentActivePath) && File.Exists(currentActivePath))
            {
                EditorSceneManager.OpenScene(currentActivePath, OpenSceneMode.Single);
            }

            Debug.Log($"[MissingScriptCleaner] Proje genelinde toplam {grandTotal} kayıp script temizlendi.");
        }

        public static void CleanLoadedScenes(bool saveIfModified)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                int removed = CleanScene(scene);
                if (removed > 0)
                {
                    Debug.Log($"[MissingScriptCleaner] '{scene.name}' sahnesinden {removed} adet eksik/silinmiş script referansı kaldırıldı.");
                    if (saveIfModified && !EditorApplication.isPlaying)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                }
            }
        }

        private static int CleanScene(Scene scene)
        {
            int total = 0;
            var rootObjects = scene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                total += CleanRecursive(go);
            }
            return total;
        }

        private static int CleanRecursive(GameObject go)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                Debug.LogWarning($"[MissingScriptCleaner] '{go.name}' GameObject üzerindeki {count} kayıp MonoBehaviour kaldırıldı.");
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                count += CleanRecursive(go.transform.GetChild(i).gameObject);
            }

            return count;
        }
    }
}
#endif
