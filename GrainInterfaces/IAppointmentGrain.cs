using Grains;

namespace DistributedAppExamUnicam.GrainInterfaces
{
    public interface IAppointmentGrain : Orleans.IGrainWithGuidKey
    {
        Task SetInitialDetails(
            Guid patientId,
            Guid doctorId,
            Guid slotId,
            DateTime appointmentTime,
            TimeSpan duration,
            string reasonForVisit,
            Guid? correlationId);

        Task<AppointmentDetailsDTO> GetAppointmentDetails();

        Task Confirm();
        Task Cancel();
        Task MarkAsCompleted();
        Task<AppointmentState> GetAppointmentState();
        Task ClearStateAsync();
    }

    [GenerateSerializer]
    public enum AppointmentStatus
    {
        [Id(0)] PendingConfirmation,
        [Id(1)] Confirmed,
        [Id(2)] Canceled,
        [Id(3)] Completed,
        [Id(4)] Failed // Aggiunto per gestire gli scenari di errore della saga
    }

    [GenerateSerializer]
    public class AppointmentDetailsDTO
    {
        [Id(0)]
        public Guid PatientId { get; set; }

        [Id(1)]
        public Guid DoctorId { get; set; }

        [Id(2)]
        public Guid SlotId { get; set; }

        [Id(3)]
        public DateTime AppointmentTime { get; set; }

        [Id(4)]
        public TimeSpan Duration { get; set; }

        [Id(5)]
        public string ReasonForVisit { get; set; } = string.Empty;

        [Id(6)]
        public Guid CorrelationId { get; set; }

        [Id(7)]
        public string Status { get; set; } = string.Empty;

        [Id(8)]
        public Guid id { get; set; } // It's good you added this, but ensure consistency
    }
}
