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

    [Tooltip("점프 높이(유닛)")]
    public float jumpHeight = 1.2f;

    [Tooltip("평지에서 점프~착지까지 걸리는 총 시간(초)")]
    public float airTime = 1.2f;

    [Tooltip("눈높이 카메라. 비우면 Camera.main을 머리에 붙여 사용")]
    public Transform cameraPivot;

    CharacterController cc;
    float pitch;
    float verticalVelocity;
    float gravity;     // 양수 크기 (jumpHeight/airTime에서 역산)
    float jumpSpeed;   // 점프 초기 속도

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;

        // 상승시간 t = airTime/2,  g = 2h/t²,  v₀ = g·t = √(2gh)
        float tUp = airTime * 0.5f;
        gravity = 2f * jumpHeight / (tUp * tUp);
        jumpSpeed = gravity * tUp;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 일시정지(timeScale 0) 중에는 이동/시점 입력을 막는다.
        if (Time.timeScale == 0f) return;

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

        bool grounded = cc.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

        // 점프 (지면에 있을 때만)
        if (grounded && InputManager.Instance.ReadJump())
            verticalVelocity = jumpSpeed;

        // 중력 (아래로)
        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;

        float yBefore = transform.position.y;
        cc.Move(velocity * Time.deltaTime);

        // 상승 중인데 Y가 거의 안 올랐다면(천장 등에 막힘) 즉시 수직 속도 반전 → 바로 하강
        float expectedRise = verticalVelocity * Time.deltaTime;
        if (verticalVelocity > 0f && transform.position.y - yBefore < expectedRise * 0.5f)
            verticalVelocity = -verticalVelocity;
    }
}
