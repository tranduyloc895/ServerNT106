namespace API_Server.DTOs
{
    public class TaskDTO
    {
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool? IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartDateFormatted => StartDate.ToString("yyyy/MM/dd");
        public string EndDateFormatted => EndDate.ToString("yyyy/MM/dd");
    }
}

