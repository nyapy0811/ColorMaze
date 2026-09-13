using System;
using System.Collections.Generic;

/// <summary>인게임 맵 에디터로 만들 수 있는 기물 종류.</summary>
public enum FixtureType
{
    ColorFilter,
    RgbFilter,
    Bucket,
    Canvas,
    Palette,
    ColorChanger,
    StackChanger,
}

/// <summary>일반 벽 블록 하나의 그리드 좌표.</summary>
[Serializable]
public class BlockEntry
{
    public int x, y, z;
}

/// <summary>기물 하나의 배치 정보. paramR/G/B, paramColorA/B는 타입에 따라 쓰는 것만 의미가 있다
/// (예: Canvas는 paramR/G/B만, RgbFilter는 paramColorA만 사용).</summary>
[Serializable]
public class FixtureEntry
{
    /// <summary>이 스테이지 안에서만 유효한 고유 id. correctOrder1/2FixtureIds가 리스트 인덱스 대신
    /// 이 id를 참조한다 — 편집 중 기물이 추가/삭제/재배치돼도 정답 순서 참조가 깨지지 않는다.</summary>
    public int id;
    public FixtureType type;
    public int x, y, z;

    // ColorFilter(R/G/B) · Palette(증가시킬 R/G/B) · Canvas(목표 R/G/B)
    public int paramR, paramG, paramB;

    // RgbFilter(목표 색 = paramColorA) · Bucket(0으로 만들 색 = paramColorA) ·
    // StackChanger(교환할 색 A/B = paramColorA/paramColorB)
    public LightColor paramColorA, paramColorB;
}

/// <summary>캔버스(Canvas 기물) 하나에 대응하는 정답 순서 하나.</summary>
[Serializable]
public class CanvasOrderEntry
{
    /// <summary>이 순서가 속한 Canvas 기물의 id(FixtureEntry.id).</summary>
    public int canvasFixtureId;
    /// <summary>fixtures의 id를 순서대로 참조하는 정답 순서 목록.</summary>
    public List<int> orderFixtureIds = new();
}

/// <summary>플레이어가 인게임 맵 에디터로 만든 스테이지 하나의 전체 데이터. JsonUtility로 직렬화한다
/// (Dictionary는 지원하지 않으므로 전부 List 기반).</summary>
[Serializable]
public class CustomStageData
{
    /// <summary>GUID. 나중에 내보내기/서버 공유로 확장해도 다른 유저의 맵과 충돌하지 않게.</summary>
    public string id;
    public string title;

    public List<BlockEntry> blocks = new();
    public List<FixtureEntry> fixtures = new();

    /// <summary>캔버스(FixtureType.Canvas)마다 하나씩 자동으로 생기는 정답 순서(가이드 기능용).
    /// 배치 순서대로 쌓이며 최대 7개(무지개 7색 마커 한도) — MapEditController가 캔버스 배치 개수를
    /// 그 한도로 제한한다.</summary>
    public List<CanvasOrderEntry> canvasOrders = new();
}
