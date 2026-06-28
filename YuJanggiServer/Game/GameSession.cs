using YuJanggiServer.Client;
using YuJanggiCommon;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;
using Yujanggi.Core.Match;
using Yujanggi.Core.Rule;
using CorePieceType = Yujanggi.Core.Domain.PieceType;
using CorePlayerTeam = Yujanggi.Core.Domain.PlayerTeam;

namespace YuJanggiServer.Game;

public sealed class GameSession
{
    public Guid GameId { get; }
    public ClientSession ChoPlayer { get; }
    public ClientSession HanPlayer { get; }
    public MatchModel Match { get; }

    private readonly Lock _gameLock = new();

    public GameSession(
        Guid gameId,
        ClientSession choPlayer,
        ClientSession hanPlayer)
    {
        GameId = gameId;
        ChoPlayer = choPlayer;
        HanPlayer = hanPlayer;
        Match = new MatchModel(
            new Turn(0),
            new Record(),
            new Score(),
            new BoardModel(),
            new JanggiRule()
        );

        Match.InitGame(Formation.EHHE, Formation.EHHE);
        Match.BindEvents();
        Match.StartGame();
    }

    public GameStartEvent CreateGameStart(PlayerSide side)
    {
        lock (_gameLock)
        {
            return new GameStartEvent(
                GameId,
                side,
                ToPlayerSide(Match.PlayerTurn),
                CreateBoardSnapshot()
            );
        }
    }

    public ErrorCode? TryGetLegalMoves(
        ClientSession session,
        BoardPosition from,
        out LegalMovesResult? result)
    {
        lock (_gameLock)
        {
            result = null;
            Pos fromPosition = new(from.X, from.Z);
            ErrorCode? validationError =
                ValidateMoveSource(session, fromPosition);

            if (validationError.HasValue)
            {
                return validationError;
            }

            Selection selection = new()
            {
                FromPos = fromPosition
            };

            Match.Rule.FindWays(Match.Board, selection);

            result = new LegalMovesResult(
                from,
                selection.LegalCells
                    .Select(position =>
                        new BoardPosition(position.X, position.Z)
                    )
                    .ToList()
            );
            return null;
        }
    }

    public ErrorCode? TryMove(
        ClientSession session,
        MoveRequest request,
        out MoveResultEvent? result)
    {
        lock (_gameLock)
        {
            result = null;
            Pos from = new(request.From.X, request.From.Z);
            Pos to = new(request.To.X, request.To.Z);
            ErrorCode? validationError =
                ValidateMoveSource(session, from);

            if (validationError.HasValue)
            {
                return validationError;
            }

            if (!Match.Board.IsInside(to))
            {
                return ErrorCode.InvalidPosition;
            }

            if (!Match.TryMove(from, to))
            {
                return ErrorCode.IllegalMove;
            }

            PlayerSide movedBy = session.Side
                ?? throw new InvalidOperationException(
                    "매칭된 세션에 진영 정보가 없습니다."
                );

            result = new MoveResultEvent(
                GameId,
                request.From,
                request.To,
                movedBy,
                ToPlayerSide(Match.PlayerTurn),
                CreateBoardSnapshot()
            );
            return null;
        }
    }

    public bool Contains(ClientSession session)
    {
        return ReferenceEquals(ChoPlayer, session) ||
            ReferenceEquals(HanPlayer, session);
    }

    public ClientSession GetOpponent(ClientSession session)
    {
        if (ReferenceEquals(ChoPlayer, session))
        {
            return HanPlayer;
        }

        if (ReferenceEquals(HanPlayer, session))
        {
            return ChoPlayer;
        }

        throw new InvalidOperationException(
            "게임에 참가하지 않은 세션입니다."
        );
    }

    public void ClearPlayers()
    {
        lock (_gameLock)
        {
            Match.UnBindEvents();
            ChoPlayer.ClearMatch();
            HanPlayer.ClearMatch();
        }
    }

    private ErrorCode? ValidateMoveSource(
        ClientSession session,
        Pos from)
    {
        if (!Contains(session) ||
            session.Side is not PlayerSide playerSide)
        {
            return ErrorCode.GameSessionNotFound;
        }

        CorePlayerTeam playerTeam = ToCorePlayerTeam(playerSide);

        if (Match.PlayerTurn != playerTeam)
        {
            return ErrorCode.NotYourTurn;
        }

        if (!Match.Board.IsInside(from))
        {
            return ErrorCode.InvalidPosition;
        }

        if (!Match.Board.HasPiece(from))
        {
            return ErrorCode.PieceNotFound;
        }

        if (Match.Board.GetPiece(from).Team != playerTeam)
        {
            return ErrorCode.NotYourPiece;
        }

        return null;
    }

    private List<BoardPieceState> CreateBoardSnapshot()
    {
        List<BoardPieceState> pieces = new();

        for (int x = 0; x < Match.Board.WIDTH; x++)
        {
            for (int z = 0; z < Match.Board.HEIGHT; z++)
            {
                Pos position = new(x, z);

                if (!Match.Board.HasPiece(position))
                {
                    continue;
                }

                PieceModel piece = Match.Board.GetPiece(position);
                pieces.Add(new BoardPieceState(
                    piece.Id,
                    x,
                    z,
                    ToPlayerSide(piece.Team),
                    ToGamePieceType(piece.Type)
                ));
            }
        }

        return pieces;
    }

    private static PlayerSide ToPlayerSide(CorePlayerTeam team)
    {
        return team switch
        {
            CorePlayerTeam.Cho => PlayerSide.Cho,
            CorePlayerTeam.Han => PlayerSide.Han,
            _ => throw new InvalidOperationException(
                $"지원하지 않는 진영입니다: {team}"
            )
        };
    }

    private static CorePlayerTeam ToCorePlayerTeam(PlayerSide side)
    {
        return side switch
        {
            PlayerSide.Cho => CorePlayerTeam.Cho,
            PlayerSide.Han => CorePlayerTeam.Han,
            _ => throw new InvalidOperationException(
                $"지원하지 않는 진영입니다: {side}"
            )
        };
    }

    private static GamePieceType ToGamePieceType(CorePieceType pieceType)
    {
        return pieceType switch
        {
            CorePieceType.King => GamePieceType.King,
            CorePieceType.Chariot => GamePieceType.Chariot,
            CorePieceType.Cannon => GamePieceType.Cannon,
            CorePieceType.Horse => GamePieceType.Horse,
            CorePieceType.Elephant => GamePieceType.Elephant,
            CorePieceType.Guard => GamePieceType.Guard,
            CorePieceType.Soldier => GamePieceType.Soldier,
            _ => throw new InvalidOperationException(
                $"지원하지 않는 기물입니다: {pieceType}"
            )
        };
    }
}
