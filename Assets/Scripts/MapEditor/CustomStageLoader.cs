using System.Collections.Generic;
using Framework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CustomStageData를 실제 기물 프리팹으로 Instantiate해서 재생 가능한 스테이지를 만든다.
/// 기존 스테이지가 MazeGeneratorEditor로 씬에 직접 저장하는 것과 동일한 결과(진짜 컴포넌트로 구성된
/// MazeGenerator)를 런타임에 만들어내므로, LevelManager/StageGuideController/필터 병합 등 기존 게임
/// 로직은 전혀 손대지 않고 그대로 재사용된다.
/// </summary>
public static class CustomStageLoader
{
    /// <summary>data를 Instantiate해서 MazeGenerator를 반환한다.
    /// 블록/기물의 y는 그리드 정수로 저장돼 있고, 기존 칸 중심 규칙(x,z=정수, y=정수+0.5)과 똑같이
    /// +0.5를 여기서 더한다. 버킷은 기존 개발자용 에디터와 동일하게 다른 기물보다 0.5 낮게 설치한다.
    /// 완료 후 SceneLoadCompleted를 발행해, LevelManager/StageGuideController/필터 병합/미리보기 등
    /// 기존에 그 이벤트로 초기화되는 시스템이 전부 자연스럽게 같이 초기화되게 한다.</summary>
    public static MazeGenerator Load(CustomStageData data, CustomStagePrefabs prefabs, Transform parent = null)
    {
        var root = new GameObject($"CustomStage_{data.title}");
        if (parent != null) root.transform.SetParent(parent, false);

        var mazeRoot = new GameObject("Maze");
        mazeRoot.transform.SetParent(root.transform, false);

        foreach (var block in data.blocks)
            PlaceBlock(block, prefabs, mazeRoot.transform);

        var mapObjectsRoot = new GameObject("MapObjects");
        mapObjectsRoot.transform.SetParent(root.transform, false);

        var byId = new Dictionary<int, MapObjectBase>();
        foreach (var fixture in data.fixtures)
        {
            var instance = PlaceFixture(fixture, prefabs, mapObjectsRoot.transform);
            if (instance != null) byId[fixture.id] = instance;
        }

        var maze = root.AddComponent<MazeGenerator>();
        FillCorrectOrder(maze.correctOrder1, data.correctOrder1FixtureIds, byId);
        FillCorrectOrder(maze.correctOrder2, data.correctOrder2FixtureIds, byId);

        FilterBlockBase.RebuildAll();

        EventBus.Publish(new SceneLoadCompleted { SceneName = SceneManager.GetActiveScene().name });

        return maze;
    }

    /// <summary>블록 하나를 Instantiate한다(MapEditController가 개별 배치할 때도 재사용).</summary>
    public static GameObject PlaceBlock(BlockEntry block, CustomStagePrefabs prefabs, Transform parent)
    {
        Vector3 pos = new Vector3(block.x, block.y + 0.5f, block.z);

        GameObject go;
        if (prefabs.wallBlockPrefab != null)
        {
            go = Object.Instantiate(prefabs.wallBlockPrefab, pos, Quaternion.identity, parent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
        }
        go.name = "Block";
        return go;
    }

    /// <summary>기물 하나를 Instantiate하고 파라미터를 적용한다(MapEditController가 개별 배치할 때도 재사용).</summary>
    public static MapObjectBase PlaceFixture(FixtureEntry fixture, CustomStagePrefabs prefabs, Transform parent)
    {
        var prefab = prefabs.PrefabFor(fixture.type);
        if (prefab == null) return null;

        Vector3 pos = new Vector3(fixture.x, fixture.y + 0.5f, fixture.z);
        if (fixture.type == FixtureType.Bucket) pos += new Vector3(0f, -0.5f, 0f);

        var go = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
        var instance = go.GetComponent<MapObjectBase>();
        ApplyParams(instance, fixture);
        return instance;
    }

    static void ApplyParams(MapObjectBase instance, FixtureEntry fixture)
    {
        switch (instance)
        {
            case ColorFilterBlock f: f.Configure(fixture.paramR, fixture.paramG, fixture.paramB); break;
            case RgbFilterBlock f: f.Configure(fixture.paramColorA); break;
            case Bucket f: f.Configure(fixture.paramColorA); break;
            case ColorPalette f: f.Configure(fixture.paramR, fixture.paramG, fixture.paramB); break;
            case StackChanger f: f.Configure(fixture.paramColorA, fixture.paramColorB); break;
            case ColorCanvas f: f.Configure(fixture.paramR, fixture.paramG, fixture.paramB); break;
            // ColorChanger는 파라미터가 없어서 별도 처리 불필요
        }
    }

    static void FillCorrectOrder(List<MapObjectBase> target, List<int> ids, Dictionary<int, MapObjectBase> byId)
    {
        foreach (var id in ids)
            if (byId.TryGetValue(id, out var obj))
                target.Add(obj);
    }
}
