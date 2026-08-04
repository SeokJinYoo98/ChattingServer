using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yujanggi.Core.Domain;
using Yujanggi.Core.Rule;

namespace YuJanggiCore.Tests;

[TestClass]
public class JanggiRuleTests
{
    private readonly JanggiRule _rule = new();

    [TestMethod]
    public void Chariot_MovesInStraightLine_WhenPathIsClear()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        var from = new Pos(0, 3);
        var to = new Pos(0, 6);
        board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));

        // Act
        var canMove = _rule.CanMove(board, from, to);

        // Assert
        Assert.IsTrue(canMove);
    }

    [TestMethod]
    public void Chariot_CannotMovePastBlockingPiece()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        var from = new Pos(0, 3);
        board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));
        board.SetPiece(new Pos(0, 5), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));

        // Act
        var canMove = _rule.CanMove(board, from, new Pos(0, 6));

        // Assert
        Assert.IsFalse(canMove);
    }

    [TestMethod]
    public void Cannon_CanMoveAfterJumpingOneNonCannonPiece()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        var from = new Pos(0, 3);
        board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Cannon, PlayerTeam.Cho));
        board.SetPiece(new Pos(0, 5), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));

        // Act
        var canMove = _rule.CanMove(board, from, new Pos(0, 6));

        // Assert
        Assert.IsTrue(canMove);
    }

    [TestMethod]
    public void Cannon_CannotMoveWithoutBridgeOrAcrossCannon()
    {
        // Arrange
        var boardWithoutBridge = JanggiTestBoard.CreateBoardWithKings();
        boardWithoutBridge.SetPiece(new Pos(0, 3), JanggiTestBoard.Piece(PieceType.Cannon, PlayerTeam.Cho));
        var boardWithCannonBridge = JanggiTestBoard.CreateBoardWithKings();
        boardWithCannonBridge.SetPiece(new Pos(0, 3), JanggiTestBoard.Piece(PieceType.Cannon, PlayerTeam.Cho));
        boardWithCannonBridge.SetPiece(new Pos(0, 5), JanggiTestBoard.Piece(PieceType.Cannon, PlayerTeam.Han));

        // Act
        var withoutBridge = _rule.CanMove(boardWithoutBridge, new Pos(0, 3), new Pos(0, 6));
        var acrossCannon = _rule.CanMove(boardWithCannonBridge, new Pos(0, 3), new Pos(0, 6));

        // Assert
        Assert.IsFalse(withoutBridge);
        Assert.IsFalse(acrossCannon);
    }

    [TestMethod]
    public void Cannon_CannotCaptureAnotherCannon()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        board.SetPiece(new Pos(0, 3), JanggiTestBoard.Piece(PieceType.Cannon, PlayerTeam.Cho));
        board.SetPiece(new Pos(0, 5), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));
        board.SetPiece(new Pos(0, 6), JanggiTestBoard.Piece(PieceType.Cannon, PlayerTeam.Han));

        // Act
        var canMove = _rule.CanMove(board, new Pos(0, 3), new Pos(0, 6));

        // Assert
        Assert.IsFalse(canMove);
    }

    [TestMethod]
    public void Horse_CannotMoveWhenItsLegIsBlocked()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        board.SetPiece(new Pos(4, 4), JanggiTestBoard.Piece(PieceType.Horse, PlayerTeam.Cho));
        board.SetPiece(new Pos(4, 5), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));

        // Act
        var canMove = _rule.CanMove(board, new Pos(4, 4), new Pos(3, 6));

        // Assert
        Assert.IsFalse(canMove);
    }

    [DataTestMethod]
    [DataRow(4, 5)]
    [DataRow(3, 5)]
    public void Elephant_CannotMoveWhenEitherIntermediatePointIsBlocked(int blockerX, int blockerZ)
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        board.SetPiece(new Pos(4, 4), JanggiTestBoard.Piece(PieceType.Elephant, PlayerTeam.Cho));
        board.SetPiece(new Pos(blockerX, blockerZ), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));

        // Act
        var canMove = _rule.CanMove(board, new Pos(4, 4), new Pos(2, 6));

        // Assert
        Assert.IsFalse(canMove);
    }

    [TestMethod]
    public void Soldiers_CanMoveForwardAndSideways_ButNotBackward()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        board.SetPiece(new Pos(4, 4), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));
        board.SetPiece(new Pos(4, 5), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Han));

        // Act
        var choForward = _rule.CanMove(board, new Pos(4, 4), new Pos(4, 5));
        var choSideways = _rule.CanMove(board, new Pos(4, 4), new Pos(3, 4));
        var choBackward = _rule.CanMove(board, new Pos(4, 4), new Pos(4, 3));
        var hanForward = _rule.CanMove(board, new Pos(4, 5), new Pos(4, 4));
        var hanSideways = _rule.CanMove(board, new Pos(4, 5), new Pos(5, 5));
        var hanBackward = _rule.CanMove(board, new Pos(4, 5), new Pos(4, 6));

        // Assert
        Assert.IsTrue(choForward);
        Assert.IsTrue(choSideways);
        Assert.IsFalse(choBackward);
        Assert.IsTrue(hanForward);
        Assert.IsTrue(hanSideways);
        Assert.IsFalse(hanBackward);
    }

    [TestMethod]
    public void Guard_CanMoveDiagonallyInsidePalace_ButCannotLeavePalace()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        JanggiTestBoard.MoveChoKing(board, new Pos(3, 0));
        board.SetPiece(new Pos(4, 1), JanggiTestBoard.Piece(PieceType.Guard, PlayerTeam.Cho));

        // Act
        var diagonal = _rule.CanMove(board, new Pos(4, 1), new Pos(3, 2));
        var outside = _rule.CanMove(board, new Pos(4, 1), new Pos(4, 3));

        // Assert
        Assert.IsTrue(diagonal);
        Assert.IsFalse(outside);
    }

    [TestMethod]
    public void Chariot_CanUseUnblockedPalaceDiagonal()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        JanggiTestBoard.MoveChoKing(board, new Pos(5, 0));
        board.SetPiece(new Pos(3, 0), JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));

        // Act
        var canMove = _rule.CanMove(board, new Pos(3, 0), new Pos(5, 2));

        // Assert
        Assert.IsTrue(canMove);
    }

    [TestMethod]
    public void Piece_CannotMoveToSquareOccupiedByFriendlyPiece()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        board.SetPiece(new Pos(0, 3), JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));
        board.SetPiece(new Pos(0, 4), JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));

        // Act
        var canMove = _rule.CanMove(board, new Pos(0, 3), new Pos(0, 4));

        // Assert
        Assert.IsFalse(canMove);
    }

    [TestMethod]
    public void PinnedPiece_CannotMoveWhenItWouldExposeOwnKingToCheck()
    {
        // Arrange
        var board = JanggiTestBoard.CreateBoardWithKings();
        board.SetPiece(new Pos(4, 3), JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));
        board.SetPiece(new Pos(4, 5), JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Han));

        // Act
        var canMove = _rule.CanMove(board, new Pos(4, 3), new Pos(3, 3));

        // Assert
        Assert.IsFalse(canMove);
    }
}
