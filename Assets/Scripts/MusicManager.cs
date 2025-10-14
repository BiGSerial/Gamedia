using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class MusicManager : MonoBehaviour
{
    public static MusicManager I;

    [Header("Playlist")]
    public AudioClip[] tracks;          // arraste suas músicas (use Load Type: Streaming)
    public bool shuffle = false;
    public bool autoPlayOnStart = true;

    [Header("StreamingAssets (opcional)")]
    [Tooltip("Se verdadeiro, usa StreamingAssets em vez de AudioClips")] 
    public bool useStreamingAssets = false;
    [Tooltip("Arquivos dentro de StreamingAssets (ex.: Music/track1.ogg)")]
    public string[] streamingFiles;
    public AudioType streamingAudioType = AudioType.OGGVORBIS;

    [Header("Volumetria")]
    [Range(0f, 1f)] public float volume = 0.3f;
    public float crossfadeTime = 1.5f;  // tempo da transição

    private AudioSource a;    // atual
    private AudioSource b;    // próximo
    private int currentIndex = -1;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Dois AudioSources para crossfade
        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { a, b })
        {
            s.playOnAwake = false;
            s.loop = false;           // sem loop para avançar na playlist
            s.spatialBlend = 0f;      // 2D
            s.volume = 0f;            // começamos mutado
        }

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void Start()
    {
        if (autoPlayOnStart)
        {
            if (!useStreamingAssets && tracks != null && tracks.Length > 0)
                PlayIndex(0);
            else if (useStreamingAssets && streamingFiles != null && streamingFiles.Length > 0)
                PlayIndex(0);
        }
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    // Chame isto se quiser trocar música por cena específica (opcional)
    void OnSceneChanged(Scene oldS, Scene newS)
    {
        // Exemplo: if (newS.name.Contains("Boss")) PlayIndex(2);
    }

    public void PlayIndex(int index)
    {
        if (useStreamingAssets)
        {
            if (streamingFiles == null || streamingFiles.Length == 0) return;
            index = Mathf.Clamp(index, 0, streamingFiles.Length - 1);
            currentIndex = index;
            StartCoroutine(CrossfadeToStreaming(streamingFiles[index]));
        }
        else
        {
            if (tracks == null || tracks.Length == 0) return;
            index = Mathf.Clamp(index, 0, tracks.Length - 1);
            currentIndex = index;
            StartCoroutine(CrossfadeTo(tracks[index]));
        }
    }

    public void PlayNext()
    {
        if (useStreamingAssets)
        {
            if (streamingFiles == null || streamingFiles.Length == 0) return;
            if (shuffle)
                currentIndex = Random.Range(0, streamingFiles.Length);
            else
                currentIndex = (currentIndex + 1 + (currentIndex < 0 ? 0 : 0)) % streamingFiles.Length;
            StartCoroutine(CrossfadeToStreaming(streamingFiles[currentIndex]));
        }
        else
        {
            if (tracks == null || tracks.Length == 0) return;
            if (shuffle)
                currentIndex = Random.Range(0, tracks.Length);
            else
                currentIndex = (currentIndex + 1 + (currentIndex < 0 ? 0 : 0)) % tracks.Length;
            StartCoroutine(CrossfadeTo(tracks[currentIndex]));
        }
    }

    public void Stop(float fadeOut = 0.8f) => StartCoroutine(FadeOutAll(fadeOut));

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        // ajusta o que estiver tocando
        if (a.isPlaying) a.volume = Mathf.Min(a.volume, volume);
        if (b.isPlaying) b.volume = Mathf.Min(b.volume, volume);
    }

    private IEnumerator CrossfadeTo(AudioClip next)
    {
        // escolhe quem toca e quem entra
        AudioSource from = a.isPlaying ? a : b;
        AudioSource to   = a.isPlaying ? b : a;

        to.clip = next;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        float dur = Mathf.Max(0.05f, crossfadeTime);
        float startFrom = from.volume;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // independe do Time.timeScale
            float k = t / dur;
            to.volume   = Mathf.Lerp(0f, volume, k);
            from.volume = Mathf.Lerp(startFrom, 0f, k);
            yield return null;
        }

        to.volume = volume;
        var oldClip = from.clip;
        from.Stop();
        from.volume = 0f;

        // libera clipe antigo se veio por streaming
        if (useStreamingAssets && oldClip != null)
            Destroy(oldClip);
    }

    private IEnumerator CrossfadeToStreaming(string relativePath)
    {
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);
        if (!fullPath.StartsWith("file://")) fullPath = "file://" + fullPath;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(fullPath, streamingAudioType))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"MusicManager: falha ao carregar '{relativePath}': {req.error}");
                yield break;
            }
            var clip = DownloadHandlerAudioClip.GetContent(req);
            yield return StartCoroutine(CrossfadeTo(clip));
        }
    }

    private IEnumerator FadeOutAll(float dur)
    {
        float t = 0f;
        float a0 = a.volume;
        float b0 = b.volume;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - (t / dur);
            a.volume = a0 * k;
            b.volume = b0 * k;
            yield return null;
        }
        a.Stop(); b.Stop();
        a.volume = b.volume = 0f;
    }

    void Update()
    {
        // avança automaticamente quando nenhuma fonte está tocando
        bool anyPlaying = (a != null && a.isPlaying) || (b != null && b.isPlaying);
        if (!anyPlaying && currentIndex >= 0)
        {
            PlayNext();
        }
    }
}