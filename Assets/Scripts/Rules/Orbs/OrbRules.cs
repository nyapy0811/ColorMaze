using System;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 오브 값의 규칙. 채널 한도는 채널마다 따로인 값(<see cref="OrbValue"/>)을 인자로 받는다
    /// (기본 한도는 설정 애셋에 있고, 맵 안에서 흡수로 늘어난다).
    /// </summary>
    public static class OrbRules
    {
        /// <summary>모든 채널이 0 ~ 그 채널의 한도인가.</summary>
        public static bool IsWithin(OrbValue value, OrbValue limit) =>
            value.R >= 0 && value.R <= limit.R &&
            value.G >= 0 && value.G <= limit.G &&
            value.B >= 0 && value.B <= limit.B;

        /// <summary>채널마다 0 ~ 그 채널의 한도로 자른다(음수는 0, 넘치면 한도). 합성·필터·증폭기의 결과가 모두 거친다.</summary>
        public static OrbValue Clamp(OrbValue value, OrbValue limit) => new(
            Math.Clamp(value.R, 0, limit.R),
            Math.Clamp(value.G, 0, limit.G),
            Math.Clamp(value.B, 0, limit.B));

        /// <summary>작동기·목적지의 목표로 쓸 수 있는가: 모든 채널 1 이상이고 세 채널이 모두 같지 않음. 기본 한도는 넘어도 된다.</summary>
        public static bool IsValidGoal(OrbValue value) =>
            value.R >= 1 && value.G >= 1 && value.B >= 1 && !IsAllEqual(value);

        /// <summary>추출 오브젝트의 값으로 쓸 수 있는가: 음수가 없고, (0,0,0)을 포함해 세 채널이 모두 같지 않음.</summary>
        public static bool IsValidSourceValue(OrbValue value) =>
            value.R >= 0 && value.G >= 0 && value.B >= 0 && !IsAllEqual(value);

        /// <summary>
        /// 오브의 색. 가장 큰 채널로 나눈 정규화 RGB라 오브는 늘 최대 밝기다((1,0,0)과 (2,0,0)은 같은 색).
        /// (0,0,0) 오브의 모양은 미정이라 일단 검정으로 둔다.
        /// </summary>
        public static Color ToColor(OrbValue value)
        {
            int max = value.Max;
            if (max <= 0) return Color.black;
            return new Color((float)value.R / max, (float)value.G / max, (float)value.B / max);
        }

        /// <summary>
        /// 흑백 색(추출한 오브젝트). 채도만 0으로 내리고 밝기는 세 채널의 평균이라,
        /// 순수한 빨강·초록·파랑이 모두 같은 회색이 된다(밝기 가중이 아님, 코어 루프 4단계).
        /// </summary>
        public static Color ToGrayscale(Color color)
        {
            float gray = (color.r + color.g + color.b) / 3f;
            return new Color(gray, gray, gray, color.a);
        }

        /// <summary>발광 세기. 가장 큰 채널에 비례하고 계수는 설정 애셋에서 받는다.</summary>
        public static float GlowIntensity(OrbValue value, float factor) => value.Max * factor;

        static bool IsAllEqual(OrbValue value) => value.R == value.G && value.G == value.B;
    }
}
