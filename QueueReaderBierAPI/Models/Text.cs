using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueReaderBierAPI.Models
{
    public class Text
    {
        public string text { get; set; }
        public float x { get; set; }
        public float y { get; set; }

        public Text(string text, float x, float y)
        {
            this.text = text;
            this.x = x;
            this.y = y;
        }
    }
}
