using System.Linq;

namespace Base.ServiceSystem
{
    public class ServicesUpdater
    {
        private readonly ITickHandler[] _tickHandlers;
        private readonly ILateTickHandler[] _lateTickHandlers;
        private readonly IFixedTickHandler[] _fixedTickHandlers;
        
        public ServicesUpdater(ServiceLocator locator)
        {
            _tickHandlers = locator.ServicesByPriority.OfType<ITickHandler>().ToArray();
            _lateTickHandlers = locator.ServicesByPriority.OfType<ILateTickHandler>().ToArray();
            _fixedTickHandlers = locator.ServicesByPriority.OfType<IFixedTickHandler>().ToArray();
        }

        public void Tick()
        {
            foreach (var handler in _tickHandlers)
                if (handler.IsEnabled)
                    handler.OnTick();
        }

        public void LateTick()
        {
            foreach (var handler in _lateTickHandlers)
                if (handler.IsEnabled)
                    handler.OnLateTick();
        }

        public void FixedTick()
        {
            foreach (var handler in _fixedTickHandlers)
                if (handler.IsEnabled)
                    handler.OnFixedTick();
        }
    }
}