
using System.Collections.Generic;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Rule
{
    using Match.Movement;
    public class PalaceRule
    {
        private PalaceMovement _palaceMovement;
        private readonly HashSet<PieceType> _addRule = new()
                { PieceType.Cannon, PieceType.Chariot, PieceType.Soldier, PieceType.King, PieceType.Guard };
        // 필터 대상
        private readonly HashSet<PieceType> _filterRule = new()
                { PieceType.King, PieceType.Guard, PieceType.Soldier };

        public PalaceRule()
        {
            _palaceMovement = new();
        }
        public void ApplyPalaceRule(IBoardModel board, Pos from, List<Pos> buffer)
        {
            var piece = board.GetPiece(from);
            var type = piece.Type;
            if (_addRule.Contains(type) && board.IsPalace(from))
                _palaceMovement.FindWays(board, from, buffer);

            
            if (_filterRule.Contains(type))
                Filter(board, in piece, from, buffer);
        }
        private void Filter(
            IBoardModel board, 
            in PieceModel piece, 
            Pos from, 
            List<Pos> buffer)
        {
            switch (piece.Type)
            {
                case PieceType.Soldier:
                    FilterSoldier(from, piece, buffer);
                    break;

                case PieceType.King:
                case PieceType.Guard:
                    buffer.RemoveAll(pos => !board.IsPalace(pos));
                    break;
            }
        }
        private void FilterSoldier(
            Pos             from,
            in PieceModel   piece,
            List<Pos>       buffer)
        {
            if (piece.Team == PlayerTeam.Cho)
                buffer.RemoveAll(pos => pos.Z < from.Z);
            else
                buffer.RemoveAll(pos => pos.Z > from.Z);
        }



        private void FilterKingGuard(IBoardModel board, List<Pos> ways)
        {
            ways.RemoveAll(pos => !board.IsPalace(pos));
        }
    }
}
