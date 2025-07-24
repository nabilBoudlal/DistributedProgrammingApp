using DistributedAppExamUnicam.GrainInterfaces;
using DistributedAppExamUnicam.Messages;
using Event;
using GrainInterfaces;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System.Reflection;


namespace DistributedAppExamUnicam.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class AppointmentsController : ControllerBase
	{
		private readonly IGrainFactory _grainFactory;
		private readonly IPublishEndpoint _publishEndpoint;

		public AppointmentsController(IGrainFactory grainFactory, IPublishEndpoint publishEndpoint)
		{
			_grainFactory = grainFactory;
			_publishEndpoint = publishEndpoint;
		}

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AppointmentDTO dto)
        {
            var appointment = _grainFactory.GetGrain<IAppointmentGrain>(id);
            await appointment.SetDetails(dto.Title, dto.Date, dto.Description, dto.UserId); 

            var notificationGrain = _grainFactory.
                GetGrain<INotificationGrain>(Guid.Parse("21a57cef-59b0-47f4-9708-9fdfa37d47e2"));
            await notificationGrain.ReceiveEvent(new Event.AppointmentCreatedEvent(id, dto.Title, dto.Date));

            await _publishEndpoint.Publish(new AppointmentCreatedMessage
            {
                Id = id,
                Title = dto.Title,
                Date = dto.Date,
                UserId = dto.UserId.ToString()
            });
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AppointmentDTO dto)
        {
            var id = dto.AppointmentId.HasValue 
                && dto.AppointmentId.Value 
                 != Guid.Empty
                  ? dto.AppointmentId.Value
                     : Guid.NewGuid();
            var appointment = _grainFactory.GetGrain<IAppointmentGrain>(id);
            await appointment.SetDetails(dto.Title, dto.Date, dto.Description, dto.UserId);

            // Aggiorna anche il UserGrain
            var userGrain = _grainFactory.GetGrain<IUserGrain>(dto.UserId);
            await userGrain.AddAppointmentAsync(id);

            // Invia evento di notifica
            var notificationGrain = _grainFactory.GetGrain<INotificationGrain>(
                Guid.Parse("21a57cef-59b0-47f4-9708-9fdfa37d47e2"));
            await notificationGrain.ReceiveEvent(new AppointmentCreatedEvent(id, dto.Title, dto.Date));

            // Invia anche su bus MassTransit
            await _publishEndpoint.Publish(new AppointmentCreatedMessage
            {
                Id = id,
                AppointmentId = id,
                Title = dto.Title,
                Date = dto.Date,
                UserId = dto.UserId.ToString()
            });

            return Ok(new { appointmentId = id });
        }




        [HttpGet("{id}")]
		public async Task<IActionResult> Get(Guid id)
		{
			var appointment = _grainFactory.GetGrain<IAppointmentGrain>(id);
			var (title, date, description, userId) = await appointment.GetDetails();
			return Ok(new { id, title, date, description, userId});
		}

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var appointment = _grainFactory.GetGrain<IAppointmentGrain>(id);

            var (title, date, description, userId) = await appointment.GetDetails();

            await appointment.ClearStateAsync();

            // Rimuovi l'appuntamento dalla lista dell'utente
            var userGrain = _grainFactory.GetGrain<IUserGrain>(userId);
            await userGrain.RemoveAppointmentAsync(id);

            await _publishEndpoint.Publish(new AppointmentDeletedMessage
            {
                Id = id,
                Title = title,
                Date = DateTime.UtcNow
            });

            return Ok(new { message = $"Appointment {id} deleted" });
        }

    }


}