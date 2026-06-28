using System.Collections.Generic;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;
namespace Yujanggi.Core.Match.Movement
{
    public class CannonMovement : Movement
    {
        //
        public override void FindWays(IBoardModel board, Pos from, List<Pos> buffer)
        {
            var piece = board.GetPiece(from);
            var team = piece.Team;

            foreach (var step in _steps)
            {
                bool bridge = false;
                var dPos = from;

                while (true)
                {
                    dPos = ApplyStep(step, team, dPos);

                    var result = CheckCell(board, team, dPos);

                    if (result == StepResult.Block)
                        break;

                    if (!bridge)
                    {
                        if (result == StepResult.Empty)
                            continue;

                        var otherPiece = board.GetPiece(dPos);

                        if (otherPiece.Type == PieceType.Cannon)
                            break;

                        bridge = true;
                        continue;
                    }

                    if (result == StepResult.Empty)
                    {
                        buffer.Add(dPos);
                        continue;
                    }

                    if (result == StepResult.Enemy)
                    {
                        var otherPiece = board.GetPiece(dPos);

                        if (otherPiece.Type != PieceType.Cannon)
                            buffer.Add(dPos);
                    }

                    break;
                }
            }

        }
        //
        Step[] _steps = new Step[] { Step.Up, Step.Down, Step.Left, Step.Right };
        

    }
}
