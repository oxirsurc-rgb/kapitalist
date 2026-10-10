using UnityEngine;
using UnityEngine.UIElements;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.UI
{
    /// <summary>
    /// Faz 12: Base Presenter for UI Toolkit.
    /// Links a specific UI page to the EventBus.
    /// </summary>
    public abstract class UIToolkitPresenter : MonoBehaviour
    {
        [Header("Configuration")]
        public string pageIdentifier; // The name of the VisualElement in the UXML
        protected VisualElement rootElement;

        public virtual void Initialize(VisualElement root)
        {
            rootElement = root.Q(pageIdentifier);
            if (rootElement == null)
            {
                Debug.LogError($"[UI Toolkit] Page {pageIdentifier} not found in UXML!");
                return;
            }
            BindEvents();
            UpdateUI();
        }

        protected abstract void BindEvents();
        protected abstract void UpdateUI();

        // Helper to quickly find elements within the page
        protected VisualElement Q(string selector) => rootElement != null ? rootElement.Q(selector) : null;
        protected Label QLabel(string selector) => rootElement != null ? rootElement.Q<Label>(selector) : null;
        protected Button QButton(string selector) => rootElement != null ? rootElement.Q<Button>(selector) : null;
    }
}
