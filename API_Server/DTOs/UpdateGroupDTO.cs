namespace API_Server.DTOs
{
    public class UpdateGroupDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IFormFile ImgGroup { get; set; }
    }
}
