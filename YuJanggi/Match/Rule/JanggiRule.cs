
namespace Yujanggi.Core.Rule
{
    using Domain;
    using System.Collections.Generic;
    using Yujanggi.Core.Board;

    public interface IJanggiRule
    {
        public void FindWays(
            IBoardModel board, 
            Selection       selection);
    }
    public class JanggiRule : IJanggiRule
    {
        private readonly MovementRule _movementRule;
        private readonly PalaceRule   _palaceRule;

        private readonly List<Pos>    _candidatesBuffer  = new(20);
        private readonly List<Pos>    _legalBuffer       = new(20);
        private readonly List<Pos>    _illegalBuffer     = new(20);
        private readonly List<Pos>    _kingInCheckBuffer = new(20);
 
        public JanggiRule()
        {
            _movementRule   = new();
            _palaceRule     = new();
        }

        //
        public bool IsLegalMove(IBoardModel board, Pos from, Pos to)
        {
            ClearBuffer();
            FindCandidates(board, from, _candidatesBuffer);
            FilterLegalMoves(board, from, _candidatesBuffer);
            return _legalBuffer.Contains(to);
        }
        public void  FindWays(IBoardModel board, Selection selection)
        {
            ClearBuffer();

            var fromPos = selection.FromPos;

            _movementRule.FindCandidateWays(board, fromPos, _candidatesBuffer);
            _palaceRule.ApplyPalaceRule(board, fromPos, _candidatesBuffer);

            FilterLegalMoves(board, selection.FromPos, _candidatesBuffer);

            selection.SetMovable(_legalBuffer, _illegalBuffer);
        }
        private void FindCandidates(IBoardModel board, Pos from, List<Pos> buffer)
        {
            _movementRule.FindCandidateWays(board, from, buffer);
            _palaceRule.ApplyPalaceRule(board, from, buffer);
        }
        private void ClearBuffer()
        {

            _illegalBuffer.Clear();
            _legalBuffer.Clear();
            _candidatesBuffer.Clear();
            _kingInCheckBuffer.Clear();
        }
        private void FilterLegalMoves(IBoardModel board, Pos from, List<Pos> targetPositions)
        {
            var piece = board.GetPiece(from);

            foreach (var toPos in targetPositions)
            {
                var moveRecord = board.DoMove(from, toPos);

                if (!IsKingInCheck(board, piece.Team))
                    _legalBuffer.Add(toPos);
                else 
                    _illegalBuffer.Add(toPos);

                board.UndoMove(moveRecord);
            }

        }
        public bool IsKingInCheck(IBoardModel board, PlayerTeam team)
        {
            var kingPos = board.GetKingPos(team);

            for (int x = 0; x < board.WIDTH; ++x)
            {
                for (int z = 0; z < board.HEIGHT; ++z)
                {
                    var pos = new Pos(x, z);

                    if (!board.HasPiece(pos))
                        continue;

                    var piece = board.GetPiece(pos);
    
                    // 내 말이면 검사할 필요 없음
                    if (piece.Team == team)
                        continue;

                    _kingInCheckBuffer.Clear();
                    FindCandidates(board, pos, _kingInCheckBuffer);
                    foreach (var way in _kingInCheckBuffer)
                    {
                        if (way == kingPos)
                            return true;
                    }
                }
            }

            return false;
        }
        //
        public bool CanMove(IBoardModel board, Pos from, Pos to)
        {
            if (!board.HasPiece(from))
                return false;

            return IsLegalMove(board, from, to);
        }
        public int CountLegalMove(IBoardModel board, PlayerTeam defence)
        {
            
            for (int x = 0; x < board.WIDTH; ++x)
            {
                for (int z = 0; z < board.HEIGHT; ++z)
                {
                    var pos = new Pos(x, z);
                    if (!board.HasPiece(pos)) continue;

                    var piece = board.GetPiece(pos);
                    if (piece.Team != defence) continue;

                    ClearBuffer();
                    FindCandidates(board, pos, _candidatesBuffer);
                    FilterLegalMoves(board, pos, _candidatesBuffer);
                    
                    if (0 < _legalBuffer.Count)
                        return _legalBuffer.Count;      
                }
            }
            return 0;
        }
    }
}