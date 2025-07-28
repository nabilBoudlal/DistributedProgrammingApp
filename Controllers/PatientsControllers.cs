using GrainInterfaces;
using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System.Threading.Tasks;

namespace DistributedAppExamUnicam.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;

        public PatientController(IGrainFactory grainFactory)
        {
            this._grainFactory = grainFactory;
        }

        [HttpGet("{patientId}")]
        public async Task<IActionResult> GetUser(Guid patientId)
        {
            var userGrain = _grainFactory.GetGrain<IPatientGrain>(patientId);
            var name = await userGrain.GetNameAsync();
            return Ok(new { patientId, name });
        }


        [HttpPost]
        public async Task<IActionResult> SetUser([FromBody] PatientDTO dto)
        {
            var userId =  dto.PatientId.HasValue 
                          && dto.PatientId.Value 
                          != Guid.Empty
                            ? dto.PatientId.Value
        :                       Guid.NewGuid();
            var userGrain = _grainFactory.GetGrain<IPatientGrain>(userId);
            await userGrain.SetNameAsync(dto.Name);

            return Ok(new { userId = userId, name = dto.Name });
        }


        [HttpGet("{patientId}/appointments")]
        public async Task<IActionResult> GetAppointments(Guid patientId)
        {
           return Ok(await _grainFactory.GetGrain<IPatientGrain>(patientId).GetAppointmentsAsync());
        }
    }

    public class PatientDTO
    {
        public Guid? PatientId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

}