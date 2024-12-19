using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using template.Client.Pages;
using template.Server.Data;
using template.Server.Helpers;
using template.Shared.DTOs;
using template.Shared.Games;
using template.Shared.Models.Games;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(AuthCheck))]

    public class GamesController : ControllerBase
    {
        private readonly DbRepository _db;


        public GamesController(DbRepository db)
        {
            _db = db;
        }


        [HttpPost("addGame")]
        public async Task<IActionResult> AddGames(int authUserId, GameToAdd gameToAdd)
        {
            if (authUserId > 0)
            {
                object newGameParam = new
                {
                    GameName = gameToAdd.Game,
                    IsPublished = false,
                    TimePerItem = 50,
                    UserId = authUserId,
                    CanPublish = false
                };
                string insertGameQuery = "INSERT INTO Games (Game, IsPublished, Time, UserId, CanPublish) VALUES (@GameName, @IsPublished, @TimePerItem, @UserId, @CanPublish)";
                int newGameId = await _db.InsertReturnIdAsyncGames(insertGameQuery, newGameParam);

                if (newGameId != 0)
                {
                    object param = new
                    {
                        GameCode = newGameId
                    };
                    string gameQuery = "SELECT Games.Game, Games.GameCode, Games.IsPublished, Games.CanPublish, Games.Time, count(Questions.ID) AS Num_Questions FROM Games LEFT OUTER JOIN Questions on Games.GameCode = Questions.GameID WHERE Games.GameCode = @GameCode GROUP BY Games.GameCode";
                    var gameRecord = await _db.GetRecordsAsync<GameToCard>(gameQuery, param);
                    GameToCard newGame = gameRecord.FirstOrDefault();
                    return Ok(newGame);
                }
                return BadRequest("Game not created");
            }
            else
            {
                return Unauthorized("user is not authenticated");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetUserGames(int authUserId)
        {
            //בדיקה שיש משתמש מחובר
            if (authUserId > 0)
            {
                //יצירת פרמטר עם המזהה של המשתמש
                object param = new
                {
                    UserId = authUserId
                };

                //שליפת המשחקים של המשתמש
                string gameQuery = "SELECT Games.Game, Games.GameCode, Games.IsPublished, Games.CanPublish, Games.Time, Games.GameCode, count(Questions.ID) AS Num_Questions FROM Games LEFT OUTER JOIN Questions on Games.GameCode = Questions.GameID WHERE Games.UserId =1 GROUP BY Games.GameCode";
                var gamesRecords = await _db.GetRecordsAsync<GameToCard>(gameQuery, param);
                List<GameToCard> GamesList = gamesRecords.ToList();
                //במידה ויש משחקים - החזרתם
                if (GamesList.Count > 0)
                {
                    foreach (GameToCard ToCheck in GamesList)
                    {
                        bool Can = await CanPublishFunc(ToCheck.GameCode);
                        ToCheck.CanPublish = Can;
                    }

                    return Ok(GamesList);
                }
                else
                    return BadRequest("No games for this user");
            }
            else
                return Unauthorized("user is not authenticated");
        }


        //שיטה שבודקת אם ניתן לפרסם את המשחק
        //אם נמצא שלא ניתן לפרסם - נוודא שהמשחק גם לא מפורסם
        private async Task<bool> CanPublishFunc(int gameId)
        {
            //במקרה שלנו - התנאי לפרסום משחק הוא לפחות שלוש שאלות
            //יש לשנות את השיטה בהתאם לתנאי הפרסום עליהם החלטתם
            int minQuestions = 10;

            //משתנה לשמירה של הסטטוס - האם ניתן לפרסום
            bool canPublish = false;

            object param = new
            {
                ID = gameId
            };

            //שאילתה שבודקת כמה שאלות יש במשחק
            string queryQuestionCount = "SELECT Count(*) FROM Questions WHERE GameID=@ID";
            var recordQuestionCount = await _db.GetRecordsAsync<int>(queryQuestionCount, param);
            int numberOfQuestions = recordQuestionCount.FirstOrDefault();

            //נשמור משתנה ריק שיכיל את שאילתת העדכון בהתאם למספר השאלות
            string updateQuery;

            //אם יש מספיק שאלות במשחק
            if (numberOfQuestions >= minQuestions)
            {
                //נשנה את הסטטוס של האם ניתן לפרסום	
                canPublish = true;
                //נעדכן את השאילתה – אם המשחק מורשה לפרסום, לא נשנה את מצב הפרסום בפועל
                updateQuery = "UPDATE Games SET CanPublish=true WHERE GameCode=@ID";
            }
            //אם אין מספיק שאלות
            else
            {
                //נעדכן את השאילתה כך שגם האם ניתן לפרסם וגם האם מפורסם שליליים
                updateQuery = "UPDATE Games SET IsPublished=false, CanPublish=false WHERE GameCode=@ID";
            }

            //נעדכן את בסיס הנתונים
            int isUpdate = await _db.SaveDataAsync(updateQuery, param);
            //נחזיר משתנה בוליאני שאומר אם ניתן לפרסם את המשחק או לא
            return canPublish;
        }


        [HttpPost("publishGame")]
        public async Task<IActionResult> publishGame(int authUserId, PublishGame game)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    UserId = authUserId,
                    gameID = game.GameCode
                };

                //שליפת שם המשחק לפי משתמש כדי לוודא שהמשחק המבוקש שייך למשתמש שמחובר
                string checkQuery = "SELECT Game FROM Games WHERE UserId = @UserId and GameCode=@gameID";
                var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
                string gameName = checkRecords.FirstOrDefault();
                //שליפת שם המשחק כדי לוודא שהמשחק המבוקש שייך למשתמש המחובר
                if (gameName != null)
                {
                    //במידה ויש רצון לפרסם את המשחק
                    if (game.IsPublished == true)
                    {
                        //נבדוק באמצעות פונקציית עזר שניתן לפרסם אותו
                        bool canPublish = await CanPublishFunc(game.GameCode);
                        //במידה ולא ניתן לפרסם	
                        if (canPublish == false)
                        {
                            //נחזיר הודעת שגיאה	
                            return BadRequest("This game cannot be published");
                        }
                    }

                    //אם ניתן לפרסם את המשחק או שרוצים להסיר אותו מפרסום
                    //נעדכן את בסיס הנתונים
                    object paramUpdate = new
                    {
                        IsPublished = game.IsPublished,
                        gameID = game.GameCode
                    };
                    string updateQuery = "UPDATE Games SET IsPublished=@IsPublished WHERE GameCode=@gameID";
                    int isUpdate = await _db.SaveDataAsync(updateQuery, paramUpdate);

                    if (isUpdate == 1)
                    {
                        return Ok();
                    }
                    return BadRequest("Update Failed");
                }
                return BadRequest("You don't have any games");
            }
            else
                return Unauthorized("user is not authenticated");
        }


        [HttpPost("Delete")]
        public async Task<IActionResult> DeleteGame(int authUserId, GameCodeClass gameCode)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    UserId = authUserId,
                    GameCode = gameCode.GameCode
                };

                string queryAuthUser = "SELECT Game FROM Games WHERE UserId=@UserId AND GameCode=@GameCode";
                var recordUser = await _db.GetRecordsAsync<string>(queryAuthUser, param);
                string Game = recordUser.FirstOrDefault();

                if (Game != "")
                {
                    object paramQ = new
                    {
                        GameID = gameCode.GameCode
                    };
                    string queryGetQuestionID = "SELECT ID FROM Questions WHERE GameID=@GameID";
                    var record = await _db.GetRecordsAsync<int>(queryGetQuestionID, paramQ);
                    List<int> Qid = record.ToList();
                    bool AnsDeleted = true;

                    foreach (int id in Qid)
                    {
                        object paramID = new
                        {
                            Question = id
                        };
                        string queryID = "DELETE FROM Answers WHERE QuestionID=@Question";
                        int isDeletedQ = await _db.SaveDataAsync(queryID, paramID);
                        if (isDeletedQ == 0)
                            AnsDeleted = false;
                    }
                    if (AnsDeleted)
                    {

                        string queryDeleteQuestion = "DELETE FROM Questions WHERE GameID=@GameID";
                        int isDeleted = await _db.SaveDataAsync(queryDeleteQuestion, paramQ);
                        string queryDeleteGame = "DELETE FROM Games WHERE GameCode=@GameCode";
                        int isGameDeleted = await _db.SaveDataAsync(queryDeleteGame, param);
                        if (isGameDeleted == 1)
                            return Ok("Deleted succesfully");
                        else
                            return BadRequest("Game Not deleted");
                    }
                    return BadRequest("Answers not deleted");
                }
                else
                    return BadRequest("It's Not Your Game");
            }
            return Unauthorized("user is not authenticated");
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update(int authUserId, SettingsUpdate Updategame)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    GameCode = Updategame.GameCode,
                    Time = Updategame.Time,
                    Game = Updategame.Game
                };
                string query = "UPDATE Games SET Time=@Time, Game=@Game WHERE GameCode=@GameCode";
                int isUpdate = await _db.SaveDataAsync(query, param);
                if (isUpdate == 1)
                    return Ok("Updated");
                else
                    return BadRequest("Update failed");
            }
            else
                return Unauthorized("user is not authenticated");
        }

        [HttpPost("AddQuestion")]
        public async Task<IActionResult> AddQusetion(int authUserId, QuestionDB question)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    QuestionText = question.QuestionText,
                    QuestionImage = question.QuestionImage,
                    GameID = question.GameID
                };

                string query = "INSERT INTO Questions (QuestionText,QuestionImage,GameID) VALUES (@QuestionText,@QuestionImage,@GameID)";
                question.ID = await _db.InsertReturnIdAsyncQuestions(query, param);
                if (question.ID > 0)
                {

                    if (question.Answers != null)
                    {
                        List<int> addedId = new List<int>();
                        foreach (AnswerDB ans in question.Answers)
                        {
                            object param_ans = new
                            {
                                AnswerText = ans.AnswerText,
                                AnswerImage = ans.AnswerImage,
                                IsCorrect = ans.IsCorrect,
                                QuestionID = question.ID
                            };
                            string query_ans = "INSERT INTO Answers (AnswerText,AnswerImage,IsCorrect,QuestionID) VALUES (@AnswerText,@AnswerImage,@IsCorrect,@QuestionID)";

                            int newAnsID = await _db.InsertReturnIdAsyncGames(query_ans, param_ans);
                            if (newAnsID > 0)
                            {
                                addedId.Add(newAnsID);
                            }
                        }
                        int diff = question.Answers.Count - addedId.Count;
                        if (diff == 0)
                        {
                            return Ok("Added");
                        }
                        else
                        {
                            return BadRequest("Question not created");
                        }
                    }
                    else
                    {
                        string query_NewQList = "SELECT Questions.QuestionText, Questions.QuestionImage, Questions.ID, Questions.GameID FROM Questions WHERE Questions.GameID=@GameID";
                        var rec = await _db.GetRecordsAsync<QuestionDB>(query_NewQList, param);
                        List<QuestionDB> newQuestionList = rec.ToList();
                        return Ok(newQuestionList);
                    }

                }
                return BadRequest("Failed to add quesiton");
            }
            return Unauthorized("user is not authenticated");
        }

        [HttpPost("EditQuestion")]
        public async Task<IActionResult> EditQusetion(int authUserId, QuestionDB question)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    QuestionText = question.QuestionText,
                    QuestionImage = question.QuestionImage,
                    ID = question.ID
                };

                string query = "UPDATE Questions SET QuestionText=@QuestionText, QuestionImage=@QuestionImage WHERE ID=@ID";
                int isUpdate = await _db.SaveDataAsync(query, param);
                if (isUpdate == 1)
                {
                    List<int> UpdatedAnswers = new List<int>();
                    foreach (AnswerDB ans in question.Answers)
                    {
                        if (ans.ID > 0)
                        {
                            object param_ans = new
                            {
                                AnswerText = ans.AnswerText,
                                AnswerImage = ans.AnswerImage,
                                IsCorrect = ans.IsCorrect,
                                ID = ans.ID
                            };
                            string query_ans = "UPDATE Answers SET AnswerText=@AnswerText, AnswerImage=@AnswerImage, IsCorrect=@IsCorrect WHERE ID=@ID";

                            int isAnsUpdated = await _db.SaveDataAsync(query_ans, param_ans);
                            if (isAnsUpdated == 1)
                            {
                                UpdatedAnswers.Add(ans.ID);
                            }
                        }
                        else if (ans.ID == 0)
                        {
                            object param_ans = new
                            {
                                AnswerText = ans.AnswerText,
                                AnswerImage = ans.AnswerImage,
                                IsCorrect = ans.IsCorrect,
                                QuestionID = question.ID
                            };
                            string query_ans = "INSERT INTO Answers (AnswerText,AnswerImage,IsCorrect,QuestionID) VALUES (@AnswerText,@AnswerImage,@IsCorrect,@QuestionID)";
                            int newAnsID = await _db.InsertReturnIdAsyncGames(query_ans, param_ans);
                            if (newAnsID > 0)
                            {
                                UpdatedAnswers.Add(newAnsID);
                            }
                        }
                        else
                            return BadRequest("ans ID invalid");
                    }
                    int diff = question.Answers.Count - UpdatedAnswers.Count;
                    if (diff == 0)
                    {
                        object paramQid = new
                        {
                            QuestionID = question.ID
                        };
                        string AnswerQuery = "SELECT Answers.ID FROM Answers WHERE Answers.QuestionID = @QuestionID";
                        var recordeA = await _db.GetRecordsAsync<int>(AnswerQuery, paramQid);
                        List<int> oldAnsList = recordeA.ToList();
                        foreach (int old in oldAnsList)
                        {
                            bool toDelete = true;
                            foreach (int updated in UpdatedAnswers)
                            {
                                if (old == updated)
                                    toDelete = false;
                            }
                            if (toDelete == true)
                            {
                                object paramToDelete = new
                                {
                                    ID = old
                                };
                                string queryAnswerToDelete = "DELETE FROM Answers WHERE Answers.ID=@ID";
                                int isDeleted = await _db.SaveDataAsync(queryAnswerToDelete, paramToDelete);
                                if (isDeleted != 1)
                                    return BadRequest("Answer not deleted");
                            }
                        }
                        return Ok("Updated");
                    }
                    else
                        return BadRequest("Not all answers were updated");
                }
                return BadRequest("Question not updated");
            }
            return Unauthorized("user is not authenticated");
        }


        [HttpGet("GetFullGame/{gameCode}")]
        public async Task<IActionResult> GetFullGame(int gameCode, int authUserId)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    UserId = authUserId,
                    GameCode = gameCode
                };

                string query = "SELECT Games.Game, Games.GameCode, Games.CanPublish, Games.Time, count(Questions.ID) AS Num_Questions FROM Games LEFT OUTER JOIN Questions on Games.GameCode = Questions.GameID WHERE Games.GameCode = @GameCode AND UserId=@UserId GROUP BY Games.GameCode";

                var record = await _db.GetRecordsAsync<FullGame>(query, param);

                FullGame game = record.FirstOrDefault();
                if (game != null)
                {
                    string QuestionQuery = "SELECT Questions.QuestionText, Questions.QuestionImage, Questions.ID, Questions.GameID FROM Questions WHERE Questions.GameID=@GameCode";
                    var recordeQ = await _db.GetRecordsAsync<QuestionDB>(QuestionQuery, param);
                    game.question_List = recordeQ.ToList();

                    if (game.question_List.Count > 0)
                    {
                        foreach (QuestionDB q in game.question_List)
                        {
                            object paramQid = new
                            {
                                QuestionID = q.ID
                            };
                            string AnswerQuery = "SELECT Answers.IsCorrect, Answers.AnswerText, Answers.AnswerImage, Answers.QuestionID, Answers.ID FROM Answers WHERE Answers.QuestionID = @QuestionID";
                            var recordeA = await _db.GetRecordsAsync<AnswerDB>(AnswerQuery, paramQid);
                            q.Answers = recordeA.ToList();
                        }
                        return Ok(game);
                    }
                    else
                        return Ok(game);
                }
                else
                    return BadRequest("No such game");
            }
            else
                return Unauthorized("user is not authenticated");
        }

        [HttpPost("DeleteQuestion")]
        public async Task<IActionResult> DeleteQuestion(int authUserId, QuestionDB question)
        {
            if (authUserId > 0)
            {
                object paramQ = new
                {
                    QuestionID = question.ID
                };
                string queryCount = "SELECT count(*) FROM Answers WHERE Answers.QuestionID=@QuestionID";
                var record = await _db.GetRecordsAsync<int>(queryCount, paramQ);
                int CountAns = record.FirstOrDefault();
                if (CountAns > 0)
                {
                    string queryDeleteAnswers = "DELETE FROM Answers WHERE Answers.QuestionID=@QuestionID";
                    int isDeleted = await _db.SaveDataAsync(queryDeleteAnswers, paramQ);
                }
                string queryDeleteQuestion = "DELETE FROM Questions WHERE ID=@QuestionID";
                int isQDeleted = await _db.SaveDataAsync(queryDeleteQuestion, paramQ);
                if (isQDeleted == 1)
                {
                    await CanPublishFunc(question.GameID);
                    return Ok("Question deleted succesfully");
                }
                else
                    return BadRequest("Couldn't delete question");
            }
            else
                return Unauthorized("user is not authenticated");
        }
    }
}
