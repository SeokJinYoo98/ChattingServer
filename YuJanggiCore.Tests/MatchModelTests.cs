using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yujanggi.Core.Domain;

namespace YuJanggiCore.Tests;

[TestClass]
public class MatchModelTests
{
    [TestMethod]
    public void LegalMove_CapturesEnemyUpdatesScoreRecordAndChangesTurn()
    {
        // Arrange
        var match = JanggiTestBoard.CreateEmptyMatch();
        var from = new Pos(0, 3);
        var to = new Pos(0, 6);
        match.Board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));
        match.Board.SetPiece(to, JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Han));
        var hanScore = -1;
        match.Score.OnScoreChanged += (team, score) =>
        {
            if (team == PlayerTeam.Han)
                hanScore = score;
        };

        // Act
        var moved = match.TryMove(from, to);

        // Assert
        Assert.IsTrue(moved);
        Assert.AreEqual(PieceType.Chariot, match.Board.GetPiece(to).Type);
        Assert.AreEqual(PlayerTeam.Cho, match.Board.GetPiece(to).Team);
        Assert.IsTrue(match.Board.GetPiece(from).IsNone);
        Assert.AreEqual(PlayerTeam.Han, match.PlayerTurn);
        Assert.AreEqual(1, match.RecordCnt);
        Assert.AreEqual(70, hanScore);
    }

    [TestMethod]
    public void IllegalMove_LeavesBoardTurnScoreAndRecordUnchanged()
    {
        // Arrange
        var match = JanggiTestBoard.CreateEmptyMatch();
        var from = new Pos(0, 3);
        var to = new Pos(1, 4);
        match.Board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));
        var hanScoreChanged = false;
        match.Score.OnScoreChanged += (team, _) => hanScoreChanged |= team == PlayerTeam.Han;

        // Act
        var moved = match.TryMove(from, to);

        // Assert
        Assert.IsFalse(moved);
        Assert.AreEqual(PieceType.Chariot, match.Board.GetPiece(from).Type);
        Assert.IsTrue(match.Board.GetPiece(to).IsNone);
        Assert.AreEqual(PlayerTeam.Cho, match.PlayerTurn);
        Assert.AreEqual(0, match.RecordCnt);
        Assert.IsFalse(hanScoreChanged);
    }

    [TestMethod]
    public void MoveByOpponentDuringChoTurn_IsRejectedAndStateIsPreserved()
    {
        // Arrange
        var match = JanggiTestBoard.CreateEmptyMatch();
        var from = new Pos(0, 6);
        var to = new Pos(0, 5);
        match.Board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Han));
        match.Board.SetPiece(to, JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Cho));
        var choScoreChanged = false;
        match.Score.OnScoreChanged += (team, _) => choScoreChanged |= team == PlayerTeam.Cho;

        // Act
        var moved = match.TryMove(from, to);

        // Assert
        Assert.IsFalse(moved);
        Assert.AreEqual(PieceType.Chariot, match.Board.GetPiece(from).Type);
        Assert.AreEqual(PieceType.Soldier, match.Board.GetPiece(to).Type);
        Assert.AreEqual(PlayerTeam.Cho, match.Board.GetPiece(to).Team);
        Assert.AreEqual(PlayerTeam.Cho, match.PlayerTurn);
        Assert.AreEqual(0, match.RecordCnt);
        Assert.IsFalse(choScoreChanged);
    }

    [TestMethod]
    public void MoveThatDoesNotResolveCheck_IsRejectedAndStateIsPreserved()
    {
        // Arrange
        var match = JanggiTestBoard.CreateEmptyMatch();
        var from = new Pos(0, 3);
        var to = new Pos(0, 4);
        match.Board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));
        match.Board.SetPiece(new Pos(4, 5), JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Han));

        // Act
        var moved = match.TryMove(from, to);

        // Assert
        Assert.IsFalse(moved);
        Assert.AreEqual(PieceType.Chariot, match.Board.GetPiece(from).Type);
        Assert.IsTrue(match.Board.GetPiece(to).IsNone);
        Assert.AreEqual(PlayerTeam.Cho, match.PlayerTurn);
        Assert.AreEqual(0, match.RecordCnt);
    }

    [TestMethod]
    public void CheckingMove_RecordsJanggun()
    {
        // Arrange
        var match = JanggiTestBoard.CreateEmptyMatch();
        var from = new Pos(4, 3);
        var to = new Pos(4, 7);
        match.Board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));

        // Act
        var moved = match.TryMove(from, to);
        var hasRecord = match.Record.TryPeek(out var context);

        // Assert
        Assert.IsTrue(moved);
        Assert.IsTrue(hasRecord);
        Assert.IsTrue(context.IsJanggun);
        Assert.AreEqual(PlayerTeam.Han, match.PlayerTurn);
    }

    [TestMethod]
    public void UndoAfterCapture_RestoresBoardTurnScoreAndRecord()
    {
        // Arrange
        var match = JanggiTestBoard.CreateEmptyMatch();
        var from = new Pos(0, 3);
        var to = new Pos(0, 6);
        match.Board.SetPiece(from, JanggiTestBoard.Piece(PieceType.Chariot, PlayerTeam.Cho));
        match.Board.SetPiece(to, JanggiTestBoard.Piece(PieceType.Soldier, PlayerTeam.Han, 7));
        var hanScore = -1;
        match.Score.OnScoreChanged += (team, score) =>
        {
            if (team == PlayerTeam.Han)
                hanScore = score;
        };
        Assert.IsTrue(match.TryMove(from, to));

        // Act
        var undone = match.TryUnDo(out var context);

        // Assert
        Assert.IsTrue(undone);
        Assert.IsTrue(context.IsCapture);
        Assert.AreEqual(PieceType.Chariot, match.Board.GetPiece(from).Type);
        Assert.AreEqual(PieceType.Soldier, match.Board.GetPiece(to).Type);
        Assert.AreEqual(PlayerTeam.Han, match.Board.GetPiece(to).Team);
        Assert.AreEqual(PlayerTeam.Cho, match.PlayerTurn);
        Assert.AreEqual(0, match.RecordCnt);
        Assert.AreEqual(72, hanScore);
    }

    [TestMethod]
    public void Handicap_RecordsPassAndChangesTurnWithoutChangingBoard()
    {
        // Arrange
        var match = JanggiTestBoard.CreateEmptyMatch();
        var choKingPosition = match.Board.GetKingPos(PlayerTeam.Cho);
        var hanKingPosition = match.Board.GetKingPos(PlayerTeam.Han);

        // Act
        match.Handicap();
        var hasRecord = match.Record.TryPeek(out var context);

        // Assert
        Assert.IsTrue(hasRecord);
        Assert.IsTrue(context.IsHandicap);
        Assert.AreEqual(PlayerTeam.Han, match.PlayerTurn);
        Assert.AreEqual(1, match.RecordCnt);
        Assert.AreEqual(choKingPosition, match.Board.GetKingPos(PlayerTeam.Cho));
        Assert.AreEqual(hanKingPosition, match.Board.GetKingPos(PlayerTeam.Han));
    }
}
