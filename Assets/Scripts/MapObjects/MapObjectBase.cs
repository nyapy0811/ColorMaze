using UnityEngine;

/// <summary>
/// 모든 맵 기물(4장)의 공통 베이스.
/// 콜라이더 참조와 "이 콜라이더가 플레이어인가" 판정을 한 곳에서 제공해,
/// 각 그룹(판정형/획득형/클리어형/소모성) 베이스가 자기 역할만 신경 쓰면 되게 한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class MapObjectBase : MonoBehaviour
{
    protected Collider Col { get; private set; }

    ColorStacks player;

    /// <summary>씬의 플레이어(ColorStacks 보유 오브젝트)를 지연 탐색해 캐싱한다.</summary>
    protected ColorStacks Player
    {
        get
        {
            if (player == null) player = FindAnyObjectByType<ColorStacks>();
            return player;
        }
    }

    protected virtual void Awake() => Col = GetComponent<Collider>();

    /// <summary>이 콜라이더가 플레이어(의 자식 콜라이더)인지 확인한다.</summary>
    protected bool IsPlayer(Collider other) => Player != null && other.GetComponentInParent<ColorStacks>() == Player;
}
