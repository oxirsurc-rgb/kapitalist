using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Ses Kaynakları")]
    public AudioClip backgroundMusic;
    public AudioClip clickSound;
    public AudioClip notificationSound;
    public AudioClip turnEndDing;

    [Header("Kriz Sesleri")]
    public AudioClip crisisWarningSound;
    public AudioClip crisisEscalationSound;
    public AudioClip crisisCollapseSound;

    // Kriz sesleri için ayrı bir kaynak (normal SFX'leri kesmez)
    [Header("Kriz Ses Süreleri (saniye)")]
    public float crisisWarningDuration = 2f;
    public float crisisEscalationDuration = 2.5f;
    public float crisisCollapseDuration = 3f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource crisisSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (backgroundMusic == null)
            backgroundMusic = Resources.Load<AudioClip>("Audio/background_music");
        if (clickSound == null)
            clickSound = Resources.Load<AudioClip>("Audio/click") ?? Resources.Load<AudioClip>("Audio/switch1");
        if (notificationSound == null)
            notificationSound = Resources.Load<AudioClip>("Sounds/Security_Notification") ?? Resources.Load<AudioClip>("Audio/notification") ?? Resources.Load<AudioClip>("Audio/switch15");
        if (turnEndDing == null)
            turnEndDing = Resources.Load<AudioClip>("Sounds/menlova-zil-sesi-433221") ?? Resources.Load<AudioClip>("Audio/turn_end") ?? Resources.Load<AudioClip>("Audio/switch33");
        if (crisisWarningSound == null)
            crisisWarningSound = Resources.Load<AudioClip>("Sounds/fronbondi_skegs-sfx-facility-failure-alarm-reverb-sound-effect-seamless-loop-478466") ?? Resources.Load<AudioClip>("Audio/crisis_warning") ?? Resources.Load<AudioClip>("Audio/switch20");
        if (crisisEscalationSound == null)
            crisisEscalationSound = Resources.Load<AudioClip>("Audio/crisis_escalation") ?? Resources.Load<AudioClip>("Audio/switch25");
        if (crisisCollapseSound == null)
            crisisCollapseSound = Resources.Load<AudioClip>("Audio/crisis_collapse") ?? Resources.Load<AudioClip>("Audio/switch30");

        // Kaç clip yüklendi logla
        int loaded = (backgroundMusic != null ? 1 : 0) + (clickSound != null ? 1 : 0)
                   + (notificationSound != null ? 1 : 0) + (turnEndDing != null ? 1 : 0);
        Debug.Log($"[AudioManager] Yüklenen ses: {loaded}/4 (banner, click, notif, turn_end)");

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        crisisSource = gameObject.AddComponent<AudioSource>();
        crisisSource.playOnAwake = false;

        musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        sfxSource.volume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        crisisSource.volume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void SetMusicVolume(float value)
    {
        musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSfxVolume(float value)
    {
        sfxSource.volume = value;
        crisisSource.volume = value;
        PlayerPrefs.SetFloat("SfxVolume", value);
    }

    // Normal sesler
    public void PlayClick() => PlayOneShot(clickSound);
    public void PlayNotification() => PlayOneShot(notificationSound);
    public void PlayTurnEndDing() => PlayOneShot(turnEndDing);

    // Kriz sesleri (süre sınırlı)
    public void PlayCrisisWarning() => PlayCrisisSound(crisisWarningSound, crisisWarningDuration);
    public void PlayCrisisEscalation() => PlayCrisisSound(crisisEscalationSound, crisisEscalationDuration);
    public void PlayCrisisCollapse() => PlayCrisisSound(crisisCollapseSound, crisisCollapseDuration);

    private void PlayCrisisSound(AudioClip clip, float maxDuration)
    {
        if (clip == null || crisisSource == null) return;

        // Önceki kriz sesini durdur
        StopAllCoroutines();
        crisisSource.Stop();

        crisisSource.clip = clip;
        crisisSource.Play();

        // Belirlenen süre sonra otomatik durdur
        StartCoroutine(StopCrisisSoundAfter(maxDuration));
    }

    private IEnumerator StopCrisisSoundAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (crisisSource != null && crisisSource.isPlaying)
        {
            // Fade-out (opsiyonel yumuşak geçiş)
            float fadeTime = 0.3f;
            float startVol = crisisSource.volume;
            float t = 0f;
            while (t < fadeTime && crisisSource.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                crisisSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }
            crisisSource.Stop();
            crisisSource.volume = startVol;   // Bir sonraki ses için geri yükle
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }
}