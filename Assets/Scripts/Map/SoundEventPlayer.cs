using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 소리 연결(9절, 코드 흐름 Q502). 맵 기본 프리팹(MapRoot)에 두고, 이벤트를 받아 소리 연결 애셋의 효과음을 낸다.
    /// 기물은 소리를 직접 내지 않으므로 소리를 바꾸거나 더할 때 이곳과 애셋만 고친다.
    /// </summary>
    public class SoundEventPlayer : MonoBehaviour
    {
        [SerializeField] SoundEventConfig config;

        void OnEnable()
        {
            EventBus.Subscribe<OrbExtracted>(OnOrbExtracted);
            EventBus.Subscribe<ExtractRejected>(OnExtractRejected);
            EventBus.Subscribe<OrbsMixed>(OnOrbsMixed);
            EventBus.Subscribe<SubmitAttempted>(OnSubmitAttempted);
            EventBus.Subscribe<ActuatorStateChanged>(OnActuatorStateChanged);
            EventBus.Subscribe<GateStateChanged>(OnGateStateChanged);
            EventBus.Subscribe<MapCleared>(OnMapCleared);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<OrbExtracted>(OnOrbExtracted);
            EventBus.Unsubscribe<ExtractRejected>(OnExtractRejected);
            EventBus.Unsubscribe<OrbsMixed>(OnOrbsMixed);
            EventBus.Unsubscribe<SubmitAttempted>(OnSubmitAttempted);
            EventBus.Unsubscribe<ActuatorStateChanged>(OnActuatorStateChanged);
            EventBus.Unsubscribe<GateStateChanged>(OnGateStateChanged);
            EventBus.Unsubscribe<MapCleared>(OnMapCleared);
        }

        void OnOrbExtracted(OrbExtracted e) => Play(config.orbExtracted);
        void OnExtractRejected(ExtractRejected e) => Play(config.extractRejected);
        void OnOrbsMixed(OrbsMixed e) => Play(config.orbsMixed);
        void OnSubmitAttempted(SubmitAttempted e) => Play(e.Accepted ? config.submitAccepted : config.submitRejected);
        void OnActuatorStateChanged(ActuatorStateChanged e) => Play(e.IsOn ? config.actuatorOn : config.actuatorOff);
        void OnGateStateChanged(GateStateChanged e) => Play(e.IsOpen ? config.gateOpened : config.gateClosed);
        void OnMapCleared(MapCleared e) => Play(config.mapCleared);

        static void Play(AudioClip clip)
        {
            if (clip != null) AudioManager.Instance.PlaySFX(clip);
        }
    }
}
