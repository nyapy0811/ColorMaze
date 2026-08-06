using System.Collections;
using Framework.Core;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 게임 전역 BGM/SFX 전담 매니저. AudioClip 애셋은 여기서만 참조하고,
/// 다른 스크립트는 의미 있는 이름의 메서드(PlayClear 등)만 호출한다
/// (AudioManager.PlayBGM/PlaySFX에 클립을 직접 넘기지 않는다).
/// MonoSingleton이라 다른 매니저들처럼 씬 전환에도 유지된다(스테이지 씬으로 넘어가도 파괴되지 않음).
/// MainMenu 씬의 오브젝트에 붙여 인스펙터에서 클립을 연결해 둘 것.
///
/// 음량 조절은 AudioMixer로 한다: AudioManager가 만든 AudioSource(BGM 1개 + SFX 풀)를
/// 시작 시 각각 BGM/SFX 믹서 그룹으로 라우팅해두고, 슬라이더 값(선형 0~1)은
/// dB = 20*log10(v)로 변환해서 믹서의 노출 파라미터에 적용한다(사람 귀는 소리 크기를
/// 로그 스케일로 느끼므로, 선형 값을 AudioSource.volume에 그대로 넣으면 슬라이더 상단
/// 구간에서 음량 변화가 거의 안 느껴진다).
/// </summary>
public class GameAudio : MonoSingleton<GameAudio>
{
    const string BgmParam = "BGMVolume";
    const string SfxParam = "SFXVolume";
    const float MinLinearVolume = 0.0001f; // 0이면 log10(0)=-무한대라 clamp

    [Header("클립")]
    [SerializeField] AudioClip bgm;
    [SerializeField] AudioClip buttonClick;
    [SerializeField] AudioClip clear;
    [SerializeField] AudioClip nope;

    [Header("믹서 (음량 조절용)")]
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] AudioMixerGroup bgmGroup;
    [SerializeField] AudioMixerGroup sfxGroup;

    protected override void OnAwake()
    {
        AudioManager.Instance.PlayBGM(bgm);
        RouteAudioSources();

        // AudioMixer.SetFloat은 Awake 시점엔 믹서가 아직 준비 안 돼 있어서 조용히 씹히는 경우가 있다
        // (한 프레임 뒤에 적용하면 문제없이 반영됨). 그래서 한 프레임 미루고 적용한다.
        StartCoroutine(ApplySavedVolumeNextFrame());
    }

    IEnumerator ApplySavedVolumeNextFrame()
    {
        yield return null;
        SetMixerVolume(BgmParam, GameSettings.Current.bgmVolume);
        SetMixerVolume(SfxParam, GameSettings.Current.sfxVolume);
    }

    public void PlayButtonClick() => AudioManager.Instance.PlaySFX(buttonClick);
    public void PlayClear() => AudioManager.Instance.PlaySFX(clear);
    public void PlayNope() => AudioManager.Instance.PlaySFX(nope);

    /// <summary>AudioManager가 자기 오브젝트에 만든 AudioSource들을 믹서 그룹으로 연결한다.
    /// AudioManager.OnAwake()에서 BGM 소스를 먼저 추가하고 그 다음 SFX 풀을 추가하므로,
    /// GetComponents 배열의 0번이 BGM, 나머지가 SFX다.</summary>
    void RouteAudioSources()
    {
        var sources = AudioManager.Instance.GetComponents<AudioSource>();
        if (sources.Length == 0) return;

        sources[0].outputAudioMixerGroup = bgmGroup;
        for (int i = 1; i < sources.Length; i++)
            sources[i].outputAudioMixerGroup = sfxGroup;
    }

    public void SetBgmVolume(float linear)
    {
        SetMixerVolume(BgmParam, linear);
        GameSettings.Current.bgmVolume = linear;
        GameSettings.Save();
    }

    public void SetSfxVolume(float linear)
    {
        SetMixerVolume(SfxParam, linear);
        GameSettings.Current.sfxVolume = linear;
        GameSettings.Save();
    }

    public float GetBgmVolume() => GameSettings.Current.bgmVolume;
    public float GetSfxVolume() => GameSettings.Current.sfxVolume;

    void SetMixerVolume(string param, float linear)
    {
        if (!audioMixer) return;
        float dB = 20f * Mathf.Log10(Mathf.Max(linear, MinLinearVolume));
        audioMixer.SetFloat(param, dB);
    }
}
