using Framework.Core;

namespace ColorMaze
{
    /// <summary>제출 팝업에서 넣기·되돌리기·회수를 함. 보내는 곳: 작동기 계열. 받는 곳: 소리 연결.</summary>
    public readonly struct SubmitAttempted : IEvent
    {
        public readonly bool Accepted;

        /// <summary>거부 이유. 받아들였으면 기본값이다.</summary>
        public readonly SubmitRejectReason Reason;

        public SubmitAttempted(bool accepted, SubmitRejectReason reason)
        {
            Accepted = accepted;
            Reason = reason;
        }
    }
}
