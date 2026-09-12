using UnityEngine;

/// <summary>인게임 맵 에디터 전용 카메라 조작 — 유니티 Scene 뷰와 동일하게 마우스만으로 탐색한다.
/// 우클릭+드래그=시점 회전, 휠클릭+드래그=평행 이동(Pan), 휠 스크롤=전후 이동(Dolly). 중력/충돌 없음.
/// 공용 Player.prefab이 아니라 MapEditor.unity의 Player 인스턴스에만 붙는다.</summary>
public class EditorFlyCamera : MonoBehaviour
{
    [Tooltip("마우스 감도(회전)")]
    public float rotateSensitivity = 0.1f;

    [Tooltip("평행 이동 속도")]
    public float panSpeed = 0.02f;

    [Tooltip("스크롤 이동 속도")]
    public float dollySpeed = 2f;

    [Tooltip("위/아래 시점 제한(도)")]
    public float pitchLimit = 85f;

    [Tooltip("눈높이 카메라. 비우면 Camera.main을 사용")]
    public Transform cameraPivot;

    float pitch;

    void Awake()
    {
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (InputManager.Instance.ReadRotateHeld()) Rotate();
        if (InputManager.Instance.ReadPanHeld()) Pan();

        float dolly = InputManager.Instance.ReadDolly();
        if (dolly != 0f) Dolly(dolly);
    }

    void Rotate()
    {
        Vector2 look = InputManager.Instance.ReadLook() * rotateSensitivity;
        transform.Rotate(Vector3.up, look.x, Space.Self);

        if (cameraPivot != null)
        {
            pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void Pan()
    {
        Vector2 delta = InputManager.Instance.ReadLook(); // 마우스 델타 재사용
        Transform view = cameraPivot != null ? cameraPivot : transform;
        transform.position += (-view.right * delta.x - view.up * delta.y) * panSpeed;
    }

    void Dolly(float amount)
    {
        Transform view = cameraPivot != null ? cameraPivot : transform;
        transform.position += view.forward * amount * dollySpeed;
    }
}
