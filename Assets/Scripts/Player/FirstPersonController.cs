using Framework.Core;
using UnityEngine;

/// <summary>실제로 점프가 발동한 순간(지면에서 뛰어오른 순간)마다 발행.</summary>
public struct PlayerJumped : IEvent { }

/// <summary>공중에 있다가 지면에 닿은 순간(착지)마다 발행.</summary>
public struct PlayerLanded : IEvent { }

/// <summary>이동 입력이 있는지 없는지가 바뀔 때만 발행 (뷰모델 이동 애니메이션 등에 사용).</summary>
public struct PlayerMoveStateChanged : IEvent
{
    public bool IsMoving;
}

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
    bool wasMoving;
    bool wasGrounded = true;

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
        Vector2 input = InputManager.Instance.ReadMove();
        Vector3 move = transform.forward * input.y + transform.right * input.x;
        if (move.sqrMagnitude > 1f) move.Normalize();

        bool isMoving = input.sqrMagnitude > 0.01f;
        if (isMoving != wasMoving)
        {
            wasMoving = isMoving;
            EventBus.Publish(new PlayerMoveStateChanged { IsMoving = isMoving });
        }

        bool grounded = cc.isGrounded;
        if (grounded && !wasGrounded) EventBus.Publish(new PlayerLanded());
        wasGrounded = grounded;

        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

        // 점프 (지면에 있을 때만)
        if (grounded && InputManager.Instance.ReadJump())
        {
            verticalVelocity = jumpSpeed;
            EventBus.Publish(new PlayerJumped());
        }

        // 중력 (아래로)
        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;

        cc.Move(velocity * Time.deltaTime);

        // 상승 중 천장 등 위쪽에 실제로 부딪혔다면 즉시 수직 속도 반전 → 바로 하강
        if (verticalVelocity > 0f && (cc.collisionFlags & CollisionFlags.Above) != 0)
            verticalVelocity = -verticalVelocity;
    }
}
