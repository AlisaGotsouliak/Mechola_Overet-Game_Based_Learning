using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using template.Shared.DTOs;

namespace template.Shared.Models.Games
{
    public class QuestionDB
    {
        public string QuestionText { get; set; }
        public string QuestionImage { get; set; }
        public int GameID { get; set; }
        public int ID { get; set; }
        public List<AnswerDB> Answers { get; set; }

    }
}
