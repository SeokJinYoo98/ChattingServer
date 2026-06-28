using System;
using System.Collections.Generic;
using Yujanggi.Core.Domain;
namespace Yujanggi.Core.Match
{
    public class Record
    {
        public event Action<int, int>  OnRecordChanged;
        public bool IsLive          => Count - 1 == _currIdx;
        public int CurrTurn         => _currIdx + 1;
        public int Count            => _records.Count;
        public int TotalTurn        => _records.Count + 1;
        // 현재 화면에 적용된 마지막 기록 Index
        private int  _currIdx = -1;
        private bool _replay = false;
        private readonly List<MoveContext> _records = new(100);
        public void StartGame()
        {
            _records.Clear();
            _currIdx = -1;
            OnRecordChanged?.Invoke(CurrTurn, TotalTurn);
        }
        public bool TryGetMoveCtx(int idx, out MoveContext context)
        {
            if (idx < 0 || _records.Count <= idx)
            {
                context = default;
                return false;
            }
            _currIdx = idx;
            context  = _records[_currIdx];
            OnRecordChanged?.Invoke(CurrTurn, TotalTurn);
            return true;
        }

  
        public void Push(MoveContext context)
        {
            _records.Add(context);

            if (!_replay) _currIdx = _records.Count - 1;
            
            OnRecordChanged?.Invoke(CurrTurn, TotalTurn);
        }
        public bool TryPop(out MoveContext context)
        {
            if (_records.Count == 0)
            {
                context = default;
                return false;
            }

            int lastIdx = _records.Count - 1;
            context = _records[lastIdx];
            _records.RemoveAt(lastIdx);

            if (!_replay) _currIdx = _records.Count - 1;

            OnRecordChanged?.Invoke(CurrTurn, TotalTurn);
            return true;
        }
        public bool TryPeek(out MoveContext context)
        {
            if (_records.Count == 0)
            {
                context = default;
                return false;
            }

            context = _records[^1];
            return true;
        }
        public void EnterReplay() => _replay = true;
        public void ExitReplay() => _replay = false;
    }
}