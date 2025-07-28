using DistributedAppExamUnicam.GrainInterfaces;
using DistributedAppExamUnicam.Grains; // Needed for TimeSlot
using Microsoft.AspNetCore.Mvc;
using Orleans; // Needed for IGrainFactory
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DistributedAppExamUnicam.Controllers
{
 
    [ApiController]
    [Route("api/[controller]")] 
    public class DoctorController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;

        public DoctorController(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

       
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateDoctor([FromBody] CreateOrUpdateDoctorDTO dto)
        {
            var doctorId = dto.DoctorId.HasValue && dto.DoctorId.Value != Guid.Empty
                               ? dto.DoctorId.Value
                               : Guid.NewGuid();

            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(doctorId);

            // Set Name and Specialization
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                await doctorGrain.SetNameAsync(dto.Name);
            }
            if (!string.IsNullOrWhiteSpace(dto.Specialization))
            {
                await doctorGrain.SetSpecializationAsync(dto.Specialization);
            }

            var currentName = await doctorGrain.GetNameAsync();
            var currentSpecialization = await doctorGrain.GetSpecializationAsync();

            return Ok(new { DoctorId = doctorId, Name = currentName, Specialization = currentSpecialization });
        }

       
        /// Gets the name and specialization details of a specific doctor.
        [HttpGet("{doctorId}")]
        public async Task<ActionResult> GetDoctorDetails(Guid doctorId)
        {
            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(doctorId);
            var name = await doctorGrain.GetNameAsync();
            var specialization = await doctorGrain.GetSpecializationAsync();

            
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(specialization))
            {
                // While the grain reference exists, its conceptual "profile" might not be set up yet.
                return NotFound($"Doctor with ID {doctorId} found, but no name or specialization has been set.");
            }

            return Ok(new { DoctorId = doctorId, Name = name, Specialization = specialization });
        }

        
        /// Updates only the name of a specific doctor.
        [HttpPut("{doctorId}/name")] 
        public async Task<IActionResult> UpdateDoctorName(Guid doctorId, [FromBody] DoctorNameUpdateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Name cannot be empty.");
            }
            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(doctorId);
            await doctorGrain.SetNameAsync(dto.Name);
            return Ok();
        }

        
        /// Updates only the specialization of a specific doctor.
        [HttpPut("{doctorId}/specialization")]
        public async Task<IActionResult> UpdateDoctorSpecialization(Guid doctorId, [FromBody] DoctorSpecializationUpdateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Specialization))
            {
                return BadRequest("Specialization cannot be empty.");
            }
            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(doctorId);
            await doctorGrain.SetSpecializationAsync(dto.Specialization);
            return Ok();
        }

       
        /// Defines or updates the availability time slots for a specific doctor.
        [HttpPost("{doctorId}/availability")] 
        public async Task<IActionResult> DefineAvailability(Guid doctorId, [FromBody] List<TimeSlot> slot)
        {
            if (slot == null )
            {
                return BadRequest("No time slots provided.");
            }

            foreach (var s in slot)
            {
                if (s.Id == Guid.Empty)
                {
                    s.Id = Guid.NewGuid();
                }
                // Ensure slots are initially unreserved when defined
                s.IsReserved = false;
                s.PatientId = null;
            }

            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(doctorId);
            await doctorGrain.DefineAvailability(slot);
            return Ok(slot);
        }

       
        /// Retrieves the available time slots for a specific doctor within a given date range.
        [HttpGet("{doctorId}/available-slots")]
        public async Task<ActionResult<List<TimeSlot>>> GetAvailableSlots(
            Guid doctorId,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            if (from == default || to == default || from >= to)
            {
                return BadRequest("Invalid date range provided. 'from' and 'to' must be valid dates and 'from' must be before 'to'.");
            }

            var doctorGrain = _grainFactory.GetGrain<IDoctorGrain>(doctorId);
            var availableSlots = await doctorGrain.GetAvailableSlots(from, to);
            return Ok(availableSlots);
        }

        

        // [HttpPost("{doctorId}/cancel-slot")]
        // public async Task<IActionResult> CancelDoctorSlot(Guid doctorId, [FromBody] Guid slotId) { /* ... */ }
    }

    public class CreateOrUpdateDoctorDTO
    {
        public Guid? DoctorId { get; set; } 
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty; 
    }

   
    public class DoctorNameUpdateDTO
    {
        public string Name { get; set; } = string.Empty;
    }

    // A DTO for updating only the specialization
    public class DoctorSpecializationUpdateDTO
    {
        public string Specialization { get; set; } = string.Empty;
    }
}