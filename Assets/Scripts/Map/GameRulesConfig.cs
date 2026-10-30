using UnityEngine;

namespace ColorMaze
{
    /// <summary>게임 규칙 수치. 모든 맵이 같이 쓴다(애셋은 Assets/Data). 맵 매니저가 들고 플레이어와 기물에 나눠 준다.</summary>
    [CreateAssetMenu(menuName = "ColorMaze/게임 규칙", fileName = "GameRulesConfig")]
    public class GameRulesConfig : ScriptableObject
    {
        [Tooltip("기본 채널 한도(R, G, B). 맵 안에서는 흡수로 늘어난다.")]
        public OrbValue baseChannelLimit = new(5, 5, 5);

        [Tooltip("소지 오브 슬롯 수")]
        public int slotCount = 5;

        [Tooltip("조준해 상호작용할 수 있는 거리(유닛)")]
        public float interactDistance = 1.3f;

        [Tooltip("오브 발광 세기 계수(가장 큰 채널 × 계수). 블룸이 꺼져 있어 1을 넘는 발광은 더 밝아 보이지 않으므로 " +
                 "값 1~5가 0.2~1이 되게 둔다(임시, 블룸은 미정).")]
        public float glowFactor = 0.2f;
    }
}
