using UnityEngine;

/// <summary>
/// 1인칭 이동 + 마우스 시점.
/// 마우스 좌우는 몸통(yaw)을, 상하는 카메라(pitch)를 회전시킨다.
/// WASD는 몸통이 바라보는 방향 기준으로 이동한다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Tooltip("이동 속도(유닛/초)")]
    public float moveSpeed = 5f;

    [Tooltip("마우스 감도")]
    public float mouseSensitivity = 0.1f;

    [Tooltip("위/아래 시점 제한(도)")]
    public float pitchLimit = 85f;

    [Tooltip("중력 가속도(음수)")]
    public float gravity = -20f;

    [Tooltip("눈높이 카메라. 비우면 Camera.main을 머리에 붙여 사용")]
    public Transform cameraPivot;

    CharacterController cc;
    float pitch;
    float verticalVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    {
        Vector2 look = InputManager.Instance.ReadLook() * mouseSensitivity;

        // 좌우: 몸통 회전
        transform.Rotate(Vector3.up, look.x, Space.Self);

        // 상하: 카메라 회전(제한)
        if (cameraPivot != null)
        {
            pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void Move()
    {
        Vector2 input = InputManager.Instance.ReadMove();
        Vector3 move = transform.forward * input.y + transform.right * input.x;
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;
        cc.Move(velocity * Time.deltaTime);
    }
}
