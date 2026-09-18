using UnityEngine;

public partial class MapEditController
{
    // 모드 탭(Place/ValueEdit/SaveLoad/Play) 패널 전환 + 탭 하이라이트 전담.
    class TabController
    {
        readonly MapEditController owner;

        public TabController(MapEditController owner) => this.owner = owner;

        /// <summary>Mode1 버튼 OnClick — 배치 모드 탭을 보여준다.</summary>
        public void ShowPlaceTab()
        {
            owner.SelectBlockTool();
            SetActivePanel(owner.mode1Panel);
            UpdateModeTabHighlight(0);
        }

        /// <summary>Mode2 버튼 OnClick — 값 수정 모드 탭을 보여준다.</summary>
        public void ShowValueEditTab()
        {
            owner.fixtureEditController.SelectEditTool();
            SetActivePanel(owner.mode2Panel);
            UpdateModeTabHighlight(1);
        }

        /// <summary>Mode4 버튼 OnClick — 카메라 이동만 가능한 저장/불러오기 탭을 보여준다.</summary>
        public void ShowSaveLoadTab()
        {
            owner.mode = EditorMode.SaveLoad;
            owner.fixtureEditController.ResetEditingSelection();
            owner.fixtureEditController.HideParamPanels();
            owner.fixtureEditController.ClearMarks();
            SetActivePanel(owner.mode4Panel);
            UpdateModeTabHighlight(3);
        }

        /// <summary>Mode5 버튼 OnClick — 카메라 이동만 가능한 플레이 테스트 탭을 보여준다. 안쪽 시작
        /// 버튼(StartPlayTest)을 눌러야 비로소 실제 플레이 테스트가 시작된다.</summary>
        public void ShowPlayTab()
        {
            owner.mode = EditorMode.PlayView;
            owner.fixtureEditController.ResetEditingSelection();
            owner.fixtureEditController.HideParamPanels();
            owner.fixtureEditController.ClearMarks();
            SetActivePanel(owner.mode5Panel);
            UpdateModeTabHighlight(4);
        }

        void SetActivePanel(GameObject panel)
        {
            if (owner.mode1Panel != null) owner.mode1Panel.SetActive(panel == owner.mode1Panel);
            if (owner.mode2Panel != null) owner.mode2Panel.SetActive(panel == owner.mode2Panel);
            if (owner.mode4Panel != null) owner.mode4Panel.SetActive(panel == owner.mode4Panel);
            if (owner.mode5Panel != null) owner.mode5Panel.SetActive(panel == owner.mode5Panel);
        }

        void UpdateModeTabHighlight(int index)
        {
            if (owner.modeTabImages == null) return;
            for (int i = 0; i < owner.modeTabImages.Length; i++)
                if (owner.modeTabImages[i] != null)
                    owner.modeTabImages[i].color = (i == index) ? owner.tabSelectedColor : owner.tabNormalColor;
        }
    }
}
