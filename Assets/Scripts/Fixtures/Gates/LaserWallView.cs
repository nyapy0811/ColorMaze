using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 레이저 벽의 겉모습(그림 1): 꺼질 때 레이저 면이 아래에서부터 위로 올라가며 천장의 발사 장치 쪽으로 사라지고,
    /// 켜질 때 다시 내려온다. 노드 시스템이 맞춰 둔 레이저 면의 위치·크기(켜짐)를 기준으로 한다.
    /// 레이저 색은 미정이라 지금은 재질 색을 그대로 쓴다.
    /// </summary>
    public class LaserWallView : MonoBehaviour
    {
        [SerializeField] Transform beam;

        [Tooltip("다 꺼지거나 켜지는 데 걸리는 시간(초)")]
        [SerializeField] float duration = 0.4f;

        Renderer beamRenderer;
        float height;
        float top;
        float openness;
        float target;

        void Awake()
        {
            beamRenderer = beam.GetComponent<Renderer>();
            height = beam.localScale.y;
            top = beam.localPosition.y + height / 2f;
            enabled = false;
        }

        public void SetOpen(bool open)
        {
            target = open ? 1f : 0f;
            enabled = true;
        }

        void Update()
        {
            openness = Mathf.MoveTowards(openness, target, Time.deltaTime / duration);
            float visible = height * (1f - openness);
            var scale = beam.localScale;
            scale.y = Mathf.Max(visible, 0.001f);
            beam.localScale = scale;
            var position = beam.localPosition;
            position.y = top - scale.y / 2f;
            beam.localPosition = position;
            beamRenderer.enabled = openness < 1f;
            if (openness == target) enabled = false;
        }
    }
}
