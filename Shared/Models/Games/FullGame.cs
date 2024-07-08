using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class FullGame
    {
        public string Game { get; set; }
        public int GameCode { get; set; }
        public bool CanPublish { get; set; }
        public int Time { get; set; }
        public int Num_Questions { get; set; }
        public List<QuestionDB> question_List { get; set; }

    }
}
