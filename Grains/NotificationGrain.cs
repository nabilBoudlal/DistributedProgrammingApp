using DistributedAppExamUnicam.GrainInterfaces;
using Event;
using Orleans.Streams;
using System.IO;

namespace DistributedAppExamUnicam.Grains
{
    [ImplicitStreamSubscription("appointment-events")]
    public class NotificationGrain : Grain, INotificationGrain, IAsyncObserver<AppointmentCreatedEvent>
    {
        private readonly IPersistentState<NotificationState>? _state;

        public NotificationGrain([PersistentState("notifications", "redisStorage")] IPersistentState<NotificationState> state)
        {
            _state = state;
        }
        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var streamProvider = this.GetStreamProvider("AppointmentStream");

            var streamId = StreamId.Create("appointment-events", this.GetPrimaryKey());
            var stream = streamProvider.GetStream<AppointmentCreatedEvent>(streamId);


            await stream.SubscribeAsync(this);

            await base.OnActivateAsync(cancellationToken);
        }

        public Task OnCompletedAsync() => Task.CompletedTask;

        public Task OnErrorAsync(Exception ex)
        {
            Console.WriteLine($"Stream Error {ex.Message}");
            return Task.CompletedTask;
        }

        public async Task OnNextAsync(AppointmentCreatedEvent item, StreamSequenceToken? token = null)
        {
            var message = $"Event: {item.Title} on {item.Date}";
            _state.State.Messages.Add(message);
            await _state.WriteStateAsync(); 
        }

        public Task ReceiveEvent(AppointmentCreatedEvent evt)
        {
            var message = $"Event received directly: {evt.Title} on {evt.Date}";
            _state.State.Messages.Add(message);
            return _state.WriteStateAsync();
        }

        public Task<List<string>> GetNotifications()
        {
            return Task.FromResult(_state.State.Messages ?? new List<string>());

        }
    }
}
