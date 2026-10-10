using UnityEngine;
using UnityEngine.UIElements;

namespace DemocracySim.Engine.UI
{
    /// <summary>
    /// Faz 12: UI Toolkit Root Controller.
    /// Manages the primary UIDocument and coordinates between uGUI (legacy) and Toolkit.
    /// </summary>
    public class UIToolkitManager : MonoBehaviour
    {
        public static UIToolkitManager Instance { get; private set; }

        [Header("Root Settings")]
        public UIDocument rootDocument;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Sayfa geçiş butonlarını bağla
root?.Q<Button>("btn-next-turn")?.RegisterCallback<ClickEvent>(evt =>
{
    var gm = FindAnyObjectByType<GameManager>();
    if (gm != null) gm.NextTurn();
});
        }

        public VisualElement root => rootDocument != null ? rootDocument.rootVisualElement : null;

        public void ShowPage(string pageName)
        {
            if (root == null) return;
            
            // Hide all pages (assuming pages are in a container called "page-container")
            var container = root.Q("page-container");
            if (container == null) return;

            foreach (var child in container.Children())
            {
                child.style.display = DisplayStyle.None;
            }

            // Show requested page
            var page = container.Q(pageName);
            if (page != null)
            {
                page.style.display = DisplayStyle.Flex;
            }
        }
    }
}
