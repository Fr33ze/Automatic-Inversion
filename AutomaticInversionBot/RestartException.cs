using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticInversionBot
{
    class RestartException : Exception
    {
        public int Current { get; set; }
        public RestartException(int current)
        {
            Current = current;
        }

        public override string ToString()
        {
            return "Restart of RES2DINV_3.59.118 at data set #" + Current + " to prevent a crash";
        }
    }
}
