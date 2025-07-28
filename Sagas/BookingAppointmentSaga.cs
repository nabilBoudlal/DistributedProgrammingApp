using DistributedAppExamUnicam.GrainInterfaces;
using DistributedAppExamUnicam.Messages;
using MassTransit;

namespace DistributedAppExamUnicam.Sagas
{
    public class BookingAppointmentSaga : MassTransitStateMachine<BookingState>
    {


        private readonly IGrainFactory _grainFactory;

        // --- STATI ---
        // Questi rappresentano i diversi punti nel ciclo di vita della prenotazione.
        public State Initial { get; private set; } 
        public State AwaitingSlotReservation { get; private set; } 
        public State AppointmentInitialized { get; private set; } 
        public State Confirmed { get; private set; } 
        public State Canceled { get; private set; } 
        public State Completed { get; private set; } 
        public State Faulted { get; private set; } 

        // --- EVENTI ---
        // Ogni Event corrisponde a un messaggio MassTransit che la saga può ricevere.
        public Event<IBookAppointmentCommand> BookAppointmentCommandReceived { get; private set; }
        public Event<ITimeSlotReservedEvent> TimeSlotReserved { get; private set; }
        public Event<ITimeSlotReservationFailedEvent> TimeSlotReservationFailed { get; private set; }
      
        public Event<IConfirmAppointmentCommand> ConfirmAppointmentCommandReceived { get; private set; }
        public Event<ICancelAppointmentCommand> CancelAppointmentCommandReceived { get; private set; }
        public Event<IMarkAppointmentCompletedCommand> MarkAppointmentCompletedCommandReceived { get; private set; }
        public Event<IDeleteAppointmentCommand> DeleteAppointmentCommandReceived { get; private set; }

        public BookingAppointmentSaga(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;

            // Indica a MassTransit quale proprietà nello stato della saga rappresenta lo stato corrente.
            InstanceState(x => x.CurrentState);

            // --- Correlazione degli Eventi con le istanze della Saga ---
            // Ogni evento deve "sapere" a quale istanza di saga appartiene.

            Event(() => BookAppointmentCommandReceived, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => TimeSlotReserved, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => TimeSlotReservationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            
            Event(() => ConfirmAppointmentCommandReceived, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => CancelAppointmentCommandReceived, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MarkAppointmentCompletedCommandReceived, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => DeleteAppointmentCommandReceived, x => x.CorrelateById(context => context.Message.CorrelationId));


            // --- DEFINIZIONE DELLE TRANSIZIONI DI STATO ---

            // 1. Inizio della Saga: Ricezione del comando di prenotazione
            Initially(
                When(BookAppointmentCommandReceived)
                    .Then(context =>
                    {
                        // Inizializza lo stato della saga con i dati ricevuti nel comando
                        context.Saga.AppointmentId = context.Message.AppointmentId;
                        context.Saga.PatientId = context.Message.PatientId;
                        context.Saga.DoctorId = context.Message.DoctorId;
                        context.Saga.SlotId = context.Message.SlotId;
                        context.Saga.AppointmentTime = context.Message.AppointmentTime;
                        context.Saga.Duration = context.Message.Duration;
                        context.Saga.ReasonForVisit = context.Message.ReasonForVisit;
                        context.Saga.CreatedTimestamp = DateTime.UtcNow;
                        Console.WriteLine($"Saga {context.Saga.CorrelationId}: Received BookAppointmentCommand for AppointmentId: {context.Saga.AppointmentId}. Transitioning to AwaitingSlotReservation.");
                    })
                    // Invia un comando al DoctorGrain (tramite MassTransit) per riservare lo slot
                    .Publish(context => new ReserveTimeSlotCommand // Questo comando sarà consumato da ReserveTimeSlotConsumer
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        DoctorId = context.Saga.DoctorId,
                        SlotId = context.Saga.SlotId,
                        AppointmentId = context.Saga.AppointmentId,
                        PatientId = context.Saga.PatientId
                    })
                    .TransitionTo(AwaitingSlotReservation)
            );

            // 2. Stato: In attesa della prenotazione dello slot
            During(AwaitingSlotReservation,
                // Se lo slot è stato riservato con successo
                When(TimeSlotReserved)
                    .ThenAsync(async context =>
                    {
                        // Inizializza l'AppointmentGrain con i dettagli della prenotazione
                        var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(context.Saga.AppointmentId);
                        await appointmentGrain.SetInitialDetails(
                            context.Saga.PatientId,
                            context.Saga.DoctorId,
                            context.Saga.SlotId,
                            context.Saga.AppointmentTime,
                            context.Saga.Duration,
                            context.Saga.ReasonForVisit,
                            context.Saga.CorrelationId
                        );
                        Console.WriteLine($"Saga {context.Saga.CorrelationId}: AppointmentGrain initialized for AppointmentId: {context.Saga.AppointmentId}. Transitioning to AppointmentInitialized.");

                        await context.Publish(new AppointmentInitializedEvent
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            AppointmentId = context.Saga.AppointmentId,
                            PatientId = context.Saga.PatientId,
                            DoctorId = context.Saga.DoctorId,
                            SlotId = context.Saga.SlotId,
                            AppointmentTime = context.Saga.AppointmentTime,
                            Duration = context.Saga.Duration,
                            ReasonForVisit = context.Saga.ReasonForVisit
                        });
                    })
                    .TransitionTo(AppointmentInitialized), // L'appuntamento è stato creato, ma non ancora "confermato" al paziente

                // Se la prenotazione dello slot fallisce
                When(TimeSlotReservationFailed)
                    .Then(context =>
                    {
                        context.Saga.FailureReason = $"Slot reservation failed: {context.Message.Reason}";
                        context.Saga.UpdatedTimestamp = DateTime.UtcNow;
                        Console.WriteLine($"Saga {context.Saga.CorrelationId}: Slot reservation failed for AppointmentId: {context.Saga.AppointmentId}. Reason: {context.Message.Reason}. Transitioning to Faulted.");

                        // Notifica il fallimento al mondo esterno 
                        context.Publish(new AppointmentCanceledEvent 
                        {
                            AppointmentId = context.Saga.AppointmentId,
                            CorrelationId = context.Saga.CorrelationId,
                            Reason = context.Saga.FailureReason
                        });
                    })
                    .TransitionTo(Faulted) // La saga termina in stato di errore
            );

