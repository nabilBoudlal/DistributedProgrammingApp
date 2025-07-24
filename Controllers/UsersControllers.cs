using GrainInterfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System.Threading.Tasks;

namespace DistributedAppExamUnicam.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;

        public UserController(IGrainFactory grainFactory)
        {
            this._grainFactory = grainFactory;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(Guid userId)
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(userId);
            var name = await userGrain.GetNameAsync();
            return Ok(new { userId, name });

        }

        [HttpPost]
        public async Task<IActionResult> SetUser([FromBody] UserDTO dto)
        {
            var userId =  dto.UserId.HasValue 
                          && dto.UserId.Value 
                          != Guid.Empty
                            ? dto.UserId.Value
        :                       Guid.NewGuid();
            var userGrain = _grainFactory.GetGrain<IUserGrain>(userId);
            await userGrain.SetNameAsync(dto.Name);

            return Ok(new { userId = userId, name = dto.Name });
        }


    

        [HttpGet("{userId}/appointments")]
        public async Task<IActionResult> GetAppointments(Guid userId)
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(userId);
            var appointmentIds = await userGrain.GetAppointmentsAsync();

            var tasks = appointmentIds.Select(async id =>
            {
                var grain = _grainFactory.GetGrain<IAppointmentGrain>(id);
                var (title, date, description, uid) = await grain.GetDetails();
                return new
                {
                    Id = id,
                    Title = title,
                    Date = date,
                    Description = description,
                    UserId = uid
                };
            });

            var results = await Task.WhenAll(tasks);
            return Ok(results);
        }




    }

    public class UserDTO
    {
        public Guid? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

}