using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 소리 연결(10.2, 코드 흐름 Q502): 이벤트마다 낼 효과음. 비워 둔 칸은 소리를 내지 않는다.
    /// 지금은 있는 소리로 임시 연결했고, 효과음은 모두 SFX 믹서 그룹으로 나간다(5종 나누기는 설정 화면을 설계할 때).
    /// </summary>
    [CreateAssetMenu(menuName = "ColorMaze/소리 연결", fileName = "SoundEventConfig")]
    public class SoundEventConfig : ScriptableObject
    {
        public AudioClip orbExtracted;
        public AudioClip extractRejected;
        public AudioClip orbsMixed;
        [Tooltip("제출 화면의 넣기·되돌리기·회수를 받아들임")]
        public AudioClip submitAccepted;
        [Tooltip("제출 화면의 넣기·되돌리기·회수를 거부")]
        public AudioClip submitRejected;
        public AudioClip actuatorOn;
        public AudioClip actuatorOff;
        public AudioClip gateOpened;
        public AudioClip gateClosed;
        public AudioClip mapCleared;
    }
}
