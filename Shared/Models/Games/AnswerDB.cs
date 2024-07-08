using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class AnswerDB
    {
        public string AnswerText { get; set; }
        public string AnswerImage { get; set; }
        public bool IsCorrect { get; set; }
        public int QuestionID { get; set; }
        public int ID { get; set; }
    }
}
