namespace DistributedAppExamUnicam.Controllers
{
    public class AppointmentDTO
    {
        public string Title { get; set; } = "";
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public Guid UserId { get; set; }
        public Guid? AppointmentId { get; set; }
    }


}