using UnityEngine;

/// <summary>
/// 오브젝트를 로컬 Y축으로 sin 파형을 따라 위아래로 둥둥 띄운다.
/// </summary>
public class FloatingBob : MonoBehaviour
{
    [Tooltip("위아래로 움직이는 폭(진폭)")]
    [SerializeField] float amplitude = 0.1f;

    [Tooltip("흔들리는 속도")]
    [SerializeField] float speed = 2f;

    Vector3 basePos;

    void Start() => basePos = transform.localPosition;

    void Update()
    {
        Vector3 p = basePos;
        p.y += Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = p;
    }
}
