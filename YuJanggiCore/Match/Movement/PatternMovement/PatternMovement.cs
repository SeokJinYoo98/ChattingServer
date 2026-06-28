using System.Collections.Generic;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Match.Movement
{
    public class PatternMovement : Movement
    {
        //
        public override void FindWays(
            IBoardModel board,
            Pos from,
            List<Pos> buffer)
        {
            var piece = board.GetPiece(from);
            foreach (var steps in _steps)
                ProcessDirection(buffer, board, piece.Team, from, steps);
        }
        //
        protected Step[][] _steps;
        
        private void ProcessDirection(
            List<Pos> buffer,
            IBoardModel board,
            PlayerTeam team,
            Pos pos,
            Step[] steps)
        {
            int len = steps.Length;
            var dPos = pos;
            for (int j = 0; j < len; ++j)
            {
                dPos = ApplyStep(steps[j], team, dPos);

                if (j < len - 1)
                {
                    if (IsBlocked(board, team, dPos))
                        return;
                }
                else
                {
                    if (CanLand(board, team, dPos))
                        buffer.Add((dPos));
                }
            }
        }
        private bool IsBlocked(IBoardModel board, PlayerTeam team, Pos pos)
        {
            var result = CheckCell(board, team, pos);
            return result != StepResult.Empty;
        }
        private bool CanLand(IBoardModel board, PlayerTeam team, Pos pos)
        {
            var result = CheckCell(board, team, pos);
            return (result == StepResult.Empty) || (result == StepResult.Enemy);
        }
    }
}
