using System;

namespace DistributedAppExamUnicam.Messages
{
    public class AppointmentDeletedMessage
    {
        public Guid Id { get; set; }
        public string? Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}