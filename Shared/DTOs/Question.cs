using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.DTOs
{
    public class Question
    {
        public string QuestionText { get; set; }
        public string QuestionImage { get; set; }
        public List<Answer> Answers { get; set; }
    }
}
