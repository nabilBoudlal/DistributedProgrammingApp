using MassTransit;

namespace DistributedAppExamUnicam.Sagas
{
    public class BookingState : SagaStateMachineInstance, ISagaVersion
    {
        // MassTransit usa CorrelationId come chiave per recuperare lo stato della saga.
        public Guid CorrelationId { get; set; }

        // Lo stato corrente della State Machine. MassTransit lo gestisce internamente.
        public string CurrentState { get; set; } = null!; 

        // --- Dati specifici della prenotazione che la saga deve mantenere ---
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public DateTime AppointmentTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string ReasonForVisit { get; set; } = string.Empty;
        public int Version { get; set; }

        public DateTime? CreatedTimestamp { get; set; }
        public DateTime? UpdatedTimestamp { get; set; }
        public string? FailureReason { get; set; } // Per salvare il motivo di un eventuale fallimento
    }
}
