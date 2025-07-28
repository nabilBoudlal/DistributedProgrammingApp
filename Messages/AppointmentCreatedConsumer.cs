using GrainInterfaces;
using MassTransit;

namespace DistributedAppExamUnicam.Messages
{
    public class AppointmentCreatedConsumer : IConsumer<AppointmentCreatedMessage>
    {
        private readonly IGrainFactory _grainFactory;

        public AppointmentCreatedConsumer(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public async Task Consume(ConsumeContext<AppointmentCreatedMessage> context)
        {
            var message = context.Message;

            Console.WriteLine($"[Consumer] Appointment received: {message.Title} for PatientId {message.UserId}");

            if (!Guid.TryParse(message.UserId, out var userGuid))
            {
                Console.WriteLine("[Consumer] Invalid PatientId in message.");
                return;
            }

            if (!message.AppointmentId.HasValue)
            {
                Console.WriteLine("[Consumer] Missing AppointmentId in message.");
                return;
            }

            var userGrain = _grainFactory.GetGrain<IPatientGrain>(userGuid);
            await userGrain.AddAppointmentAsync(message.AppointmentId.Value);
        }

    }
}