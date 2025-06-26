using GrainInterfaces;
using Orleans;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Grains
{
    public class UserGrain : Grain, IUserGrain
    {
        private string _name = "Default";

        public Task<string> GetNameAsync() => Task.FromResult(_name);

        public Task SetNameAsync(string name)
        {
            _name = name;
            return Task.CompletedTask;
        }
    }
}
