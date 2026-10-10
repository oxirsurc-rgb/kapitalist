using System;
using System.Collections;
using UnityEngine;

// Panel açılıp kapanması şu ana kadar anlıktı (SetActive true/false). Bu sınıf
// CanvasGroup + Coroutine ile 0.2-0.3sn fade-in/fade-out sağlar. UIManager
// (MonoBehaviour olduğu için) StartCoroutine ile bu metodu kullanır.
public static class UIFader
{
    public static CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    public static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, Action onComplete = null)
    {
        if (cg == null) { onComplete?.Invoke(); yield break; }

        float t = 0f;
        cg.alpha = from;
        cg.interactable = to > 0.5f;
        cg.blocksRaycasts = to > 0.5f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, duration <= 0f ? 1f : t / duration);
            yield return null;
        }

        cg.alpha = to;
        onComplete?.Invoke();
    }
}
