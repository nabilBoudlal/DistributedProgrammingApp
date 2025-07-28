using DistributedAppExamUnicam.GrainInterfaces;
using Grains;

namespace DistributedAppExamUnicam.Grains
{
    public class DoctorGrain : Grain, IDoctorGrain
    {

        private readonly IPersistentState<DoctorState> _state;

        public DoctorGrain([PersistentState("state", "redisStorage")] IPersistentState<DoctorState> state)
        {
            _state = state;
        }

        public Task<string> GetNameAsync()
        {
            return Task.FromResult(_state.State.name);
        }

        public Task SetNameAsync(string name)
        {
            _state.State.name = name;
            return _state.WriteStateAsync();
        }
        public Task<string> GetSpecializationAsync()
        {
            return Task.FromResult(_state.State.specialization);
        }

        public Task SetSpecializationAsync(string specialization)
        {
            _state.State.specialization = specialization;
            return _state.WriteStateAsync();
        }


        public async Task DefineAvailability(List<TimeSlot> slots)
        {
            foreach (var newSlot in slots)
            {
                if (newSlot.Id == Guid.Empty)
                {
                    newSlot.Id = Guid.NewGuid();
                }
                newSlot.IsReserved = false; // Always set to false when defining/redefining availability
                newSlot.PatientId = null;
                newSlot.AppointmentId = null;

                var existingSlot = _state.State.Availability.FirstOrDefault(s => s.Id == newSlot.Id);
                if (existingSlot != null)
                {
                    // Update existing slot 
                    existingSlot.Start = newSlot.Start;
                    existingSlot.Duration = newSlot.Duration;
                }
                else
                {
                    // Add new slot
                    _state.State.Availability.Add(newSlot);
                }
            }


            await _state.WriteStateAsync();
        }

        public Task<List<TimeSlot>> GetAvailableSlots(DateTime from, DateTime to)
        {
            var availableSlots = _state.State.Availability
                .Where(slot => slot.Start >= from && slot.Start < to && !slot.IsReserved)
                .ToList();
            return Task.FromResult(availableSlots);

        }

        public async Task<ReserveTimeSlotResult> TryReserveTimeSlot(Guid slotId, Guid appointmentId, Guid patientId)
        {
            var slot = _state.State.Availability
                .FirstOrDefault(s => s.Id == slotId);

            if (slot == null)
            {
                return ReserveTimeSlotResult.NotFound;
            }

            if (slot.IsReserved)
            {
                // Important for idempotency: if already reserved by *this specific appointment*, it's a success.
                // This prevents issues if the saga retries the reservation command.
                if (slot.AppointmentId == appointmentId && slot.PatientId == patientId)
                {
                    return ReserveTimeSlotResult.Success;
                }
                return ReserveTimeSlotResult.AlreadyReserved;
            }

            // Check for potential overlaps if you have a complex scheduling system
            // This example doesn't include overlap logic, but it's where you'd put it.
            // For now, it just checks the specific slotId.

            slot.IsReserved = true;
            slot.PatientId = patientId;
            slot.AppointmentId = appointmentId; // Store the appointment ID that reserved this slot
            await _state.WriteStateAsync();
            return ReserveTimeSlotResult.Success;
        }

        // --- New ReleaseTimeSlot method for compensation ---
        public async Task ReleaseTimeSlot(Guid slotId, Guid appointmentId)
        {
            var slot = _state.State.Availability
                .FirstOrDefault(s => s.Id == slotId);

            // Only release if the slot exists, is reserved, and was reserved by *this* specific appointment.
            // This prevents other legitimate bookings from being accidentally released.
            if (slot != null && slot.IsReserved && slot.AppointmentId == appointmentId)
            {
                slot.IsReserved = false;
                slot.PatientId = null;
                slot.AppointmentId = null;
                await _state.WriteStateAsync();
            }
            // If the slot doesn't exist, isn't reserved, or was reserved by another appointment,
            // we simply do nothing. This makes the operation idempotent and safe for retries.
        }

        public Task<List<TimeSlot>> GetAllTimeSlots()
        {
            return Task.FromResult(_state.State.Availability);
        }
    }
    public class DoctorState
    {
        public string name { get; set; } = string.Empty;
        public string specialization { get; set; } = string.Empty;
        public List<TimeSlot> Availability { get; set; } = new List<TimeSlot>();

    }

    [GenerateSerializer] 
    public class TimeSlot
    {
        [Id(0)] 
        public Guid Id { get; set; }

        [Id(1)] 
        public DateTime Start { get; set; }

        [Id(2)] 
        public TimeSpan Duration { get; set; }

        [Id(3)] 
        public bool IsReserved { get; set; }

        [Id(4)] 
        public Guid? PatientId { get; set; }

        [Orleans.Id(5)] 
        public Guid? AppointmentId { get; set; }
    }
}
