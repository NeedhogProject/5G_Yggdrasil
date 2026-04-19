using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioManager
/// - BGM : 마을 / 층별(1~4층) / 전투 상황별 트랙 관리
///         즉시 전환 & 크로스페이드 모두 지원
/// - SFX : 오디오 풀링으로 다수 동시 재생 지원
/// - 볼륨 : PlayerPrefs 영구 저장
///
/// [사용법]
///   AudioManager.Instance.PlayBGM(BGMTrack.Town);
///   AudioManager.Instance.PlayBGM(BGMTrack.Floor1, fadeTime: 1.5f);
///   AudioManager.Instance.PlaySFX(SFXClip.CoinFlip);
///   AudioManager.Instance.SetBGMVolume(0.8f);
/// </summary>

// ───────────────────────────────────────────
// 열거형 — 트랙/클립 추가 시 여기에만 추가
// ───────────────────────────────────────────

public enum BGMTrack
{
    None,
    Town,       // 마을
    Floor1,     // 1층 — 위그드라실의 가호
    Floor2,     // 2층 — 썩어가는 뿌리
    Floor3,     // 3층 — 심연의 둥지 입구
    Floor4,     // 4층 — 니드호그의 둥지
    Battle,     // 전투 (보스 등 별도 전투 BGM)
    Ending,     // 엔딩
}

public enum SFXClip
{
    // 강화 시스템
    CoinFlip,
    EnhanceSuccess,
    EnhanceFail,

    // 각인 시스템
    InscribeApply,
    InscribeReset,

    // 인벤토리 / 장비
    ItemPickup,
    ItemEquip,
    ItemDrop,

    // 전투
    AttackDagger,
    AttackSword,
    AttackSpear,
    PlayerHit,
    PlayerDeath,
    EnemyHit,
    EnemyDeath,

    // UI
    UIOpen,
    UIClose,
    UIClick,
    UIError,

    // 세트 효과 발동
    SetEffectActivate,

    // 환경
    KeyPickup,
    DoorOpen,
}

// ───────────────────────────────────────────
// Inspector 바인딩용 데이터 클래스
// ───────────────────────────────────────────

[System.Serializable]
public class BGMEntry
{
    public BGMTrack track;
    public AudioClip clip;
}

[System.Serializable]
public class SFXEntry
{
    public SFXClip sfx;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.9f, 1.1f)] public float pitchMin = 1f;
    [Range(0.9f, 1.1f)] public float pitchMax = 1f;
}

// ───────────────────────────────────────────
// AudioManager 본체
// ───────────────────────────────────────────

