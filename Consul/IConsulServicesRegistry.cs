using System.Threading.Tasks;
using Consul;

namespace Consul
{
    public interface IConsulServicesRegistry
    {
        Task<AgentService> GetAsync(string name);
    }

}


