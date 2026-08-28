#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [개발자 전용] 메인메뉴 스테이지 미리보기용 썸네일을, 씬을 직접 열어서 카메라로 찍은 스크린샷으로 만든다.
/// 모든 스테이지 씬에 공통으로 쓰는 고정 카메라 Transform(Position 10,10,-8 / Rotation 20,-45,0)으로
/// 16:9 비율(1280x720) PNG를 찍어서 Assets/Textures/StageThumbnails/Chapter{N}/{씬이름}.png 에 저장하고,
/// Sprite로 임포트한 뒤 Assets/Resources/StageTable.asset의 해당 챕터/스테이지 슬롯에 자동으로 연결한다.
/// 스테이지 씬 자체에는 플레이어/뷰모델/HUD 오브젝트가 없으므로 Culling Mask는 Everything으로 둔다.
/// </summary>
public static class StageThumbnailCapturer
{
    static readonly Vector3 CameraPosition = new Vector3(10, 10, -8);
    static readonly Vector3 CameraEuler = new Vector3(20, -45, 0);
    const int Width = 1280;
    const int Height = 720; // 16:9

    const string StageTablePath = "Assets/Resources/StageTable.asset";
    const string ThumbnailRoot = "Assets/Textures/StageThumbnails";

    [MenuItem("ColorMaze/스테이지 썸네일 촬영/전체 씬 촬영")]
    static void CaptureAll()
    {
        var stageTable = AssetDatabase.LoadAssetAtPath<StageTable>(StageTablePath);
        if (stageTable == null || stageTable.chapters == null)
        {
            EditorUtility.DisplayDialog("스테이지 썸네일 촬영", $"{StageTablePath} 를 찾을 수 없습니다.", "확인");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; // 취소하면 중단

        // 촬영할 목록을 미리 다 뽑아둔다 — 루프 도중 씬 전환/에셋 재임포트를 반복하면
        // 처음 로드해둔 stageTable 참조가 중간에 끊길 수 있어서(MissingReferenceException),
        // 이후로는 StageTable을 다시 참조하지 않고 매번 새로 로드해서 쓴다.
        var entries = new System.Collections.Generic.List<(int chapterIndex, int stageIndex, string sceneName)>();
        for (int chapterIndex = 0; chapterIndex < stageTable.chapters.Length; chapterIndex++)
        {
            var sceneNames = stageTable.chapters[chapterIndex]?.sceneNames;
            if (sceneNames == null) continue;
            for (int stageIndex = 0; stageIndex < sceneNames.Length; stageIndex++)
            {
                string sceneName = sceneNames[stageIndex];
                if (!string.IsNullOrEmpty(sceneName))
                    entries.Add((chapterIndex, stageIndex, sceneName));
            }
        }

        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        int captured = 0, skipped = 0;
        try
        {
            foreach (var (chapterIndex, stageIndex, sceneName) in entries)
            {
                string scenePath = $"Assets/Scenes/Chapter{chapterIndex + 1}/{sceneName}.unity";
                if (!File.Exists(scenePath)) { skipped++; continue; }

                EditorUtility.DisplayProgressBar("스테이지 썸네일 촬영", sceneName, captured / (float)entries.Count);

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                CaptureCurrentlyOpenScene(sceneName, chapterIndex, stageIndex);
                captured++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("스테이지 썸네일 촬영",
            $"완료됐습니다.\n촬영 {captured}개, 씬 파일 없어서 건너뜀 {skipped}개.", "확인");
    }

    [MenuItem("ColorMaze/스테이지 썸네일 촬영/현재 씬 촬영")]
    static void CaptureCurrent()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        string sceneName = activeScene.name;
        var m = Regex.Match(sceneName, @"^(\d+)-(\d+)$");
        if (!m.Success)
        {
            EditorUtility.DisplayDialog("스테이지 썸네일 촬영",
                $"현재 씬 '{sceneName}'은 'N-M' 형식의 스테이지 씬이 아닙니다.", "확인");
            return;
        }

        var stageTable = AssetDatabase.LoadAssetAtPath<StageTable>(StageTablePath);
        if (stageTable == null || stageTable.chapters == null)
        {
            EditorUtility.DisplayDialog("스테이지 썸네일 촬영", $"{StageTablePath} 를 찾을 수 없습니다.", "확인");
            return;
        }

        int chapterIndex = int.Parse(m.Groups[1].Value) - 1;
        int stageIndex = int.Parse(m.Groups[2].Value) - 1;
        if (chapterIndex < 0 || chapterIndex >= stageTable.chapters.Length)
        {
            EditorUtility.DisplayDialog("스테이지 썸네일 촬영", $"'{sceneName}'에 해당하는 챕터가 StageTable에 없습니다.", "확인");
            return;
        }

        CaptureCurrentlyOpenScene(sceneName, chapterIndex, stageIndex);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("스테이지 썸네일 촬영", $"'{sceneName}' 썸네일 촬영 완료.", "확인");
    }

    /// <summary>현재 에디터에 열려있는 씬을 고정 카메라로 찍어서 PNG로 저장하고, Sprite로 임포트한 뒤
    /// StageTable의 해당 슬롯에 연결한다. (씬을 열거나 저장하지는 않음 — 호출하는 쪽 책임.)
    /// StageTable은 매번 새로 로드해서 쓴다 — 반복 호출 도중 이전에 로드해둔 참조가 끊길 수 있어서다.</summary>
    static void CaptureCurrentlyOpenScene(string sceneName, int chapterIndex, int stageIndex)
    {
        // 필터(ColorFilter/RGBFilter)의 병합 메시·색은 평소 Start()(플레이 모드) 또는 인스펙터에서
        // 값을 바꿀 때 OnValidate()로만 다시 만들어진다. 씬을 열기만 하고 아무 것도 안 건드리면
        // 이 갱신이 한 번도 안 일어나서 필터가 색 없이(기본 머티리얼로) 찍힌다 — 촬영 전에 강제로 한 번 재생성한다.
        FilterBlockBase.RebuildAll();

        var camGo = new GameObject("~ThumbnailCaptureCamera");
        var cam = camGo.AddComponent<Camera>();
        camGo.transform.position = CameraPosition;
        camGo.transform.rotation = Quaternion.Euler(CameraEuler);
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.cullingMask = ~0; // 스테이지 씬에는 플레이어/뷰모델/HUD가 없으므로 전부 렌더링
        cam.fieldOfView = 60;

        var rt = new RenderTexture(Width, Height, 24);
        cam.targetTexture = rt;
        cam.Render();

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);

        string dir = $"{ThumbnailRoot}/Chapter{chapterIndex + 1}";
        Directory.CreateDirectory(dir);
        string pngPath = $"{dir}/{sceneName}.png";
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);

        var stageTable = AssetDatabase.LoadAssetAtPath<StageTable>(StageTablePath);
        if (stageTable == null || stageTable.chapters == null || chapterIndex >= stageTable.chapters.Length) return;
        var thumbnails = stageTable.chapters[chapterIndex].thumbnails;
        if (thumbnails == null || stageIndex >= thumbnails.Length) return;

        thumbnails[stageIndex] = sprite;
        EditorUtility.SetDirty(stageTable);
        AssetDatabase.SaveAssets(); // 캡처마다 바로 저장 — 참조를 오래 들고 있지 않는다
    }
}
#endif
