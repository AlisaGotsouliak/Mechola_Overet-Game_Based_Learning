using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.DTOs
{
    public class GeneralGameInfo
    {
        public string Game { get; set; }

        public int Time { get; set; }

        public List<Question> questions { get; set; }
    }
}
