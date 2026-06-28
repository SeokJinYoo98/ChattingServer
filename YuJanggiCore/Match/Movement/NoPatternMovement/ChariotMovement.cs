using System.Collections.Generic;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Match.Movement
{
    public class ChariotMovement : Movement
    {
        //
        public override void FindWays(IBoardModel board, Pos from, List<Pos> buffer)
        {
            var piece = board.GetPiece(from);
            var team = piece.Team;

            foreach (var step in _steps)
            {
                var dPos = from;
                while (true)
                {
                    dPos = ApplyStep(step, team, dPos);
                    var result = CheckCell(board, team, dPos);

                    if (result == StepResult.Block || result == StepResult.Team)
                        break;

                    buffer.Add(dPos);
                    if (result == StepResult.Enemy)
                        break;
                }
            }
        }
        //
        Step[] _steps = new Step[] { Step.Up, Step.Down, Step.Left, Step.Right };

    }
}