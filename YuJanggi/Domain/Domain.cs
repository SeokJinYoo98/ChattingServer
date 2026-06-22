using System;
using System.Collections;
using System.Collections.Generic;

namespace Yujanggi.Core.Domain
{
    public interface IGameResultContext
    {
        GameResultInfo? GameResult { get; }
    }
    public interface IGameInputReceiver
    {
        void RequestMove(Pos from, Pos to);
        void ChangeSelection(int? pieceId, IReadOnlyList<Pos> legal, IReadOnlyList<Pos> illegal);
    }
    public interface ISessionTransition
    {
        void ToLive();
        void ToReplay();
        void ToEnd();
        void ToEndReplay();
    }
    public struct GameSessionInfo
    {
        public GameModeType Mode;
        public PlayerType Cho;
        public Formation ChoFormation;
        public PlayerType Han;
        public Formation HanFormation;
        public float TurnTime;
    }
    public static class GameSessionStore
    {
        public static GameSessionInfo Current;
    }

    public enum Formation { HEHE, EHEH, EHHE, HEEH }
    public enum PlayerType { Local, AI, Network }
    public enum GameModeType { Local, AI, Network }

    
    public interface IInputHandler
    {
        public event Action<Pos> OnBoardClicked;
        public event Action      OnEmptyClicked;
        public void RotateCamera(PlayerTeam team);
        public void Activate();
        public void Deactivate();
    }
    public interface IAIController
    {

    }
    public interface ILocalPlayer
    {
        public event Action<int?, IReadOnlyList<Pos>, IReadOnlyList<Pos>> OnSelectionChanged;
    }

    public interface IPlayerController
    {
        public event Action<Pos, Pos> OnMoveRequest;
        public PlayerTeam Team { get; }
        public bool IsLocal();
        public void BeginTurn();
        public void EndTurn();
        public void BindEvents(IGameInputReceiver receiver);
        public void UnBindEvents(IGameInputReceiver receiver);
    }

    public enum             PlayerTeam
    { Cho, Han, None }
    public enum             PieceType
    {
        King,       // 궁
        Chariot,    // 차
        Cannon,     // 포
        Horse,      // 마
        Elephant,   // 상
        Guard,      // 사
        Soldier,    // 졸/병
        None
    }


    public readonly struct Pos : IEquatable<Pos>
    {
        public Pos(int x, int z)
        {
            this.X = x;
            this.Z = z;
        }
        public readonly int X;
        public readonly int Z;

        public static Pos operator +(Pos a, Pos b)
            => new Pos(a.X + b.X, a.Z + b.Z);
        public static Pos operator -(Pos a, Pos b)
            => new Pos(a.X - b.X, a.Z - b.Z);
        public static bool operator ==(Pos a, Pos b)
            => a.X == b.X && a.Z == b.Z;
        public static bool operator !=(Pos a, Pos b)
            => !(a == b);
        public bool Equals(Pos other)
            => X == other.X && Z == other.Z;
        public override bool Equals(object obj)
            => obj is Pos other && Equals(other);
        public override int GetHashCode()
            => HashCode.Combine(X, Z);
        public override string ToString()
        {
            return $"({X}, {Z})";
        }
        public static readonly Pos Up = new Pos(+0, +1);
        public static readonly Pos Down = new Pos(+0, -1);
        public static readonly Pos Left = new Pos(-1, +0);
        public static readonly Pos Right = new Pos(+1, +0);
        public static readonly Pos LeftUp = new Pos(-1, +1);
        public static readonly Pos RightUp = new Pos(+1, +1);
        public static readonly Pos LeftDown = new Pos(-1, -1);
        public static readonly Pos RightDown = new Pos(+1, -1);
        public static readonly Pos Invalid = new Pos(-100, -100);
    }
    

    public enum GameResult
    {
        Draw,
        CheckMate,
        GiveUp,
        Score
    }
    public struct GameResultInfo
    {
        public int          MoveCnt;
        public GameResult   Type;
        public PlayerTeam   Loser;
    }

}