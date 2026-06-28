namespace Yujanggi.Core.Board
{
    using Domain;
    using System;

    public class CellData
    {
        public CellData(bool palace = false, PlayerTeam team=PlayerTeam.None, PieceType type=PieceType.None)
        {
            Palace = palace;
            Piece  = PieceModel.None;
        }
        public bool          Palace;
        public PieceModel    Piece;
    }
    public readonly struct PieceModel
    {
        public static PieceModel None
            => new(PieceType.None, PlayerTeam.None, -1);
        //public static bool operator ==(PieceModel a, PieceModel b)
        //    => a.Type == b.Type && a.Team == b.Team;
        //public static bool operator !=(PieceModel a, PieceModel b)
        //    => !(a == b);
        //public override bool Equals(object obj)
        //    => obj is PieceModel other && this == other;
        //public override int GetHashCode()
        //    => HashCode.Combine(Type, Team);

        public readonly PlayerTeam Team;
        public readonly PieceType  Type;
        public readonly int        Id;
        public bool IsNone => Type == PieceType.None;
        public PieceModel(PieceType type, PlayerTeam team, int id)
        {
            Type = type;
            Team = team;
            Id = id;
        }
    }
}