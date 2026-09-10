using UnityEngine;

/// <summary>인게임 맵 에디터/런타임 로더가 쓰는 기물 프리팹 모음. MazeGenerator가 스테이지마다
/// 개별로 들고 있는 7종 프리팹과 같은 것을, 씬에 묶이지 않는 애셋 하나로 공유해서 쓴다.
/// Resources 폴더에 둬서 CustomStageLoader가 StageTable과 같은 방식(Resources.Load)으로 불러온다.</summary>
[CreateAssetMenu(fileName = "CustomStagePrefabs", menuName = "ColorMaze/Custom Stage Prefabs")]
public class CustomStagePrefabs : ScriptableObject
{
    [Header("기물 프리팹 (MazeGenerator와 동일한 7종)")]
    public GameObject colorFilterPrefab;
    public GameObject rgbFilterPrefab;
    public GameObject bucketPrefab;
    public GameObject canvasPrefab;
    public GameObject palettePrefab;
    public GameObject colorChangerPrefab;
    public GameObject stackChangerPrefab;

    [Header("일반 벽 블록 (비우면 기본 큐브를 생성)")]
    public GameObject wallBlockPrefab;

    public GameObject PrefabFor(FixtureType type) => type switch
    {
        FixtureType.ColorFilter => colorFilterPrefab,
        FixtureType.RgbFilter => rgbFilterPrefab,
        FixtureType.Bucket => bucketPrefab,
        FixtureType.Canvas => canvasPrefab,
        FixtureType.Palette => palettePrefab,
        FixtureType.ColorChanger => colorChangerPrefab,
        FixtureType.StackChanger => stackChangerPrefab,
        _ => null,
    };
}
