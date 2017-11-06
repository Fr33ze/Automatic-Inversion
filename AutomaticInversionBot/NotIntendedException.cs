using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticInversionBot
{
    class NotIntendedException : Exception
    {
        public int Current { get; set; }
        public string Action { get; set; }
        public NotIntendedException(int current, string action)
        {
            Current = current;
            Action = action;
        }

        public override string ToString()
        {
            return "NotIntendedException at dataset #" + Current + " while performing action: [" + Action + "]";
        }
    }
}