public class AudioManager : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ── Inspector 설정 ───────────────────────
    [Header("BGM 트랙 목록")]
    [SerializeField] private List<BGMEntry> bgmEntries = new();

    [Header("SFX 클립 목록")]
    [SerializeField] private List<SFXEntry> sfxEntries = new();

    [Header("풀링")]
    [SerializeField] private int sfxPoolSize = 10;

    [Header("기본 볼륨 (첫 실행 시 적용)")]
    [SerializeField] [Range(0f, 1f)] private float defaultBGMVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float defaultSFXVolume = 0.9f;

    // ── PlayerPrefs 키 ───────────────────────
    private const string KEY_BGM_VOL = "BGMVolume";
    private const string KEY_SFX_VOL = "SFXVolume";

    // ── 내부 상태 ────────────────────────────
    private AudioSource bgmSourceA;        // 크로스페이드 채널 A
    private AudioSource bgmSourceB;        // 크로스페이드 채널 B
    private bool        isChannelA = true; // 현재 메인 채널

    private float bgmVolume;
    private float sfxVolume;

    private BGMTrack currentTrack = BGMTrack.None;
    private Coroutine fadeCoroutine;

    // SFX 오디오 풀
    private Queue<AudioSource> sfxPool = new();
    private List<AudioSource>  sfxActive = new();

    // 빠른 조회용 딕셔너리
    private Dictionary<BGMTrack, BGMEntry> bgmMap = new();
    private Dictionary<SFXClip,  SFXEntry> sfxMap = new();

    // ── 프로퍼티 ─────────────────────────────
    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    public BGMTrack CurrentTrack => currentTrack;

    // ════════════════════════════════════════
    // 초기화
    // ════════════════════════════════════════

    private void Awake()
    {
        // 싱글톤 — 씬 전환 시 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookupMaps();
        LoadVolumeSettings();
        SetupAudioSources();
        BuildSFXPool();
    }

    private void BuildLookupMaps()
    {
        foreach (var entry in bgmEntries)
            bgmMap[entry.track] = entry;
        foreach (var entry in sfxEntries)
            sfxMap[entry.sfx] = entry;
    }

    private void LoadVolumeSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOL, defaultBGMVolume);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, defaultSFXVolume);
    }

    private void SetupAudioSources()
    {
        // BGM 채널 A / B 생성
        bgmSourceA = CreateAudioSource("BGM_A", loop: true);
        bgmSourceB = CreateAudioSource("BGM_B", loop: true);
        bgmSourceA.volume = bgmVolume;
        bgmSourceB.volume = 0f;
    }

    private void BuildSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = CreateAudioSource($"SFX_Pool_{i}", loop: false);
            src.volume = 0f;
            sfxPool.Enqueue(src);
        }
    }

    private AudioSource CreateAudioSource(string sourceName, bool loop)
    {
        GameObject go = new(sourceName);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        return src;
    }

    // ════════════════════════════════════════
    // BGM — 공개 API
    // ════════════════════════════════════════

    /// <summary>
    /// BGM 재생.
    /// fadeTime = 0 → 즉시 전환 / fadeTime > 0 → 크로스페이드
    /// </summary>
    public void PlayBGM(BGMTrack track, float fadeTime = 0f)
    {
        if (track == currentTrack) return;
        if (!bgmMap.TryGetValue(track, out BGMEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[AudioManager] BGM 클립 없음: {track}");
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        currentTrack = track;

        if (fadeTime <= 0f)
            SwitchBGMImmediate(entry.clip);
        else
            fadeCoroutine = StartCoroutine(CrossFadeBGM(entry.clip, fadeTime));
    }

    /// <summary>BGM 정지</summary>
    public void StopBGM(float fadeTime = 0f)
    {
        if (fadeTime <= 0f)
        {
            bgmSourceA.Stop();
            bgmSourceB.Stop();
        }
        else
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutCurrent(fadeTime));
        }
        currentTrack = BGMTrack.None;
    }

    // ── BGM 내부 처리 ────────────────────────

    private void SwitchBGMImmediate(AudioClip clip)
    {
        AudioSource main = isChannelA ? bgmSourceA : bgmSourceB;
        AudioSource sub  = isChannelA ? bgmSourceB : bgmSourceA;

        sub.Stop();
        sub.volume = 0f;

        main.clip   = clip;
        main.volume = bgmVolume;
        main.Play();
    }

    private IEnumerator CrossFadeBGM(AudioClip newClip, float duration)
    {
        // 다음 채널에 새 클립 준비
        AudioSource incoming = isChannelA ? bgmSourceB : bgmSourceA;
        AudioSource outgoing = isChannelA ? bgmSourceA : bgmSourceB;

        incoming.clip   = newClip;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed = 0f;
        float startVol = outgoing.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            outgoing.volume = Mathf.Lerp(startVol, 0f, t);
            incoming.volume = Mathf.Lerp(0f, bgmVolume, t);
            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;
        incoming.volume = bgmVolume;

        isChannelA = !isChannelA;
        fadeCoroutine = null;
    }

    private IEnumerator FadeOutCurrent(float duration)
    {
        AudioSource main = isChannelA ? bgmSourceA : bgmSourceB;
        float startVol = main.volume;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            main.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        main.Stop();
        main.volume = 0f;
    }

    // ════════════════════════════════════════
    // SFX — 공개 API
    // ════════════════════════════════════════

    /// <summary>효과음 재생 (풀링)</summary>
    public void PlaySFX(SFXClip sfx)
    {
        if (!sfxMap.TryGetValue(sfx, out SFXEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX 클립 없음: {sfx}");
            return;
        }

        AudioSource src = GetPooledSFXSource();
        if (src == null) return; // 풀 소진 시 스킵

        src.clip   = entry.clip;
        src.volume = entry.volume * sfxVolume;
        src.pitch  = Random.Range(entry.pitchMin, entry.pitchMax);
        src.Play();

        sfxActive.Add(src);
        StartCoroutine(ReturnToPool(src, entry.clip.length / src.pitch));
    }

    /// <summary>
    /// 위치 기반 3D SFX 재생 (추후 공간음향 확장용)
    /// </summary>
    public void PlaySFXAt(SFXClip sfx, Vector3 position)
    {
        if (!sfxMap.TryGetValue(sfx, out SFXEntry entry) || entry.clip == null) return;
        AudioSource.PlayClipAtPoint(entry.clip, position, entry.volume * sfxVolume);
    }

    // ── SFX 풀링 내부 처리 ───────────────────

    private AudioSource GetPooledSFXSource()
    {
        // 재생 완료된 active 소스 먼저 재활용 시도
        for (int i = sfxActive.Count - 1; i >= 0; i--)
        {
            if (!sfxActive[i].isPlaying)
            {
                AudioSource recycled = sfxActive[i];
                sfxActive.RemoveAt(i);
                return recycled;
            }
        }

        if (sfxPool.Count > 0)
            return sfxPool.Dequeue();

        Debug.LogWarning("[AudioManager] SFX 풀 소진 — 가장 오래된 소스 강제 재사용");
        if (sfxActive.Count > 0)
        {
            AudioSource oldest = sfxActive[0];
            sfxActive.RemoveAt(0);
            oldest.Stop();
            return oldest;
        }
        return null;
    }

    private IEnumerator ReturnToPool(AudioSource src, float delay)
    {
        yield return new WaitForSecondsRealtime(delay + 0.05f);
        src.Stop();
        src.clip   = null;
        src.volume = 0f;
        sfxActive.Remove(src);
        sfxPool.Enqueue(src);
    }

    // ════════════════════════════════════════
    // 볼륨 — 공개 API
    // ════════════════════════════════════════

    /// <summary>BGM 볼륨 설정 및 저장</summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KEY_BGM_VOL, bgmVolume);

        // 현재 재생 중인 BGM 채널에 즉시 반영
        AudioSource main = isChannelA ? bgmSourceA : bgmSourceB;
        if (main.isPlaying) main.volume = bgmVolume;
    }

    /// <summary>SFX 볼륨 설정 및 저장</summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, sfxVolume);
        // 재생 중인 SFX는 다음 재생부터 반영됨
    }

    /// <summary>PlayerPrefs 볼륨 저장 강제 플러시 (게임 종료 시 호출 권장)</summary>
    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    // ════════════════════════════════════════
    // 편의 헬퍼 — 층/상황별 BGM 자동 전환
    // ════════════════════════════════════════

    /// <summary>
    /// 층 번호(0 = 마을, 1~4 = 던전 층)로 BGM 자동 전환
    /// </summary>
    public void PlayFloorBGM(int floor, float fadeTime = 1.0f)
    {
        BGMTrack track = floor switch
        {
            0 => BGMTrack.Town,
            1 => BGMTrack.Floor1,
            2 => BGMTrack.Floor2,
            3 => BGMTrack.Floor3,
            4 => BGMTrack.Floor4,
            _ => BGMTrack.None
        };

        if (track != BGMTrack.None)
            PlayBGM(track, fadeTime);
    }

    /// <summary>
    /// 전투 BGM 전환 (전투 시작 시 호출)
    /// fadeTime = 0 → 즉시 / 기본 0.5초 페이드
    /// </summary>
    public void EnterBattle(float fadeTime = 0.5f)
    {
        PlayBGM(BGMTrack.Battle, fadeTime);
    }

    /// <summary>
    /// 전투 종료 후 이전 필드 BGM으로 복귀
    /// </summary>
    public void ExitBattle(int currentFloor, float fadeTime = 1.0f)
    {
        PlayFloorBGM(currentFloor, fadeTime);
    }

    // ════════════════════════════════════════
    // 생명주기z
    // ════════════════════════════════════════

    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveSettings();
    }

    private void Update()
    {
        // 재생 완료된 active SFX 정리 (풀 복귀 코루틴과 이중 보호)
        for (int i = sfxActive.Count - 1; i >= 0; i--)
        {
            if (sfxActive[i] != null && !sfxActive[i].isPlaying)
            {
                AudioSource src = sfxActive[i];
                sfxActive.RemoveAt(i);
                src.clip = null;
                sfxPool.Enqueue(src);
            }
        }
    }
}
