

namespace Yujanggi.Core.Domain
{
    using Board;
    public readonly struct MoveRecord
    {
        public static MoveRecord None => new(Pos.Invalid, Pos.Invalid, PieceModel.None, PieceModel.None);
        public MoveRecord(Pos from, Pos to, PieceModel moved, PieceModel captured)
        {
            From = from; To = to;
            MovedPiece      = moved;
            CapturedPiece   = captured;
        }

        public Pos           From { get; }
        public Pos           To { get; }
        public PieceModel    MovedPiece { get; }
        public PieceModel    CapturedPiece { get; }

        public bool IsCapture => !CapturedPiece.IsNone;
       
    }
}