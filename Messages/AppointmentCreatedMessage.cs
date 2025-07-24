namespace DistributedAppExamUnicam.Messages
{
    public class AppointmentCreatedMessage
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? UserId { get; set; }
        public Guid? AppointmentId { get; set; }
    }
}
