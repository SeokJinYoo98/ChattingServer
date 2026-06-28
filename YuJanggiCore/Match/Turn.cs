using System;
using Yujanggi.Core.Domain;

namespace Yujanggi.Core.Match
{
    public class Turn
    {
        public event Action<PlayerTeam> OnTurnChanged;
        public event Action<(PlayerTeam team, int time)> OnTimeChanged;
        public event Action OnTurnEnd;

        public PlayerTeam CurrentTeam { get; private set; }
        public bool  IsEnd => _isEnd;
        private bool _isEnd = false;
        private bool _noTime = false;
        
        public Turn(float maxTime)
        {
            _maxTurnTime = maxTime;
            _noTime = (int)_maxTurnTime == 0;
        }
        public void StartGame(PlayerTeam player)
        {
            _isEnd = false;
            CurrentTeam = player;
            _turnTime   = _maxTurnTime;
            OnTurnChanged?.Invoke(CurrentTeam);
            OnTimeChanged?.Invoke((PlayerTeam.Cho, (int)_turnTime));
            OnTimeChanged?.Invoke((PlayerTeam.Han, (int)_turnTime));
        }

        public void EndGame()
        {
            _isEnd = true;
        }


        public PlayerTeam NextTurn()
        {
            if (_isEnd) 
                return PlayerTeam.None;
            _turnTime = _maxTurnTime;
            OnTimeChanged?.Invoke((CurrentTeam, (int)_turnTime));

            CurrentTeam = (CurrentTeam == PlayerTeam.Cho)
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            OnTimeChanged?.Invoke((CurrentTeam, (int)_turnTime));
            OnTurnChanged?.Invoke(CurrentTeam);
            return CurrentTeam;
        }
        private float           _timer = 0;
        private float           _turnTime = 30;
        private readonly float  _maxTurnTime = 30;

        public void Update(float deltaTime)
        {
            if (_noTime || _isEnd) return;

            _timer += deltaTime;
            if (1 <= _timer)
            {
                _turnTime -= _timer; _timer = 0;
                int remainingTime = Math.Max(0, (int)Math.Ceiling(_turnTime));
                OnTimeChanged?.Invoke((CurrentTeam, remainingTime));
                if (_turnTime <= 0) OnTurnEnd?.Invoke(); 
            }
        }
    }
}