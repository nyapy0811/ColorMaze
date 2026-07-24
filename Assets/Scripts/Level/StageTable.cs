using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 챕터별 스테이지 씬 이름을 담는 공용 데이터 애셋.
/// MainMenuController(스테이지 선택)와 ClearScreenController(다음 스테이지 계산)가
/// 같은 애셋 하나를 참조해서, 스테이지 목록이 한 곳에만 존재하도록 한다.
/// </summary>
[CreateAssetMenu(fileName = "StageTable", menuName = "ColorMaze/Stage Table")]
public class StageTable : ScriptableObject
{
    [System.Serializable]
    public class ChapterStageScenes
    {
        public string[] sceneNames = new string[10];
    }

    public ChapterStageScenes[] chapters;

    /// <summary>모든 챕터의 스테이지를 순서대로 이어붙인 목록(빈 자리도 그대로 포함). "다음 스테이지" 계산에 쓴다 —
    /// 바로 다음 자리가 비어있으면 그 자리를 건너뛰지 않고 "다음 스테이지 없음"으로 취급해야 하기 때문에 필터링하지 않는다.</summary>
    public List<string> Flattened()
    {
        var list = new List<string>();
        if (chapters == null) return list;

        foreach (var chapter in chapters)
        {
            if (chapter?.sceneNames == null) continue;
            list.AddRange(chapter.sceneNames);
        }
        return list;
    }
}
