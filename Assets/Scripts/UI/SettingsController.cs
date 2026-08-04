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

    [Header("저장 초기화 확인창 (기본 비활성 상태로 둘 것)")]
    [SerializeField] GameObject resetConfirmPanel;

    void OnEnable()
    {
        if (bgmSlider)
        {
            bgmSlider.SetValueWithoutNotify(GameAudio.Instance.GetBgmVolume());
            bgmSlider.onValueChanged.AddListener(SetBgm);
        }
        if (sfxSlider)
        {
            sfxSlider.SetValueWithoutNotify(GameAudio.Instance.GetSfxVolume());
            sfxSlider.onValueChanged.AddListener(SetSfx);
        }
        if (sensitivitySlider)
        {
            var fpc = FirstPersonController.Instance;
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

    void SetBgm(float v) => GameAudio.Instance.SetBgmVolume(v);

    void SetSfx(float v) => GameAudio.Instance.SetSfxVolume(v);

    void SetSensitivity(float v)
    {
        var fpc = FirstPersonController.Instance;
        if (fpc) fpc.mouseSensitivity = v;
    }

    // --- 버튼 OnClick 연결용 ---

    /// <summary>저장 초기화 버튼: 바로 초기화하지 않고 확인창을 띄운다.</summary>
    public void OnResetSaveButton()
    {
        GameAudio.Instance.PlayButtonClick();
        if (resetConfirmPanel) resetConfirmPanel.SetActive(true);
    }

    /// <summary>확인창의 "확인" 버튼: 실제로 저장 파일을 초기화한다.</summary>
    public void OnConfirmResetButton()
    {
        GameAudio.Instance.PlayButtonClick();
        SaveManager.Instance.Delete();
        if (resetConfirmPanel) resetConfirmPanel.SetActive(false);
    }

    /// <summary>확인창의 "취소" 버튼: 초기화하지 않고 닫는다.</summary>
    public void OnCancelResetButton()
    {
        GameAudio.Instance.PlayButtonClick();
        if (resetConfirmPanel) resetConfirmPanel.SetActive(false);
    }
}
