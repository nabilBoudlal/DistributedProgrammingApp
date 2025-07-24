using Event;
using GrainInterfaces;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Grains
{
    public class AppointmentGrain : Grain, IAppointmentGrain
    {
        private readonly IPersistentState<AppointmentState> _state;
        private IAsyncStream<Event.AppointmentCreatedEvent>? _stream;


        public AppointmentGrain([PersistentState("state", "redisStorage")] IPersistentState<AppointmentState> state)
        {
            _state = state;

        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var streamProvider = this.GetStreamProvider("AppointmentStream");

            var streamId = StreamId.Create("appointment-events", this.GetPrimaryKey());
            _stream = streamProvider.GetStream<AppointmentCreatedEvent>(streamId);

            return base.OnActivateAsync(cancellationToken);
        }

        public async Task SetDetails(string title, DateTime date, string description, Guid userId)
        {
            _state.State.Title = title;
            _state.State.Date = date;
            _state.State.Description = description;
            _state.State.UserId = userId; 
            await _state.WriteStateAsync();

            if (_stream != null)
            {
                var evt = new AppointmentCreatedEvent(this.GetPrimaryKey(), title, date);
                await _stream.OnNextAsync(evt);
            }
        }


        public Task<(string Title, DateTime Date, string Description, Guid UserId)> GetDetails()
        {
            return Task.FromResult((
                _state.State.Title,
                _state.State.Date,
                _state.State.Description,
                _state.State.UserId));
        }


        public async Task ClearStateAsync()
        {
            await _state.ClearStateAsync();

            // Reset in-memory state a default
            _state.State = new AppointmentState();

            // Per sicurezza scrivi anche lo stato "vuoto" (opzionale)
            await _state.WriteStateAsync();

            // Disattiva il grain così si ricarica da zero al prossimo accesso
            DeactivateOnIdle();
        }




    }

    public class AppointmentState
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;

        public Guid UserId { get; set; }
    }
}