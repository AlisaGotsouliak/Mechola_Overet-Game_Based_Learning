using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using template.Server.Helpers;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly FilesManage _filesManage;

        public MediaController(FilesManage filesManage)
        {
            _filesManage = filesManage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64)
        {
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadedFiles");
            return Ok(fileName);
        }

        [HttpPost("deleteImages")]
        public async Task<IActionResult> DeleteImages([FromBody] List<string> images)
        {
            var countFalseTry = 0;
            foreach (string img in images)
            {
                if (_filesManage.DeleteFile(img, "") == false)
                {
                    countFalseTry++;
                }
            }

            if (countFalseTry > 0)
            {
                return BadRequest("problem with " + countFalseTry.ToString() + " images");
            }
            return Ok("deleted");
        }
    }
}
