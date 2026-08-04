using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 클릭음을 재생한다. 재생 여부는 GameAudio(전역 오디오 매니저)에게 맡기고,
/// 이 컴포넌트는 어떤 버튼에 붙었는지만 안다. 클릭 사운드가 필요한 Button에 붙일 것.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSfx : MonoBehaviour
{
    void Awake() => GetComponent<Button>().onClick.AddListener(() => GameAudio.Instance.PlayButtonClick());
}
