using GrainInterfaces;
using Orleans;
using Orleans.Providers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Grains
{

    public class PatientGrain : Grain, IPatientGrain
     {
        private readonly IPersistentState<PatientState> _state;

        public PatientGrain([PersistentState("state", "redisStorage")] IPersistentState<PatientState> state)
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



        public async Task ClearStateAsync()
        {
            await _state.ClearStateAsync();

            // Reset in-memory state a default
            _state.State = new PatientState();

            // Per sicurezza scrivi anche lo stato "vuoto" (opzionale)
            await _state.WriteStateAsync();

            // Disattiva il grain così si ricarica da zero al prossimo accesso
            DeactivateOnIdle();
        }

        public async Task RemoveAppointmentAsync(Guid appointmentId)
        {
            if (_state.State.appointments.Remove(appointmentId))
            {
                await _state.WriteStateAsync();
                Console.WriteLine($"Patient {this.GetPrimaryKeyString()}: Removed appointment {appointmentId}. Total appointments: {_state.State.appointments.Count}");
            }
        }

        public async Task AddAppointmentAsync(Guid appointmentId)
        {
            if (!_state.State.appointments.Contains(appointmentId))
            {
                _state.State.appointments.Add(appointmentId);
                await _state.WriteStateAsync();
                Console.WriteLine($"Patient {this.GetPrimaryKeyString()}: Added appointment {appointmentId}. Total appointments: {_state.State.appointments.Count}");
            }
        }

        public Task<List<Guid>> GetAppointmentsAsync()
        {
            return Task.FromResult(_state.State.appointments);
        }
    }

    public class PatientState
    {
        public string name { get; set; } = string.Empty;
        public List<Guid> appointments { get; set; } = new();

    }


}
