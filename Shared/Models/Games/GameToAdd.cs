using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Games
{
    public class GameToAdd
    {
        public string Game { get; set; }
        public int Time { get; set; }
    }
}
