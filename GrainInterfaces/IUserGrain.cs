using Orleans;
using System.Threading.Tasks;

namespace GrainInterfaces
{
    public interface IUserGrain : IGrainWithGuidKey
    {
        Task<string> GetNameAsync();
        Task SetNameAsync(string name);

        Task AddAppointmentAsync(Guid appointmentId);

        Task<List<Guid>> GetAppointmentsAsync();

        Task RemoveAppointmentAsync(Guid appointmentId);


    }
}