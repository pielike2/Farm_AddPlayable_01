using Base;
using Base.ServiceSystem;

namespace Playable.Services
{
    public class ServicesProvider : BaseServicesProvider
    {
        protected override void CreatePlainServices()
        {
            CreatePlainService<TickByDemandService>();
        }
    }
}