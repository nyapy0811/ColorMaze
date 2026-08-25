using UnityEngine;

/// <summary>
/// 미로의 앵커 컴포넌트.
/// 블록은 Scene 뷰 편집(MazeGeneratorEditor)으로 이 오브젝트 하위의 "Maze"에 쌓이며,
/// 실제 씬 오브젝트로 저장된다.
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Header("빠른 설치용 필터 프리팹 (Scene 뷰 블록 편집 모드의 라디오 버튼에서 사용)")]
    public GameObject colorFilterPrefab;
    public GameObject rgbFilterPrefab;

    [Header("빠른 설치용 상호작용 기물 프리팹 (Scene 뷰 블록 편집 모드의 라디오 버튼에서 사용)")]
    public GameObject bucketPrefab;
    public GameObject canvasPrefab;
    public GameObject palettePrefab;
    public GameObject colorChangerPrefab;
    public GameObject stackChangerPrefab;

    [Header("기물 순서 참고용 (인게임 기능과 무관 — 설계 확인용 목록)")]
    [Tooltip("정답 순서대로 기물을 끌어다 놓는 목록 1 (캔버스가 여러 개인 스테이지의 첫 번째 서브 퍼즐 등)")]
    public System.Collections.Generic.List<MapObjectBase> correctOrder1 = new();
    [Tooltip("정답 순서대로 기물을 끌어다 놓는 목록 2 (두 번째 서브 퍼즐 등, 없으면 비워둠)")]
    public System.Collections.Generic.List<MapObjectBase> correctOrder2 = new();
    // 두 목록 중 어디에도 없는 기물은 전부 더미/함정으로 취급한다(별도 목록 불필요).
}
