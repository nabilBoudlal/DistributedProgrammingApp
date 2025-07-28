using DistributedAppExamUnicam.Grains;

namespace DistributedAppExamUnicam.GrainInterfaces
{
    public interface IDoctorGrain : IGrainWithGuidKey
    {
        Task<string> GetNameAsync();
        Task SetNameAsync(string name);
        Task<string> GetSpecializationAsync();
        Task SetSpecializationAsync(string specialization);

        Task DefineAvailability(List<TimeSlot> slots); 
        Task<List<TimeSlot>> GetAvailableSlots(DateTime from, DateTime to);
        Task<List<TimeSlot>> GetAllTimeSlots(); 

        Task<ReserveTimeSlotResult> TryReserveTimeSlot(Guid slotId, Guid appointmentId, Guid patientId);
        Task ReleaseTimeSlot(Guid slotId, Guid appointmentId);


    }

    public enum ReserveTimeSlotResult
    {
        Success,
        NotFound,
        AlreadyReserved,
        Overlap 
    }

}
