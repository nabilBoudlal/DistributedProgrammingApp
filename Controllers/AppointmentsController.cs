using DistributedAppExamUnicam.GrainInterfaces; 
using DistributedAppExamUnicam.Messages; 
using Grains; 
using MassTransit; 
using Microsoft.AspNetCore.Mvc;
using Orleans; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DistributedAppExamUnicam.Controllers
{
    public class BookingAppointmentRequestDTO
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public Guid SlotId { get; set; } 
        public DateTime AppointmentTime { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid CorrelationId { get; set; }
        public TimeSpan Duration { get; set; }
        public string ReasonForVisit { get; set; } = string.Empty;
    }

    public class ConfirmAppointmentRequest
    {
        public Guid CorrelationId { get; set; } 
        public Guid AppointmentId { get; set; }
    }

    public class CancelAppointmentRequest
    {
        public Guid CorrelationId { get; set; }
        public Guid AppointmentId { get; set; }
    }

    public class DeleteAppointmentRequest
    {
        public Guid CorrelationId { get; set; }
        public Guid AppointmentId { get; set; }
    }

    public class CompleteAppointmentRequest
    {
        public Guid CorrelationId { get; set; }
        public Guid AppointmentId { get; set; }
    }

  
    [ApiController]
    [Route("api/[controller]")] 
    public class AppointmentsController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IPublishEndpoint _publishEndpoint;

        public AppointmentsController(IGrainFactory grainFactory, IPublishEndpoint publishEndpoint)
        {
            _grainFactory = grainFactory;
            _publishEndpoint = publishEndpoint;
        }

        
        /// Avvia il processo di prenotazione per un nuovo appuntamento.
        [HttpPost("book")]
        public async Task<IActionResult> BookAppointment([FromBody] BookingAppointmentRequestDTO request)
        {

            Guid correlationId;
            Guid appointmentId;

            if (request.PatientId == Guid.Empty || request.DoctorId == Guid.Empty || request.SlotId == Guid.Empty)
            {
                return BadRequest("PatientId, DoctorId, and SlotId must be provided.");
            }
            if (request.AppointmentTime == default || request.Duration == default || request.Duration <= TimeSpan.Zero)
            {
                return BadRequest("Valid AppointmentTime and Duration must be provided.");
            }

            if (request.AppointmentId == Guid.Empty)
            {
                appointmentId = Guid.NewGuid(); // ID per la saga (il processo di booking)
            }
            else
            {
                appointmentId = request.AppointmentId;
            }
            
            if (request.CorrelationId == Guid.Empty)
            {
                correlationId = Guid.NewGuid(); // ID unico per l'istanza specifica dell'appuntamento (il Grain)
            }
            else
            {
                correlationId = request.CorrelationId;
            }

            
            

            await _publishEndpoint.Publish<IBookAppointmentCommand>(new BookAppointmentCommand 
            {
                CorrelationId = correlationId,
                AppointmentId = appointmentId,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                SlotId = request.SlotId,
                AppointmentTime = request.AppointmentTime,
                Duration = request.Duration,
                ReasonForVisit = request.ReasonForVisit
            });

            return Accepted(new { AppointmentId = appointmentId, BookingCorrelationId = correlationId, Message = "Appointment booking initiated." });
        }

       
        /// Recupera i dettagli di uno specifico appuntamento.
        [HttpGet("{appointmentId}")]
        public async Task<IActionResult> GetAppointmentDetails(Guid appointmentId)
        {
            var appointmentGrain = _grainFactory.GetGrain<IAppointmentGrain>(appointmentId);
            var state = await appointmentGrain.GetAppointmentState();

            Console.Out.WriteLine(state.ToString());

            if (state == null || state.PatientId == Guid.Empty) 
            {
                return NotFound($"Appointment with ID {appointmentId} not found or not initialized.");
            }

            return Ok(new AppointmentDetailsDTO
            {
                id = appointmentId,
                PatientId = state.PatientId,
                DoctorId = state.DoctorId,
                SlotId = state.SlotId,
                AppointmentTime = state.AppointmentTime,
                Duration = state.Duration,
                ReasonForVisit = state.ReasonForVisit,
                Status = state.Status.ToString(), // Conversione a stringa
                CorrelationId = (Guid) state.CorrelationId
            });
        }

       
        /// Invia un comando per confermare un appuntamento.
        [HttpPost("{appointmentId}/confirm")]
        public async Task<IActionResult> ConfirmAppointment(Guid appointmentId, [FromBody] ConfirmAppointmentRequest request)
        {
            // Verificare che request.CorrelationId non sia Guid.Empty
            if (request.CorrelationId == Guid.Empty)
            {
                return BadRequest("CorrelationId is required for confirmation.");
            }

            // Pubblica il comando con il CorrelationId corretto
            await _publishEndpoint.Publish<IConfirmAppointmentCommand>(new ConfirmAppointmentCommand
            {
                AppointmentId = appointmentId,
                CorrelationId = request.CorrelationId 
            });

            return Accepted();
        }


        /// Invia un comando per annullare un appuntamento.
        [HttpPost("{appointmentId}/cancel")]
        public async Task<IActionResult> CancelAppointment(Guid appointmentId, [FromBody] CancelAppointmentRequest request)
        {
            // Verificare che request.CorrelationId non sia Guid.Empty
            if (request.CorrelationId == Guid.Empty)
            {
                return BadRequest("CorrelationId is required for confirmation.");
            }
            await _publishEndpoint.Publish<ICancelAppointmentCommand>(new CancelAppointmentCommand {
                AppointmentId = appointmentId, 
                CorrelationId = request.CorrelationId
            });
            return Accepted($"Cancellation command for appointment {appointmentId} sent.");
        }

       
        /// Invia un comando per marcare un appuntamento come completato.
        [HttpPost("{appointmentId}/complete")]
        public async Task<IActionResult> CompleteAppointment(Guid appointmentId, [FromBody] CompleteAppointmentRequest request)
        {
            // Verificare che request.CorrelationId non sia Guid.Empty
            if (request.CorrelationId == Guid.Empty)
            {
                return BadRequest("CorrelationId is required for confirmation.");
            }

            await _publishEndpoint.Publish<IMarkAppointmentCompletedCommand>(new MarkAppointmentCompletedCommand 
            { 
                AppointmentId = appointmentId ,
                CorrelationId = request.CorrelationId
            });
            return Accepted($"Completion command for appointment {appointmentId} sent.");
        }



        /// Invia un comando per avviare la cancellazione/eliminazione di un appuntamento.
        [HttpDelete("{appointmentId}")]
        public async Task<IActionResult> DeleteAppointment(Guid appointmentId, [FromBody] DeleteAppointmentRequest request)
        {
            // Verificare che request.CorrelationId non sia Guid.Empty
            if (request.CorrelationId == Guid.Empty)
            {
                return BadRequest("CorrelationId is required for confirmation.");
            }

            await _publishEndpoint.Publish<IDeleteAppointmentCommand>(new DeleteAppointmentCommand { 
                AppointmentId = appointmentId,
                CorrelationId = request.CorrelationId
            });
            return Accepted(new { message = $"Deletion command for appointment {appointmentId} sent." });
        }
    }
}