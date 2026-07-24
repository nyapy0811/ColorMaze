/// <summary>
/// 기물이 R/G/B 값을 텍스트 라벨로 보여줄 때 쓰는 공용 형식.
/// 판정 방식에 따라 형식이 다르다:
///  - 정숫값 자체로 판정하는 기물(예: 캔버스) → "R G B" (공백 구분)
///  - 비율(변환 색)로 판정하는 기물(예: 필터) → "R:G:B" (콜론 구분)
/// 둘 다 숫자를 해당 색으로 물들인 리치 텍스트를 쓴다.
/// </summary>
public static class StackLabelFormat
{
    public static string ByValue(int red, int green, int blue) =>
        $"<color=#FF0000>{red}</color> <color=#00FF00>{green}</color> <color=#0000FF>{blue}</color>";

    public static string ByRatio(int red, int green, int blue) =>
        $"<color=#FF0000>{red}</color><color=#000000>:</color><color=#00FF00>{green}</color><color=#000000>:</color><color=#0000FF>{blue}</color>";
}
