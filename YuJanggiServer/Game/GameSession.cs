using MyServer.Client;
using YuJanggiCommon;
using Yujanggi.Core.Board;
using Yujanggi.Core.Domain;
using Yujanggi.Core.Match;
using Yujanggi.Core.Rule;
using CorePieceType = Yujanggi.Core.Domain.PieceType;
using CorePlayerTeam = Yujanggi.Core.Domain.PlayerTeam;

namespace MyServer.Game;

public sealed class GameSession
{
    public Guid GameId { get; }
    public ClientSession ChoPlayer { get; }
    public ClientSession HanPlayer { get; }
    public MatchModel Match { get; }

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

        return new GameStartEvent(
            GameId,
            side,
            ToPlayerSide(Match.PlayerTurn),
            pieces
        );
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
        Match.UnBindEvents();
        ChoPlayer.ClearMatch();
        HanPlayer.ClearMatch();
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
