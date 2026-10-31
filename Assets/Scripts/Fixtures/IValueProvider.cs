namespace ColorMaze
{
    /// <summary>
    /// 조준하면 값 표시에 값을 보이는 기물(추출 전 추출 오브젝트, 작동기 계열의 요구 값).
    /// 상호작용과 나눈 이유: 다 쓴 일회용 작동기는 상호작용은 안 되지만 값은 보인다(3차 Q478).
    /// </summary>
    public interface IValueProvider
    {
        /// <summary>지금 보일 값이 있으면 true와 그 값.</summary>
        bool TryGetDisplayValue(out OrbValue value);
    }
}
