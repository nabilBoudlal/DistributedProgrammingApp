using GrainInterfaces;
using Orleans;
using Orleans.Providers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Grains
{

    public class UserGrain : Grain, IUserGrain
     {
        private readonly IPersistentState<UserState> _state;

        public UserGrain([PersistentState("state", "redisStorage")] IPersistentState<UserState> state)
        {
            _state = state;
        }

        public Task<string> GetNameAsync()
        {
            return Task.FromResult(_state.State.name);
        } 


        public Task SetNameAsync(string name)
        {
            _state.State.name = name;
            return _state.WriteStateAsync();
        }

        public Task AddAppointmentAsync(Guid appointmentId)
        {
            if (!_state.State.appointments.Contains(appointmentId))
            {
                _state.State.appointments.Add(appointmentId);
                return _state.WriteStateAsync();
            }
          return Task.CompletedTask;
            
        }

        public Task<List<Guid>> GetAppointmentsAsync()
        {
            return Task.FromResult(_state.State.appointments);
        }

        public async Task ClearStateAsync()
        {
            await _state.ClearStateAsync();

            // Reset in-memory state a default
            _state.State = new UserState();

            // Per sicurezza scrivi anche lo stato "vuoto" (opzionale)
            await _state.WriteStateAsync();

            // Disattiva il grain così si ricarica da zero al prossimo accesso
            DeactivateOnIdle();
        }

        public async Task RemoveAppointmentAsync(Guid appointmentId)
        {
            _state.State.appointments.Remove(appointmentId);
            await _state.WriteStateAsync();
        }


    }

    public class UserState
    {
        public string name { get; set; } = string.Empty;
        public List<Guid> appointments { get; set; } = new();
    }
}
