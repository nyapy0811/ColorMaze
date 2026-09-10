using UnityEngine;

/// <summary>인게임 맵 에디터가 배치한 블록/기물의 그리드 좌표를 들고 있는 꼬리표.
/// 제거 입력 시 레이캐스트가 맞은 오브젝트에서 이 컴포넌트를 찾아 어느 칸을 지울지 판단한다.</summary>
public class PlacedTag : MonoBehaviour
{
    public Vector3Int Cell;
}
