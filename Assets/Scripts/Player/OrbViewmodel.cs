using System.Collections.Generic;
using Framework.Core;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 뷰모델: 화면 오른쪽 아래의 손과 그 위에 떠 있는 소지 오브(소지 오브 HUD를 겸함, 빈칸은 안 보임). 손과 구는 임시 모양이다.
    /// '소지 오브 바뀜'을 받아 구를 다시 칠한다(색은 정규화 RGB, 발광은 가장 큰 채널에 비례).
    /// '추출함'을 받으면 손을 뻗었다 돌아오고 새 구가 오브젝트 쪽에서 날아와 자리에 든다.
    /// '추출 불가'를 받으면 손과 구가 짧게 흔들린다(추출 불가 효과, 임시).
    /// 뷰모델 레이어만 그리는 오버레이 카메라가 따로 있어 벽에 붙어도 손이 벽에 묻히지 않는다.
    /// </summary>
    public class OrbViewmodel : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("손과 구를 함께 움직이는 부분(뻗기·흔들기)")]
        [SerializeField] Transform rig;

        [Tooltip("구들이 도는 중심(손 위)")]
        [SerializeField] Transform orbAnchor;

        [Tooltip("소지 오브 하나의 모양(임시 구). 발광이 켜진 재질이어야 한다.")]
        [SerializeField] Renderer orbPrefab;

        [Header("움직임")]
        [SerializeField] float orbitRadius = 0.035f;
        [Tooltip("구들이 손 위를 도는 속도(도/초)")]
        [SerializeField] float orbitSpeed = 30f;
        [SerializeField] float bobHeight = 0.004f;
        [SerializeField] float reachDistance = 0.06f;
        [Tooltip("손 뻗기와 구가 날아오는 시간(초)")]
        [SerializeField] float extractTime = 0.6f;
        [SerializeField] float shakeDistance = 0.008f;
        [SerializeField] float shakeTime = 0.3f;

        readonly List<Renderer> orbs = new();
        MaterialPropertyBlock block;
        Vector3 restPosition;
        Vector3 flyFrom; // 구들이 도는 중심 기준
        float extractTimer = -1f; // 진행 중이 아니면 음수
        float shakeTimer = -1f;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            restPosition = rig.localPosition;
        }

        void OnEnable()
        {
            EventBus.Subscribe<InventoryChanged>(OnInventoryChanged);
            EventBus.Subscribe<OrbExtracted>(OnOrbExtracted);
            EventBus.Subscribe<ExtractRejected>(OnExtractRejected);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<InventoryChanged>(OnInventoryChanged);
            EventBus.Unsubscribe<OrbExtracted>(OnOrbExtracted);
            EventBus.Unsubscribe<ExtractRejected>(OnExtractRejected);
        }

        void OnInventoryChanged(InventoryChanged e)
        {
            var items = e.Inventory.Orbs.Items;
            while (orbs.Count < items.Count) orbs.Add(Instantiate(orbPrefab, orbAnchor));
            while (orbs.Count > items.Count)
            {
                Destroy(orbs[^1].gameObject);
                orbs.RemoveAt(orbs.Count - 1);
            }

            float glowFactor = MapManager.Current.Rules.glowFactor;
            for (int i = 0; i < items.Count; i++)
            {
                var color = OrbRules.ToColor(items[i].Value);
                orbs[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(EmissionColorId, color * OrbRules.GlowIntensity(items[i].Value, glowFactor));
                orbs[i].SetPropertyBlock(block);
            }
        }

        // '소지 오브 바뀜'이 먼저 와서 새 구는 이미 끝에 있다.
        void OnOrbExtracted(OrbExtracted e)
        {
            flyFrom = orbAnchor.InverseTransformPoint(e.Position);
            extractTimer = 0f;
        }

        void OnExtractRejected(ExtractRejected _) => shakeTimer = 0f;

        void Update()
        {
            float extract = Advance(ref extractTimer, extractTime);
            float shake = Advance(ref shakeTimer, shakeTime);
            var offset = Vector3.zero;
            if (extract >= 0f) offset.z += Mathf.Sin(extract * Mathf.PI) * reachDistance;
            if (shake >= 0f) offset.x += Mathf.Sin(shake * 40f) * shakeDistance * (1f - shake);
            rig.localPosition = restPosition + offset;

            for (int i = 0; i < orbs.Count; i++)
            {
                float angle = (360f * i / orbs.Count + Time.time * orbitSpeed) * Mathf.Deg2Rad;
                var slot = new Vector3(Mathf.Cos(angle) * orbitRadius, Mathf.Sin(Time.time * 2f + i) * bobHeight, Mathf.Sin(angle) * orbitRadius);
                bool flying = extract >= 0f && i == orbs.Count - 1;
                orbs[i].transform.localPosition = flying ? Vector3.Lerp(flyFrom, slot, Mathf.SmoothStep(0f, 1f, extract)) : slot;
            }
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
