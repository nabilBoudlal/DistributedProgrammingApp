using DistributedAppExamUnicam.GrainInterfaces; 


namespace Grains
{
    [Orleans.GenerateSerializer] 
    public class AppointmentState
    {
        [Orleans.Id(0)]
        public Guid PatientId { get; set; }

        [Orleans.Id(1)]
        public Guid DoctorId { get; set; }

        [Orleans.Id(2)]
        public Guid SlotId { get; set; }

        [Orleans.Id(3)]
        public DateTime AppointmentTime { get; set; }

        [Orleans.Id(4)]
        public TimeSpan Duration { get; set; }

        [Orleans.Id(5)]
        public string ReasonForVisit { get; set; } = string.Empty;

        [Orleans.Id(6)]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.PendingConfirmation;

        [Orleans.Id(7)]
        // Inizializza per evitare riferimenti null
        public List<string> DocumentUrls { get; set; } = new List<string>();

        [Orleans.Id(8)]
        // È nullable, quindi il controllo '?.CorrelationId' nel DTO è corretto
        public Guid? CorrelationId { get; set; }
    }

    public class AppointmentGrain : Grain, IAppointmentGrain
    {
        private readonly IPersistentState<AppointmentState> _state;

        public AppointmentGrain([PersistentState("appointmentState", "redisStorage")] IPersistentState<AppointmentState> state)
        {
            _state = state;
        }

        public async Task SetInitialDetails(
            Guid patientId,
            Guid doctorId,
            Guid slotId,
            DateTime appointmentTime,
            TimeSpan duration,
            string reasonForVisit,
            Guid? correlationId)
        {
            // Permette la re-inizializzazione se lo stato è default o pending,
            // per supportare i retry della saga.
            if (_state.State.Status != default(AppointmentStatus) && _state.State.Status != AppointmentStatus.PendingConfirmation)
            {
                throw new InvalidOperationException($"Appointment is in status {_state.State.Status} and cannot be re-initialized.");
            }

            _state.State.PatientId = patientId;
            _state.State.DoctorId = doctorId;
            _state.State.SlotId = slotId;
            _state.State.AppointmentTime = appointmentTime;
            _state.State.Duration = duration;
            _state.State.ReasonForVisit = reasonForVisit;
            _state.State.Status = AppointmentStatus.PendingConfirmation;
            _state.State.DocumentUrls ??= new List<string>(); 
            _state.State.CorrelationId = correlationId;

            await _state.WriteStateAsync();
        }

        public Task<AppointmentDetailsDTO> GetAppointmentDetails()
        {
            return Task.FromResult(new AppointmentDetailsDTO
            {
                PatientId = _state.State.PatientId,
                DoctorId = _state.State.DoctorId,
                SlotId = _state.State.SlotId,
                AppointmentTime = _state.State.AppointmentTime,
                Duration = _state.State.Duration,
                ReasonForVisit = _state.State.ReasonForVisit,
                CorrelationId =  (Guid) _state.State.CorrelationId,
                Status = _state.State.Status.ToString()
            });
        }


        public async Task Confirm()
        {
            if (_state.State.Status != AppointmentStatus.PendingConfirmation)
            {
                throw new InvalidOperationException($"Appointment status is {_state.State.Status}, expected PendingConfirmation to confirm.");
            }
            _state.State.Status = AppointmentStatus.Confirmed;
            await _state.WriteStateAsync();
        }

        public async Task Cancel()
        {
            if (_state.State.Status == AppointmentStatus.Completed)
            {
                throw new InvalidOperationException("Completed appointments cannot be canceled.");
            }
            _state.State.Status = AppointmentStatus.Canceled;
            await _state.WriteStateAsync();
        }

        public async Task MarkAsCompleted()
        {
            if (_state.State.Status != AppointmentStatus.Confirmed)
            {
                throw new InvalidOperationException($"Appointment status is {_state.State.Status}, expected Confirmed to mark as completed.");
            }
            _state.State.Status = AppointmentStatus.Completed;
            await _state.WriteStateAsync();
        }

        public Task<AppointmentState> GetAppointmentState()
        {
            return Task.FromResult(_state.State);
        }

        public async Task ClearStateAsync()
        {
            await _state.ClearStateAsync();
            _state.State = new AppointmentState(); // Reset l'oggetto stato dopo il clear
            DeactivateOnIdle();
        }
    }
}