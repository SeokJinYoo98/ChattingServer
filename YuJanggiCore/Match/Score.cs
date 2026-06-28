using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Match
{
    using System;
    public class Score
    {
        public event Action<PlayerTeam, int> OnScoreChanged;
        private int _choScore = 72;
        private int _hanScore = 72;
        private int GetPieceScore(PieceType type)
        {
            return type switch
            {
                PieceType.Chariot => 13,    // 차
                PieceType.Cannon => 7,      // 포
                PieceType.Horse => 5,       // 마
                PieceType.Elephant => 3,    // 상
                PieceType.Guard => 3,       // 사
                PieceType.Soldier => 2,     // 졸
                PieceType.King => 10000,
                _ => 0
            };
        }
        public void ApplyScore(PlayerTeam team, PieceType type, bool isUndo = false)
        {
            var value = GetPieceScore(type);
            value = isUndo ? value : value * -1;
            if (team == PlayerTeam.Cho)
                _choScore += value;  
            else
                _hanScore += value;

            OnScoreChanged?.Invoke(team, team == PlayerTeam.Cho ? _choScore : _hanScore);
        }
        public PlayerTeam Winner()
        {
            if (_choScore == _hanScore)
                return PlayerTeam.None;

            return _choScore < _hanScore ? PlayerTeam.Han : PlayerTeam.Cho;
        }

        public void StartGame()
        {
            _choScore = 72;
            _hanScore = 72;

            OnScoreChanged?.Invoke(PlayerTeam.Cho, _choScore);
            OnScoreChanged?.Invoke(PlayerTeam.Han, _hanScore);
        }
    }
}
