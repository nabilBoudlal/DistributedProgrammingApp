using Orleans;

namespace Event
{   
    [GenerateSerializer]
    public record AppointmentCreatedEvent(
        [property: Id(0)] Guid AppointmentId,
        [property: Id(1)] string Title,
        [property: Id(2)] DateTime Date
    );
}
