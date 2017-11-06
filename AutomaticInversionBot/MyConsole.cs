using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomaticInversionBot
{
    public class MyConsole
    {
        public static List<string> Lines { get; set; }

        private static StringBuilder combined;

        public static void WriteLine(object line)
        {
            if (Lines == null)
            {
                Lines = new List<string>();
                combined = new StringBuilder();
            }
            Lines.Add(line.ToString());
            combined.Append("<p class=\'entry\'><span style=\'color: black;\'>" + (Lines.Count - 1) + "</span> " + line + "</p>");
        }
        public static string GetHTML(int fromLine)
        {
            combined.Clear();
            for (int i = fromLine; i < Lines.Count; i++)
            {
                combined.Append("<p class=\'entry\'><span style=\'color: black;\'>" + i + "</span> " + Lines[i] + "</p>");
            }
            return combined.ToString();
        }
    }
}