using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMaze
{
    /// <summary>
    /// 맵 목록(애셋은 Assets/Data, 3차 Q487·Q488). 목록 순서가 맵 선택 화면의 순서이자 '다음 맵' 순서다.
    /// 맵 매니저는 자기 씬 이름으로 자기 항목을 찾는다.
    /// </summary>
    [CreateAssetMenu(menuName = "ColorMaze/맵 목록", fileName = "MapList")]
    public class MapList : ScriptableObject
    {
        public List<MapListEntry> maps = new();

        public MapListEntry FindByScene(string sceneName) => maps.Find(entry => entry.sceneName == sceneName);
    }

    [Serializable]
    public class MapListEntry
    {
        [Tooltip("맵 id. 기록을 이 고정 문자열로 저장하므로 한 번 정하면 바꾸지 않는다(씬 이름을 바꿔도 기록이 이어짐).")]
        public string mapId;

        [Tooltip("맵 씬 이름 (Build Settings에 있어야 함)")]
        public string sceneName;

        [Tooltip("맵 선택 화면에 보일 이름")]
        public string displayName;

        public Sprite thumbnail;

        [Tooltip("맵 버전. 퍼즐에 영향을 주는 수정을 할 때만 올린다. 저장된 체크포인트의 버전과 다르면 그 기록을 버린다.")]
        public int version = 1;
    }
}
