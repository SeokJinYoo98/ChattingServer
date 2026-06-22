using System;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;
using Yujanggi.Core.Rule;
namespace Yujanggi.Core.Match
{
    public class MatchEvents
    {
        public event Action<MoveContext>               OnPieceMoved;
        public event Action<PlayerTeam>               OnCheckOccurred;
        public event Action                           OnCheckReleased;
        public event Action<GameResultInfo>           OnGameEnded;
        public event Action<PlayerTeam>               OnTurnChanged;
        public void PieceMoved(MoveContext ctx)
            => OnPieceMoved?.Invoke(ctx);
        public void CheckOccurred(PlayerTeam team)
            => OnCheckOccurred?.Invoke(team);
        public void CheckReleased()
            => OnCheckReleased?.Invoke();
        public void GameEnded(GameResultInfo info)
            => OnGameEnded?.Invoke(info);
        public void TurnChanged(PlayerTeam next)
            => OnTurnChanged?.Invoke(next);
    }
    public interface IMatchUIDatas
    {
        public MatchEvents  MatchEvent { get; }
        public Turn         Turn { get; }
        public JanggiRule   Rule { get; }
        public Record       Record { get; }
        public Score        Score { get; }
    }
    public interface ILiveMatch : IMatchUIDatas
    {
        public int         RecordCnt { get; }
        public PlayerTeam  PlayerTurn { get; }
        public BoardModel  Board { get; }

        public void GiveUp();
        public bool TryMove(Pos from, Pos to);
        public bool TryUnDo(out MoveContext ctx);
        public void Handicap(); 
    }

    public class MatchModel : ILiveMatch, IMatchUIDatas
    {
        public int           RecordCnt => Record.Count;
        public  PlayerTeam   PlayerTurn => Turn.CurrentTeam;
        public  MatchEvents  MatchEvent { get; } = new();
        public  Turn         Turn { get; }
        public  Record       Record { get; }
        public  Score        Score { get; }
        public  BoardModel   Board { get; }
        public  JanggiRule   Rule { get; }
        public MatchModel(Turn turn, Record record, Score score, BoardModel board, JanggiRule rule)
        {
            Turn   = turn;
            Record = record;
            Score  = score;
            Board  = board;
            Rule   = rule;
        }
        public bool     TryMove(Pos from, Pos to)
        {
            if (Turn.IsEnd)
                return false;
            
            if (!Board.IsInside(from) || !Board.IsInside(to))
                return false;

            if (!Board.HasPiece(from))
                return false;

            if (!Rule.IsLegalMove(Board, from, to))
                return false;

            ExecuteMove(from, to);
            return true;
        }
        public void InitGame(Formation cho, Formation han)
        {
            Board.ResetBoard();
            BoardInitializer.SetUpPieces(Board, cho, han);
        }
        public void     StartGame()
        { 
            Record.StartGame();
            Score.StartGame();
            Turn.StartGame(PlayerTeam.Cho);
        }

        public void     UnBindEvents()
        {
            this.Turn.OnTurnChanged  -= TurnChanged;
            this.Turn.OnTurnEnd      -= Handicap;

        }
        public void     BindEvents()
        {
            this.Turn.OnTurnChanged  += TurnChanged;
            this.Turn.OnTurnEnd      += Handicap;
        }

        public bool     TryUnDo(out MoveContext ctx)
        {
            ctx = default;

            if (Turn.IsEnd)
                return false;

            if (!Record.TryPop(out ctx))
                return false;
            
            if (!ctx.IsHandicap)
            {
                var record = ctx.Record;
                Board.UndoMove(record);

                if (record.IsCapture)
                {
                    var captured = record.CapturedPiece;
                    Score.ApplyScore(captured.Team, captured.Type, true);
                }
            }

            Turn.NextTurn();

            return true;
        }

        public void Handicap()
        {
            if (Turn.IsEnd) return;
            Record.Push(MoveContext.Handicap);
            Turn.NextTurn();
        }
        public void GiveUp()
        {
            OnGameEnded(GameResult.GiveUp, Turn.CurrentTeam);
        }
        public void  Tick(float deltaTime)
        {
            Turn.Update(deltaTime);
        }


        private bool IsCheck(PlayerTeam otherTeam)
        {
            var result = Rule.IsKingInCheck(Board, otherTeam);
            if (result) MatchEvent.CheckOccurred(otherTeam); 
            if (Record.TryPeek(out var ctx) && ctx.IsJanggun)
                MatchEvent.CheckReleased();
            return result;
        }
        private bool HasAnyLegalMove(PlayerTeam otherTeam)
        {
            int cnt = Rule.CountLegalMove(Board, otherTeam);
            if (cnt == 0)
                return true;
            

            return false;
        }
        private void ExecuteMove(Pos from, Pos to)
        {
            var record = Board.DoMove(from, to);

            var otherTeam = Turn.CurrentTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            if (record.IsCapture)
                Score.ApplyScore(otherTeam, record.CapturedPiece.Type);

            var isJanggun = IsCheck(otherTeam);
            var isEnd     = HasAnyLegalMove(otherTeam);
            var ctx       = new MoveContext(record, isJanggun, isEnd);

            Record.Push(ctx);
            MatchEvent.PieceMoved(ctx);

            if (isEnd) 
                OnGameEnded(GameResult.CheckMate, otherTeam);
            else 
                Turn.NextTurn();
        }
        private void TurnChanged(PlayerTeam next)
        {
            this.MatchEvent.TurnChanged(next);
        }
        private void OnGameEnded(GameResult type, PlayerTeam loser)
        {
            this.Turn.EndGame();
            var info     = new GameResultInfo();
            info.Type    = type;
            info.Loser   = loser;
            info.MoveCnt = Record.TotalTurn;
            MatchEvent.GameEnded(info);
        }
    }
}