using GrainInterfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;


namespace DistributedAppExamUnicam.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class AppointmentsController : ControllerBase
	{
		private readonly IGrainFactory _grainFactory;

		public AppointmentsController(IGrainFactory grainFactory)
		{
			_grainFactory = grainFactory;
		}

		[HttpPost("{id}")]
		public async Task<IActionResult> CreateOrUpdate(Guid id, [FromBody] AppointmentDTO dto)
		{
			var appointment = _grainFactory.GetGrain<IAppointmentGrain>(id);
			await appointment.SetDetails(dto.Title, dto.Date, dto.Description);
			return Ok();
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> Get(Guid id)
		{
			var appointment = _grainFactory.GetGrain<IAppointmentGrain>(id);
			var (title, date, description) = await appointment.GetDetails();
			return Ok(new { id, title, date, description });
		}
	}


}