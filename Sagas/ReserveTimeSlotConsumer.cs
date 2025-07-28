using DistributedAppExamUnicam.GrainInterfaces;
using DistributedAppExamUnicam.Messages;
using MassTransit;

namespace DistributedAppExamUnicam.Sagas
{
    public class ReserveTimeSlotConsumer : IConsumer<IReserveTimeSlotCommand>
    {
        private readonly IGrainFactory _grainFactory;

        public ReserveTimeSlotConsumer(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public async Task Consume(ConsumeContext<IReserveTimeSlotCommand> context)
        {
            Console.WriteLine($"ReserveTimeSlotConsumer: Received command for SlotId: {context.Message.SlotId}, DoctorId: {context.Message.DoctorId}, CorrelationId: {context.Message.CorrelationId}");

            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(context.Message.DoctorId);
            var result = await doctorGrain.TryReserveTimeSlot(context.Message.SlotId, context.Message.AppointmentId, context.Message.PatientId);

            if (result == ReserveTimeSlotResult.Success)
            {
                // Pubblica l'evento di successo
                await context.Publish<ITimeSlotReservedEvent>(new TimeSlotReservedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    DoctorId = context.Message.DoctorId,
                    SlotId = context.Message.SlotId,
                    AppointmentId = context.Message.AppointmentId
                });
                Console.WriteLine($"ReserveTimeSlotConsumer: Slot {context.Message.SlotId} successfully reserved for AppointmentId: {context.Message.AppointmentId}");
            }
            else
            {
                // Pubblica l'evento di fallimento con il motivo
                string reason;
                switch (result)
                {
                    case ReserveTimeSlotResult.NotFound:
                        reason = "Slot not found.";
                        break;
                    case ReserveTimeSlotResult.AlreadyReserved:
                        reason = "Slot already reserved by another party.";
                        break;
                    case ReserveTimeSlotResult.Overlap:
                        reason = "Slot overlaps with existing reservation.";
                        break;
                    default:
                        reason = "Unknown reservation error.";
                        break;
                }

                await context.Publish<ITimeSlotReservationFailedEvent>(new TimeSlotReservationFailedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    DoctorId = context.Message.DoctorId,
                    SlotId = context.Message.SlotId,
                    AppointmentId = context.Message.AppointmentId,
                    Reason = reason
                });
                Console.WriteLine($"ReserveTimeSlotConsumer: Slot {context.Message.SlotId} reservation FAILED for AppointmentId: {context.Message.AppointmentId}. Reason: {reason}");
            }
        }
    }
        public class ReleaseTimeSlotConsumer : IConsumer<IReleaseTimeSlotCommand>
        {
            private readonly IGrainFactory _grainFactory;

            public ReleaseTimeSlotConsumer(IGrainFactory grainFactory)
            {
                _grainFactory = grainFactory;
            }

            public async Task Consume(ConsumeContext<IReleaseTimeSlotCommand> context)
            {
                Console.WriteLine($"ReleaseTimeSlotConsumer: Received command for SlotId: {context.Message.SlotId}, DoctorId: {context.Message.DoctorId}, CorrelationId: {context.Message.CorrelationId}");

                var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(context.Message.DoctorId);
                await doctorGrain.ReleaseTimeSlot(context.Message.SlotId, context.Message.AppointmentId);

                // Pubblica un evento per notificare che lo slot è stato rilasciato
                await context.Publish<ITimeSlotReleasedEvent>(new TimeSlotReleasedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    DoctorId = context.Message.DoctorId,
                    SlotId = context.Message.SlotId,
                    AppointmentId = context.Message.AppointmentId
                });
                Console.WriteLine($"ReleaseTimeSlotConsumer: Slot {context.Message.SlotId} released for AppointmentId: {context.Message.AppointmentId}");
            }
        }
    
}
