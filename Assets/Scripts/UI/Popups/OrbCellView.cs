using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorMaze
{
    /// <summary>
    /// 오브 칸(공통 UI 부품, 8.2): 색 원과 검은 테두리(고르면 굵어짐), 마우스를 올리면 뜨는 값 상자, 클릭, 거부될 때의 흔들림.
    /// 합성 화면, 제출 화면, 인벤토리 창이 같이 쓴다. 끌어다 놓기는 흡수를 만들 때 더한다.
    /// </summary>
    public class OrbCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("오브 색을 칠하는 원")]
        [SerializeField] Image fill;

        [Tooltip("원 뒤의 검은 원. 칸 크기에 맞춰 늘어나고(앵커 꽉 채움) 바깥으로 삐져나온 만큼이 테두리다.")]
        [SerializeField] RectTransform ring;

        [Tooltip("흔드는 부분(원과 테두리). 칸 자체는 줄 배치가 움직이므로 안쪽만 흔든다.")]
        [SerializeField] RectTransform visual;

        [SerializeField] OrbValueLabel valueLabel;
        [SerializeField] float ringWidth = 3f;
        [SerializeField] float selectedRingWidth = 8f;
        [SerializeField] float shakeDuration = 0.35f;
        [SerializeField] float shakeDistance = 8f;

        OrbValue value;
        Action clicked;

        /// <summary>그릴 값과 고름 여부, 눌렀을 때 할 일.</summary>
        public void Set(OrbValue orbValue, bool selected, Action onClick)
        {
            value = orbValue;
            clicked = onClick;
            fill.color = OrbRules.ToColor(orbValue);
            float width = selected ? selectedRingWidth : ringWidth;
            ring.sizeDelta = new Vector2(width * 2f, width * 2f);
            valueLabel.Hide();
        }

        /// <summary>거부: 제자리에서 좌우로 흔들린다(그림 4).</summary>
        public void Shake()
        {
            StopAllCoroutines();
            StartCoroutine(Shaking());
        }

        IEnumerator Shaking()
        {
            for (float t = 0f; t < shakeDuration; t += Time.unscaledDeltaTime)
            {
                float fade = 1f - t / shakeDuration;
                visual.anchoredPosition = new Vector2(Mathf.Sin(t * 60f) * shakeDistance * fade, 0f);
                yield return null;
            }
            visual.anchoredPosition = Vector2.zero;
        }

        public void OnPointerEnter(PointerEventData eventData) => valueLabel.Show(value);

        public void OnPointerExit(PointerEventData eventData) => valueLabel.Hide();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) clicked?.Invoke();
        }
    }
}
