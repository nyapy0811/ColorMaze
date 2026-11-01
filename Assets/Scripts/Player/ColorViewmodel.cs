using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 뷰모델: 화면 오른쪽 아래의 손과 그 위에 떠 있는 지금 색 구 하나. 손과 구는 임시 모양이다.
    /// '지금 색 바뀜'을 받아 구를 칠한다(색은 정규화 RGB, 발광은 가장 큰 채널에 비례, (0,0,0)이면 숨김).
    /// '색 흡수'를 받으면 손을 뻗었다 돌아오고 구가 색 원천 쪽에서 날아와 자리에 든다.
    /// '색 거부'를 받으면 손과 구가 짧게 흔들린다(임시).
    /// 뷰모델 레이어만 그리는 오버레이 카메라가 따로 있어 벽에 붙어도 손이 벽에 묻히지 않는다.
    /// </summary>
    public class ColorViewmodel : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("손과 구를 함께 움직이는 부분(뻗기·흔들기)")]
        [SerializeField] Transform rig;

        [Tooltip("지금 색 구(임시 구, 손 위 자리의 자식). 발광이 켜진 재질이어야 한다.")]
        [SerializeField] Renderer sphere;

        [Header("움직임")]
        [SerializeField] float bobHeight = 0.004f;
        [SerializeField] float reachDistance = 0.06f;
        [Tooltip("손 뻗기와 구가 날아오는 시간(초)")]
        [SerializeField] float absorbTime = 0.6f;
        [SerializeField] float shakeDistance = 0.008f;
        [SerializeField] float shakeTime = 0.3f;

        MaterialPropertyBlock block;
        Vector3 restPosition;
        Vector3 flyFrom; // 구의 부모 기준
        float absorbTimer = -1f; // 진행 중이 아니면 음수
        float shakeTimer = -1f;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            restPosition = rig.localPosition;
            sphere.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            EventBus.Subscribe<CurrentColorChanged>(OnCurrentColorChanged);
            EventBus.Subscribe<ColorAbsorbed>(OnColorAbsorbed);
            EventBus.Subscribe<ColorRejected>(OnColorRejected);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<CurrentColorChanged>(OnCurrentColorChanged);
            EventBus.Unsubscribe<ColorAbsorbed>(OnColorAbsorbed);
            EventBus.Unsubscribe<ColorRejected>(OnColorRejected);
        }

        void OnCurrentColorChanged(CurrentColorChanged e)
        {
            sphere.gameObject.SetActive(e.Value != ColorValue.Zero);
            var color = ColorRules.ToColor(e.Value);
            sphere.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(EmissionColorId, color * ColorRules.GlowIntensity(e.Value, MapManager.Current.Rules.glowFactor));
            sphere.SetPropertyBlock(block);
        }

        // '지금 색 바뀜'이 먼저 와서 구는 이미 새 색이다.
        void OnColorAbsorbed(ColorAbsorbed e)
        {
            flyFrom = sphere.transform.parent.InverseTransformPoint(e.Position);
            absorbTimer = 0f;
        }

        void OnColorRejected(ColorRejected _) => shakeTimer = 0f;

        void Update()
        {
            float absorb = Advance(ref absorbTimer, absorbTime);
            float shake = Advance(ref shakeTimer, shakeTime);
            var offset = Vector3.zero;
            if (absorb >= 0f) offset.z += Mathf.Sin(absorb * Mathf.PI) * reachDistance;
            if (shake >= 0f) offset.x += Mathf.Sin(shake * 40f) * shakeDistance * (1f - shake);
            rig.localPosition = restPosition + offset;

            var slot = new Vector3(0f, Mathf.Sin(Time.time * 2f) * bobHeight, 0f);
            sphere.transform.localPosition = absorb >= 0f ? Vector3.Lerp(flyFrom, slot, Mathf.SmoothStep(0f, 1f, absorb)) : slot;
        }

        // 진행 중인 연출의 진행도(0~1)를 돌려주고 시간을 더한다. 진행 중이 아니면 -1, 끝나면 타이머를 끈다.
        static float Advance(ref float timer, float duration)
        {
            if (timer < 0f) return -1f;
            float progress = Mathf.Clamp01(timer / duration);
            timer += Time.deltaTime;
            if (progress >= 1f) timer = -1f;
            return progress;
        }
    }
}
