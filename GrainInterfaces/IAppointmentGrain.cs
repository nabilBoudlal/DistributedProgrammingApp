using Orleans;
using System.Threading.Tasks;

namespace GrainInterfaces
{

    public interface IAppointmentGrain : IGrainWithGuidKey
    {
        Task SetDetails(string title, DateTime date, string description, Guid userId);

        Task<(string Title, DateTime Date, string Description, Guid UserId)> GetDetails();
        Task ClearStateAsync();
    }
}