using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using template.Server.Helpers;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {

        private readonly FilesManage _filesManage;

        public ImagesController(FilesManage filesManage)
        {
            _filesManage = filesManage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64) //controller for "translating" the name from a long string into a name that will be in DB
        {
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadedFiles"); //function for saving the image in a folder, in this case in "uploadedFiles" and sending the string of chars with the type of file we want to save
            return Ok(fileName); //returning the name as saved in the folder
        }

        [HttpPost("deleteImage")]
        public async Task<IActionResult> DeleteImages([FromBody] string image)
        {
            var countFalseTry = 0;
            if (_filesManage.DeleteFile(image, "") == false)
            {
                countFalseTry++;
            }

            if (countFalseTry > 0)
            {
                //לעורך, אין גישה לשרת ולא יוכל לתקן דבר. הבדיקה משמשת אותנו לצורך בדיקות
                return BadRequest("problem with " + countFalseTry.ToString() + " image");
            }
            return Ok("deleted");
        }
    }
}

    
