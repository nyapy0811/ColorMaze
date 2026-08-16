#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [개발자 전용] Build Settings의 씬 리스트를 정리한다.
///   1) Assets/Scenes 아래 있는데 리스트에 없는 씬을 전부 추가한다(활성화 상태로).
///   2) 리스트 전체를 아래 순서로 다시 정렬한다: MainMenu → UIScene → 챕터 스테이지
///      (Chapter1/1-1 ~ Chapter2/2-1처럼 "N-M" 이름을 챕터·스테이지 번호 기준 자연수 정렬) → 그 외(테스트용 등, 이름순).
/// 기존에 등록돼 있던 씬의 enabled(빌드 포함 여부) 값은 그대로 유지한다.
/// </summary>
public static class BuildSceneListOrganizer
{
    [MenuItem("ColorMaze/씬 리스트 일괄 정리 (누락 추가 + 순서 정리)")]
    static void Organize()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        var allPaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();

        var existingEnabled = EditorBuildSettings.scenes.ToDictionary(s => s.path, s => s.enabled);
        int added = allPaths.Count(p => !existingEnabled.ContainsKey(p));

        var sortedPaths = allPaths.OrderBy(GetSortKey, System.StringComparer.Ordinal).ToList();

        EditorBuildSettings.scenes = sortedPaths
            .Select(p => new EditorBuildSettingsScene(p, !existingEnabled.TryGetValue(p, out var enabled) || enabled))
            .ToArray();

        Debug.Log($"[BuildSceneListOrganizer] 씬 리스트 정리 완료 — 전체 {sortedPaths.Count}개, 새로 추가 {added}개.");
        EditorUtility.DisplayDialog("씬 리스트 정리",
            $"완료됐습니다.\n전체 {sortedPaths.Count}개, 새로 추가 {added}개.",
            "확인");
    }

    // 정렬 우선순위: MainMenu(0) → UIScene(1) → "N-M" 형식 챕터/스테이지 씬(챕터·스테이지 번호 순) → 그 외(이름순).
    static string GetSortKey(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);

        if (name == "MainMenu") return "0";
        if (name == "UIScene") return "1";

        var m = Regex.Match(name, @"^(\d+)-(\d+)$");
        if (m.Success)
        {
            int chapter = int.Parse(m.Groups[1].Value);
            int stage = int.Parse(m.Groups[2].Value);
            return $"2_{chapter:D3}_{stage:D3}";
        }

        return $"9_{name}";
    }
}
#endif
