using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    public class ExTap : Tap
    {
        [Newtonsoft.Json.JsonProperty]
        public ExTapDirection direction = ExTapDirection.None;
    }

    public enum ExTapDirection
    {
        None,
        Down,
        Center
    }
}
