using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 큐브 맵의 프리팹(애셋은 Assets/Data). 블록은 아직 한 종류이고 임시로 1×1×1 회색 상자다(복셀 모델은 아트 단계).
    /// 기물 프리팹은 큐브 맵 편집 도구의 팔레트가 쓴다(지금은 시작 지점·색 원천·목적지만, 2026-11-07).
    /// </summary>
    [CreateAssetMenu(menuName = "ColorMaze/큐브 맵 설정", fileName = "CubeGridConfig")]
    public class CubeGridConfig : ScriptableObject
    {
        [Tooltip("블록(1×1×1, 피벗은 가운데, 콜라이더 포함, static)")]
        public GameObject blockPrefab;

        [Header("팔레트 기물 (피벗은 바닥 가운데)")]
        public GameObject startPointPrefab;
        public GameObject colorSourcePrefab;
        public GameObject goalPrefab;
    }
}
