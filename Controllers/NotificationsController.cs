using DistributedAppExamUnicam.GrainInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace DistributedAppExamUnicam.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;

        public NotificationsController(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var grain = _grainFactory.GetGrain<INotificationGrain>(id);
            var notifications = await grain.GetNotifications();
            return Ok(notifications);
        }


    }
}
