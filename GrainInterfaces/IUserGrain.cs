using Orleans;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    public interface IUserGrain : IGrainWithStringKey
    {
        Task<string> GetNameAsync();
        Task SetNameAsync(string name);
    }
}