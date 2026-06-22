using System.Collections.Generic;
using Yujanggi.Core.Board;

namespace Yujanggi.Core.Domain
{
    public readonly struct SelectionInfo
    {
        public SelectionInfo(PieceModel piece, Pos pos)
        {
            Piece = piece;
            Pos = pos;
        }

        public PieceModel Piece { get; }
        public Pos Pos { get; }
    }

    public class Selection
    {
        private readonly HashSet<Pos> _legalSet       = new(20);
        private readonly List<Pos>    _legalCells     = new(20);
        private readonly List<Pos>    _illLegalCells  = new(20);
        public IReadOnlyList<Pos> LegalCells   => _legalCells;
        public IReadOnlyList<Pos> IllegalCells => _illLegalCells;
        public bool               HasSelection => FromPos != Pos.Invalid;
        public Pos                FromPos = Pos.Invalid;
        public void Clear()
        {
            _legalSet.Clear();
            _legalCells.Clear();
            _illLegalCells.Clear();
        }
        public void SetMovable(List<Pos> legalCells, List<Pos> illegalCells)
        {
            Clear();
            foreach (var pos in legalCells)
            {
                _legalCells.Add(pos);
                _legalSet.Add(pos);
            }

            _illLegalCells.AddRange(illegalCells);
        }
        public bool IsMovable(Pos pos) => _legalSet.Contains(pos);
    }

    public readonly struct MoveContext
    {
        public static MoveContext Handicap => new(MoveRecord.None, false, false);
        public MoveContext(MoveRecord record, bool isJanggun, bool isEnd)
        {
            Record     = record;
            MovePlayer = record.MovedPiece.Team;

            IsJanggun   = isJanggun;
            EndGame     = isEnd;
        }
        public MoveRecord   Record { get; }

        public bool         IsJanggun { get; }
        public bool         EndGame { get; }
        public PlayerTeam   MovePlayer { get; }
        public PlayerTeam   NextPlayer => MovePlayer == PlayerTeam.Cho ? PlayerTeam.Han : PlayerTeam.Cho;

        public bool IsCapture => Record.IsCapture;
        public bool IsHandicap
            => Record.Equals(MoveRecord.None);
    }
}