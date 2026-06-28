using System.Collections.Generic;
using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Board
{

    public interface IBoardModel 
    {

        public int WIDTH { get; }
        public int HEIGHT { get; }
        public Pos          GetKingPos(PlayerTeam team);
        bool                IsInside(Pos pos);
        bool                HasPiece(Pos pos);
        PieceModel          GetPiece(Pos pos);
        public bool         IsPalace(Pos pos);
        public void         SetPiece(Pos pos, PieceModel piece);
        public MoveRecord   DoMove(Pos from, Pos to);
        public void         UndoMove(in MoveRecord moveRecord);
    }

    public class BoardModel : IBoardModel
    {
        public int WIDTH  => _width;
        public int HEIGHT => _height;

        public BoardModel(int width = 9, int height = 10)
        {
            _width = width; _height = height;
            CreateBoard();

            _choKingPos = new Pos(4, 1);
            _hanKingPos = new Pos(4, 8);
        }
        public void         ResetBoard()
        {
            for (int x = 0; x < _width; ++x)
            {
                for (int z = 0; z < _height; ++z)
                {
                    _board[x, z].Piece = PieceModel.None;
                }
            }
        }
        public bool         IsPalace(Pos pos)
        {
            if (!IsInside(pos)) return false;
            return _board[pos.X, pos.Z].Palace;
        }
        public bool         IsInside(Pos pos)
            => 0 <= pos.X && pos.X < _width && 0 <= pos.Z && pos.Z < _height;
        public bool         HasPiece(Pos pos)
            => !_board[pos.X, pos.Z].Piece.IsNone;
        public PieceModel   GetPiece(Pos pos)
            => _board[pos.X, pos.Z].Piece;
        public void         SetPiece(Pos pos, PieceModel piece)
        {
            _board[pos.X, pos.Z].Piece = piece;
        }
        public Pos          GetKingPos(PlayerTeam team)
            => team == PlayerTeam.Cho ? _choKingPos : _hanKingPos;
        public MoveRecord   DoMove(Pos from, Pos to)
        {
            var moved       = GetPiece(from);
            var captured    = GetPiece(to);

            SetPiece(from, PieceModel.None);
            SetPiece(to, moved);

            UpdateKingPos(to, moved);

            return new(from, to, moved, captured);
        }
        public void         UndoMove(in MoveRecord moveRecord)
        {
            var from = moveRecord.From;
            var to = moveRecord.To;

            var moved = moveRecord.MovedPiece;

            SetPiece(from, moved);
            SetPiece(to, moveRecord.IsCapture ? moveRecord.CapturedPiece : PieceModel.None);

            UpdateKingPos(from, moved);
        }

        // private
        private CellData[,] _board;

        private readonly int _width = 9;
        private readonly int _height = 10;

        private Pos _choKingPos;
        private Pos _hanKingPos;
       
        // private
        private void UpdateKingPos(Pos pos, PieceModel piece)
        {
            if (piece.Type != PieceType.King)
                return;

            if (piece.Team == PlayerTeam.Cho)
                _choKingPos = pos;
            else
                _hanKingPos = pos;
        }
        private void CreateBoard()
        {
            _board = new CellData[_width, _height];
            for (int x = 0; x < _width; ++x)
            {
                for (int z = 0; z < _height; ++z)
                {
                    _board[x, z] = new();
                    _board[x, z].Piece = PieceModel.None;
                }
            }


            for (int x = 3; x <= 5; ++x)
            {
                for (int z = 0; z <= 2; ++z)
                {
                    _board[x, z].Palace = true;
                    _board[x, z + 7].Palace = true;
                }
            }
        }

    }
}
