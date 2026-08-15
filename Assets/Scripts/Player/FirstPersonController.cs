using UnityEngine;

/// <summary>
/// 1인칭 이동 + 마우스 시점.
/// 마우스 좌우는 몸통(yaw)을, 상하는 카메라(pitch)를 회전시킨다.
/// WASD는 몸통이 바라보는 방향 기준으로 이동한다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    /// <summary>씬에 하나뿐인 플레이어의 FirstPersonController. Awake에서 등록되고 OnDestroy에서 해제된다.</summary>
    public static FirstPersonController Instance { get; private set; }

    [Tooltip("이동 속도(유닛/초)")]
    public float moveSpeed = 5f;

    [Tooltip("마우스 감도")]
    public float mouseSensitivity = 0.1f;

    [Tooltip("위/아래 시점 제한(도)")]
    public float pitchLimit = 85f;

    [Tooltip("정지 상태에서 최대 이동 속도까지 도달하는 데 걸리는 시간(초, 지면 전용)")]
    public float accelerationTime = 0.1f;

    [Tooltip("공중에서 이동키를 뗐을 때 최대 속도에서 0까지 감속되는 데 걸리는 시간(초)")]
    public float airDecelerationTime = 0.1f;

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
    Vector3 horizontalVelocity; // 수평 이동 속도. 공중에서는 땅을 떠난 순간 값(방향+속도)이 착지할 때까지 그대로 고정된다
                                 // (그렇지 않으면 공중에서 이동키로 방향을 바꿔 1칸짜리 벽을 옆으로 우회해 넘어갈 수 있다).

    void Awake()
    {
        Instance = this;
        cc = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;

        if (GameSettings.HasSave()) mouseSensitivity = GameSettings.Current.mouseSensitivity;

        // 상승시간 t = airTime/2,  g = 2h/t²,  v₀ = g·t = √(2gh)
        float tUp = airTime * 0.5f;
        gravity = 2f * jumpHeight / (tUp * tUp);
        jumpSpeed = gravity * tUp;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
        bool grounded = cc.isGrounded;
        Vector2 input = InputManager.Instance.ReadMove();

        if (grounded)
        {
            // 지면: 입력 방향으로 목표 속도를 정하고, accelerationTime으로 역산한 가속도만큼씩 서서히 다가간다(관성).
            float accel = moveSpeed / Mathf.Max(accelerationTime, 0.0001f);
            Vector3 move = transform.forward * input.y + transform.right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();
            Vector3 targetVelocity = move * moveSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accel * Time.deltaTime);
        }
        else if (input.sqrMagnitude == 0f)
        {
            // 공중: 방향은 못 바꾼다. 땅에서 누르고 있던 이동키를 계속 누르고 있으면 속도를 유지하고,
            // 키를 떼면 airDecelerationTime으로 역산한 비율로 감속한다.
            float decel = moveSpeed / Mathf.Max(airDecelerationTime, 0.0001f);
            float speed = Mathf.MoveTowards(horizontalVelocity.magnitude, 0f, decel * Time.deltaTime);
            horizontalVelocity = horizontalVelocity.normalized * speed;
        }

        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

        // 점프 (지면에 있을 때만)
        if (grounded && InputManager.Instance.ReadJump())
        {
            verticalVelocity = jumpSpeed;
        }

        // 중력 (아래로)
        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        cc.Move(velocity * Time.deltaTime);

        // 상승 중 천장 등 위쪽에 실제로 부딪혔다면 즉시 수직 속도 반전 → 바로 하강
        if (verticalVelocity > 0f && (cc.collisionFlags & CollisionFlags.Above) != 0)
            verticalVelocity = -verticalVelocity;
    }
}