            // 3. Stato: Appuntamento Inizializzato (in attesa di conferma finale)
            During(AppointmentInitialized,
                When(ConfirmAppointmentCommandReceived)
                    .ThenAsync(async context =>
                    {
                        var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(context.Saga.AppointmentId);
                        await appointmentGrain.Confirm();
                        context.Saga.UpdatedTimestamp = DateTime.UtcNow;
                        Console.WriteLine($"Saga {context.Saga.CorrelationId}: Appointment {context.Saga.AppointmentId} confirmed. Transitioning to Confirmed.");

                        await context.Publish(new AppointmentConfirmedEvent
                        {
                            AppointmentId = context.Saga.AppointmentId,
                            CorrelationId = context.Saga.CorrelationId
                        });
                    })
                    .TransitionTo(Confirmed),

                // Permetti l'annullamento anche se l'appuntamento è solo inizializzato
                When(CancelAppointmentCommandReceived)
                    .ThenAsync(async context =>
                    {
                        await HandleAppointmentCancellation(context.Saga, context.Message.AppointmentId, context.Message.Reason, context);
                    })
                    .TransitionTo(Canceled)
            );


            // 4. Stato: Appuntamento Confermato (Pronto per la visita)
            During(Confirmed,
                When(MarkAppointmentCompletedCommandReceived)
                    .ThenAsync(async context =>
                    {
                        var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(context.Saga.AppointmentId);
                        await appointmentGrain.MarkAsCompleted();
                        context.Saga.UpdatedTimestamp = DateTime.UtcNow;
                        Console.WriteLine($"Saga {context.Saga.CorrelationId}: Appointment {context.Saga.AppointmentId} marked as completed. Transitioning to Completed.");

                        await context.Publish(new AppointmentCompletedEvent
                        {
                            AppointmentId = context.Saga.AppointmentId,
                            CorrelationId = context.Saga.CorrelationId,
                            CompletionTime = DateTime.UtcNow
                        });
                    })
                    .TransitionTo(Completed),

                // Permetti l'annullamento anche da stato "Confirmed"
                When(CancelAppointmentCommandReceived)
                    .ThenAsync(async context =>
                    {
                        await HandleAppointmentCancellation(context.Saga, context.Message.AppointmentId, context.Message.Reason, context);
                    })
                    .TransitionTo(Canceled)
            );

           
            During(Confirmed, Canceled, Completed, Faulted, 
                When(DeleteAppointmentCommandReceived)
                    .ThenAsync(async context =>
                    {
                        var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(context.Saga.AppointmentId);
                        
                        await appointmentGrain.ClearStateAsync();
                        Console.WriteLine($"Saga {context.Saga.CorrelationId}: Appointment {context.Saga.AppointmentId} state cleared (deleted).");
                        /
                         context.Publish(new AppointmentDeletedEvent
                         {
                             AppointmentId = context.Saga.AppointmentId,
                             CorrelationId = context.Saga.CorrelationId,
                             
                         });
                    })
                    .Finalize() 
            );

            SetCompletedWhenFinalized();
        }

      
        private async Task HandleAppointmentCancellation(
            BookingState sagaState,
            Guid appointmentId,
            string? reason,
            BehaviorContext<BookingState, ICancelAppointmentCommand> context)
        {
            var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(appointmentId);
            await appointmentGrain.Cancel(); 

     
            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(sagaState.DoctorId);
            await doctorGrain.ReleaseTimeSlot(sagaState.SlotId, sagaState.AppointmentId);

            sagaState.UpdatedTimestamp = DateTime.UtcNow;
            Console.WriteLine($"Saga {sagaState.CorrelationId}: Appointment {appointmentId} canceled from {sagaState.CurrentState} state.");

            await context.Publish(new AppointmentCanceledEvent
            {
                AppointmentId = appointmentId,
                CorrelationId = sagaState.CorrelationId,
                Reason = reason ?? "Canceled by user/system"
            });
        }
    }
}
