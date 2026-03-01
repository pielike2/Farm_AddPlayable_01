using System.Collections.Generic;
using Base.ServiceSystem;
using Utility;

namespace Playable.Services
{
    public class TickByDemandService : Service, ITickHandler
    {
        private static readonly HashSet<ITickByDemand> _tickTargets = new HashSet<ITickByDemand>();
        private static readonly Dictionary<ITickByDemand, int> _tickTargetCounters = new Dictionary<ITickByDemand, int>();
        
        public void OnTick()
        {
            foreach (var item in _tickTargets)
                item.Tick();
        }

        public void AddTickTarget(ITickByDemand target)
        {
            if (_tickTargets.Add(target))
            {
                target.EnterTick();
                _tickTargetCounters[target] = 1;
            }
            else
            {
                _tickTargetCounters[target]++;
            }
        }

        public void RemoveTickTarget(ITickByDemand target)
        {
            _tickTargetCounters[target]--;
            if (_tickTargetCounters[target] <= 0 && _tickTargets.Remove(target))
            {
                target.ExitTick();
                _tickTargetCounters.Remove(target);
            }
        }
    }
}