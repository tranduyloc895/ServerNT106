using API_Server.DTOs;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace API_Server.Services
{
    public class ImageService
    {
        private readonly IConfiguration _configuration;
        public ImageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<string> UploadImage(ImageDTO imageDTO)
        {
            if (imageDTO.file == null || imageDTO.file.Length == 0)
            {
                throw new ArgumentException("File ảnh không tồn tại.");
            }
            var clientId = _configuration["ImgurSettings:ClientId"];
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Client-ID", clientId);
                using (var content = new MultipartFormDataContent())
                {
                    var memorystream = new MemoryStream();
                    await imageDTO.file.CopyToAsync(memorystream);
                    memorystream.Seek(0, SeekOrigin.Begin);
                    var imagecontent = new StreamContent(memorystream);
                    imagecontent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageDTO.file.ContentType);
                    content.Add(imagecontent, "image", imageDTO.file.FileName);
                    var response = await client.PostAsync("https://api.imgur.com/3/upload", content);
                    response.EnsureSuccessStatusCode();
                    var responseData = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(responseData);
                    if (json["success"]?.Value<bool>() == true)
                    {
                        return json["data"]["link"].ToString();
                    }
                    else
                    {
                        throw new Exception("Imgur upload failed: " + json["data"]["error"].ToString());
                    }
                }    
            }
        }
    }
}
