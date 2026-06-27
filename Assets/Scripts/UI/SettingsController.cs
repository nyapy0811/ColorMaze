using Framework.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 화면. BGM/SFX 음량과 마우스 감도를 슬라이더로 조절한다.
/// 일시정지 메뉴에서 열린다(별도 패널). 패널이 켜질 때 현재 값으로 슬라이더를 초기화한다.
/// </summary>
public class SettingsController : MonoBehaviour
{
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider sensitivitySlider;

    void OnEnable()
    {
        var audio = AudioManager.Instance;

        if (bgmSlider)
        {
            bgmSlider.SetValueWithoutNotify(audio.bgmVolume);
            bgmSlider.onValueChanged.AddListener(SetBgm);
        }
        if (sfxSlider)
        {
            sfxSlider.SetValueWithoutNotify(audio.sfxVolume);
            sfxSlider.onValueChanged.AddListener(SetSfx);
        }
        if (sensitivitySlider)
        {
            var fpc = FindAnyObjectByType<FirstPersonController>();
            if (fpc) sensitivitySlider.SetValueWithoutNotify(fpc.mouseSensitivity);
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }
    }

    void OnDisable()
    {
        if (bgmSlider) bgmSlider.onValueChanged.RemoveListener(SetBgm);
        if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(SetSfx);
        if (sensitivitySlider) sensitivitySlider.onValueChanged.RemoveListener(SetSensitivity);
    }

    void SetBgm(float v) => AudioManager.Instance.bgmVolume = v;
    void SetSfx(float v) => AudioManager.Instance.sfxVolume = v;

    void SetSensitivity(float v)
    {
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc) fpc.mouseSensitivity = v;
    }
}
