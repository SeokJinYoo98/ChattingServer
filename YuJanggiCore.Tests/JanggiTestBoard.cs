using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;
using Yujanggi.Core.Match;
using Yujanggi.Core.Rule;

namespace YuJanggiCore.Tests;

internal static class JanggiTestBoard
{
    public static BoardModel CreateBoardWithKings()
    {
        var board = new BoardModel();
        board.ResetBoard();
        board.SetPiece(new Pos(4, 1), Piece(PieceType.King, PlayerTeam.Cho));
        board.SetPiece(new Pos(4, 8), Piece(PieceType.King, PlayerTeam.Han));
        return board;
    }

    public static MatchModel CreateEmptyMatch()
    {
        var match = new MatchModel(
            new Turn(0),
            new Record(),
            new Score(),
            new BoardModel(),
            new JanggiRule());

        match.Board.ResetBoard();
        match.Board.SetPiece(new Pos(4, 1), Piece(PieceType.King, PlayerTeam.Cho));
        match.Board.SetPiece(new Pos(4, 8), Piece(PieceType.King, PlayerTeam.Han));
        match.StartGame();
        return match;
    }

    public static PieceModel Piece(PieceType type, PlayerTeam team, int id = 0)
        => new(type, team, id);

    public static void MoveChoKing(BoardModel board, Pos to)
        => board.DoMove(new Pos(4, 1), to);
}
