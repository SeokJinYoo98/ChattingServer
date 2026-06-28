using System.Collections.Generic;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Match.Movement
{
    public enum Step
    { Right, Left, Up, Down, RightUp, RightDown, LeftUp, LeftDown }
    public enum StepResult
    { Block, Empty, Enemy, Team }
    public abstract class Movement
    {

        //
        public abstract void FindWays(
            IBoardModel board,
            Pos from,
            List<Pos> buffer);
        //
        private int Forward(PlayerTeam team)
            => team == PlayerTeam.Cho? 1 : -1;
        protected static readonly Pos[] Dirs =
        {
            Pos.Right,
            Pos.Left,
            Pos.Up,
            Pos.Down,
            Pos.RightUp,
            Pos.RightDown,
            Pos.LeftUp,
            Pos.LeftDown
        };
        protected Pos ApplyStep(
            Step step,
            Pos pos)
            => pos + GetDir(step);
        protected Pos GetDir(Step step)
            => Dirs[(int)step];
        protected Pos ApplyStep(
            Step step,
            PlayerTeam team,
            Pos pos)
            => pos += GetDir(step, team);
        protected Pos GetDir(Step step, PlayerTeam team)
        {
            var dir = Dirs[(int)step];
            return new Pos(dir.X, dir.Z * Forward(team));
        }

        protected static StepResult CheckCell(
            IBoardModel board,
            PlayerTeam team,
            Pos pos)
        {
            if (!board.IsInside(pos))
                return StepResult.Block;

            if (!board.HasPiece(pos))
                return StepResult.Empty;

            if (board.GetPiece(pos).Team == team)
                return StepResult.Team;

            return StepResult.Enemy;
        }
    }
}