using MyServer.Client;
using Yujanggi.Core.Board;
using Yujanggi.Core.Match;
using Yujanggi.Core.Rule;

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
        ChoPlayer.ClearMatch();
        HanPlayer.ClearMatch();
    }
}
