namespace ColorMaze
{
    /// <summary>제출 팝업에서 오브를 누른 것이 거부된 이유. 지금은 모두 흔들림 하나지만 소리·연출을 나눌 때 쓴다.</summary>
    public enum SubmitRejectReason
    {
        /// <summary>판정이 맞지 않음.</summary>
        WrongValue,
        /// <summary>제출 슬롯에 이미 오브가 있음.</summary>
        SlotFull,
        /// <summary>소지 오브가 가득 차 회수할 수 없음.</summary>
        InventoryFull,
        /// <summary>회수하면 닫힐 문 계열의 막는 자리에 플레이어가 있음.</summary>
        GateOccupied,
    }
}
