using Event;

namespace DistributedAppExamUnicam.GrainInterfaces
{
    public interface INotificationGrain : IGrainWithGuidKey
    {
        Task ReceiveEvent(AppointmentCreatedEvent evt);
        Task<List<string>> GetNotifications();
    }
}
