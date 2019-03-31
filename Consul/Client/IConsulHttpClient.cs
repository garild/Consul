using System.Threading.Tasks;

namespace Consul.Client
{
    public interface IConsulHttpClient
    {
        Task<T> GetAsync<T>(string requestUri);
    }
}