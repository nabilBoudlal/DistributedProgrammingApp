using System;
using System.Collections.Generic; 

namespace DistributedAppExamUnicam.Messages
{
    // --- COMMANDS --- (Richieste di azione)

    public interface IBookAppointmentCommand
    {
        Guid CorrelationId { get; }
        Guid AppointmentId { get; }
        Guid PatientId { get; }
        Guid DoctorId { get; }
        Guid SlotId { get; }
        DateTime AppointmentTime { get; }
        TimeSpan Duration { get; }
        string ReasonForVisit { get; }
    }

    public class BookAppointmentCommand : IBookAppointmentCommand
    {
        public Guid CorrelationId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public DateTime AppointmentTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string ReasonForVisit { get; set; } = string.Empty;
    }

    public interface IConfirmAppointmentCommand
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }
    }

    public class ConfirmAppointmentCommand : IConfirmAppointmentCommand
    {
        public Guid AppointmentId { get; set; }

        public Guid CorrelationId { get; set; }
    }

    public interface ICancelAppointmentCommand
    {
        Guid AppointmentId { get; }
        string? Reason { get; }
        Guid CorrelationId { get; }
    }

    public class CancelAppointmentCommand : ICancelAppointmentCommand
    {
        public Guid AppointmentId { get; set; }
        public string? Reason { get; set; }
        public Guid CorrelationId { get; set; }
    }

    public interface IMarkAppointmentCompletedCommand
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }

    }

    public class MarkAppointmentCompletedCommand : IMarkAppointmentCompletedCommand
    {
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
    }

    public interface IDeleteAppointmentCommand
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }
    }

    public class DeleteAppointmentCommand : IDeleteAppointmentCommand
    {
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
    }

    // Comandi specifici per la gestione degli slot (consumati dalla Saga)
    public interface IReserveTimeSlotCommand
    {
        Guid CorrelationId { get; }
        Guid DoctorId { get; }
        Guid SlotId { get; }
        Guid AppointmentId { get; } // Per collegare la riserva all'appuntamento specifico
        Guid PatientId { get; } // Aggiunto per consentire al DoctorGrain di assegnare il paziente

    }

    public class ReserveTimeSlotCommand : IReserveTimeSlotCommand
    {
        public Guid CorrelationId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
    }

    public interface IReleaseTimeSlotCommand
    {
        Guid CorrelationId { get; }
        Guid DoctorId { get; }
        Guid SlotId { get; }
        Guid AppointmentId { get; }
    }

    public class ReleaseTimeSlotCommand : IReleaseTimeSlotCommand
    {
        public Guid CorrelationId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public Guid AppointmentId { get; set; }
    }

    // --- EVENTS --- (Notifiche di eventi accaduti)

    public interface IAppointmentInitializedEvent
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }
        Guid PatientId { get; }
        Guid DoctorId { get; }
        Guid SlotId { get; }
        DateTime AppointmentTime { get; }
        TimeSpan Duration { get; }
        string ReasonForVisit { get; }
    }

    public class AppointmentInitializedEvent : IAppointmentInitializedEvent
    {
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public DateTime AppointmentTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string ReasonForVisit { get; set; } = string.Empty;
    }

    public interface IAppointmentConfirmedEvent
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }
    }

    public class AppointmentConfirmedEvent : IAppointmentConfirmedEvent
    {
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
    }

    public interface IAppointmentCanceledEvent
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }
        string? Reason { get; }
    }

    public class AppointmentCanceledEvent : IAppointmentCanceledEvent
    {
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
        public string? Reason { get; set; }
    }

    public interface IAppointmentCompletedEvent
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }
        DateTime CompletionTime { get; }
    }

    public class AppointmentDeletedEvent : IAppointmentDeletedEvent
    {
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
    }

    public interface IAppointmentDeletedEvent
    {
        Guid AppointmentId { get; }
        Guid CorrelationId { get; }
    }

    public class AppointmentCompletedEvent : IAppointmentCompletedEvent
    {
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTime CompletionTime { get; set; }
    }
    public interface ITimeSlotReservedEvent
    {
        Guid CorrelationId { get; }
        Guid DoctorId { get; }
        Guid SlotId { get; }
        Guid AppointmentId { get; }
    }

    public class TimeSlotReservedEvent : ITimeSlotReservedEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public Guid AppointmentId { get; set; }
    }

    public interface ITimeSlotReservationFailedEvent
    {
        Guid CorrelationId { get; }
        Guid DoctorId { get; }
        Guid SlotId { get; }
        Guid AppointmentId { get; }
        string Reason { get; }
    }

    public class TimeSlotReservationFailedEvent : ITimeSlotReservationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public Guid AppointmentId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public interface ITimeSlotReleasedEvent
    {
        Guid CorrelationId { get; }
        Guid DoctorId { get; }
        Guid SlotId { get; }
        Guid AppointmentId { get; }
    }

    public class TimeSlotReleasedEvent : ITimeSlotReleasedEvent
    {
        public Guid CorrelationId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; }
        public Guid AppointmentId { get; set; }
    }
}