using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using template.Server.Data;
using template.Shared.DTOs;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnityController : ControllerBase
    {
        private readonly DbRepository _db;

        public UnityController(DbRepository db)
        {
            _db = db;
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> CheckValidCode(int code)
        {
            object param = new
            {
                GameCode = code
            };
            string query = "SELECT IsPublished FROM Games WHERE GameCode=@GameCode";
            var record = await _db.GetRecordsAsync<Published>(query, param);
            Published published = record.FirstOrDefault();

            if (published != null)
            {
                if (published.IsPublished)
                {
                    object paramInfo = new
                    {
                        GameCode = code
                    };
                    string queryInfo = "SELECT Game, Time FROM Games WHERE GameCode=@GameCode";
                    var Info = await _db.GetRecordsAsync<GeneralGameInfo>(queryInfo, paramInfo);
                    GeneralGameInfo GameInfoToReturn = Info.FirstOrDefault();

                    if (GameInfoToReturn.Game != null)
                    {
                        object paramQ = new
                        {
                            GameID = code
                        };
                        string queryQ = "SELECT QuestionImage, QuestionText FROM Questions WHERE GameID=@GameID";
                        var recordQ = await _db.GetRecordsAsync<Question>(queryQ, paramQ);
                        GameInfoToReturn.questions = recordQ.ToList();

                        string queryID = "SELECT ID FROM Questions WHERE GameID=@GameID";
                        var recordID = await _db.GetRecordsAsync<QuestionID>(queryID, paramQ);
                        List<QuestionID> qID = recordID.ToList();

                        if (qID.Count != 0)
                        {
                            for (int i = 0; i < qID.Count; i++)
                            {
                                object paramA = new
                                {
                                    QuestionID = qID[i].ID
                                };
                                string queryA = "SELECT AnswerImage,AnswerText, IsCorrect FROM Answers WHERE QuestionID=@QuestionID";
                                var recordA = await _db.GetRecordsAsync<Answer>(queryA, paramA);
                                GameInfoToReturn.questions[i].Answers = recordA.ToList();
                            }
                            return Ok(GameInfoToReturn);
                        }
                        else
                            return BadRequest("There are not questions in the game");
                    }
                    else
                        return BadRequest("No game info");
                }
                else
                    return BadRequest("The game is not published");
            }
            else
                return BadRequest("The game code does not exist");
        }
    }
}
