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
        public async Task<IActionResult> GetUser(string userId)
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(userId);
            var name = await userGrain.GetNameAsync();
            return Ok(new { userId, name });

        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> SetUser(string userId, [FromBody] string name)
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(userId);
            await userGrain.SetNameAsync(name);
            return Ok();
        }
    }
}