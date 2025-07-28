using DistributedAppExamUnicam.GrainInterfaces;
using GrainInterfaces;
using MassTransit;

namespace DistributedAppExamUnicam.Messages
{
   
        public class PatientAppointmentRegistrarConsumer : IConsumer<IAppointmentInitializedEvent>,
                                                          IConsumer<IAppointmentCanceledEvent>,
                                                          IConsumer<IAppointmentCompletedEvent>,
                                                          IConsumer<IAppointmentDeletedEvent> 
        {
            private readonly IGrainFactory _grainFactory;

            public PatientAppointmentRegistrarConsumer(IGrainFactory grainFactory)
            {
                _grainFactory = grainFactory;
            }

            public async Task Consume(ConsumeContext<IAppointmentInitializedEvent> context)
            {
                Console.WriteLine($"PatientAppointmentRegistrarConsumer: Received AppointmentInitializedEvent for PatientId: {context.Message.PatientId}, AppointmentId: {context.Message.AppointmentId}");
                var patientGrain = _grainFactory.GetGrain<IPatientGrain>(context.Message.PatientId);
                await patientGrain.AddAppointmentAsync(context.Message.AppointmentId);
                Console.WriteLine($"PatientAppointmentRegistrarConsumer: Appointment {context.Message.AppointmentId} registered with Patient {context.Message.PatientId}.");
            }

            public async Task Consume(ConsumeContext<IAppointmentCanceledEvent> context)
            {
                Console.WriteLine($"PatientAppointmentRegistrarConsumer: Received AppointmentCanceledEvent for PatientId: (lookup needed), AppointmentId: {context.Message.AppointmentId}");
               
                var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(context.Message.AppointmentId);
                var appointmentDetails = await appointmentGrain.GetAppointmentDetails(); // Assumi questo metodo esista

                if (appointmentDetails != null && appointmentDetails.PatientId != Guid.Empty)
                {
                    var patientGrain = _grainFactory.GetGrain<IPatientGrain>(appointmentDetails.PatientId);
                    await patientGrain.RemoveAppointmentAsync(context.Message.AppointmentId);
                    Console.WriteLine($"PatientAppointmentRegistrarConsumer: Appointment {context.Message.AppointmentId} removed from Patient {appointmentDetails.PatientId} due to cancellation.");
                }
                else
                {
                    Console.WriteLine($"PatientAppointmentRegistrarConsumer: Could not find PatientId for canceled appointment {context.Message.AppointmentId}.");
                }
            }

            public async Task Consume(ConsumeContext<IAppointmentCompletedEvent> context)
            {
                Console.WriteLine($"PatientAppointmentRegistrarConsumer: Received AppointmentCompletedEvent for PatientId: (lookup needed), AppointmentId: {context.Message.AppointmentId}");
                
                var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(context.Message.AppointmentId);
                var appointmentDetails = await appointmentGrain.GetAppointmentDetails();

                if (appointmentDetails != null && appointmentDetails.PatientId != Guid.Empty)
                {
                    var patientGrain = _grainFactory.GetGrain<IPatientGrain>(appointmentDetails.PatientId);
                    await patientGrain.RemoveAppointmentAsync(context.Message.AppointmentId);
                    Console.WriteLine($"PatientAppointmentRegistrarConsumer: Appointment {context.Message.AppointmentId} marked as completed for Patient {appointmentDetails.PatientId}.");
                }
            }

            public async Task Consume(ConsumeContext<IAppointmentDeletedEvent> context)
            {
                Console.WriteLine($"PatientAppointmentRegistrarConsumer: Received AppointmentDeletedEvent for PatientId: (lookup needed), AppointmentId: {context.Message.AppointmentId}");
                var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(context.Message.AppointmentId);
                var appointmentDetails = await appointmentGrain.GetAppointmentDetails();

                if (appointmentDetails != null && appointmentDetails.PatientId != Guid.Empty)
                {
                    var patientGrain = _grainFactory.GetGrain<IPatientGrain>(appointmentDetails.PatientId);
                    await patientGrain.RemoveAppointmentAsync(context.Message.AppointmentId);
                    Console.WriteLine($"PatientAppointmentRegistrarConsumer: Appointment {context.Message.AppointmentId} completely removed from Patient {appointmentDetails.PatientId}.");
                }
            }
        }
}