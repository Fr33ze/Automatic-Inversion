using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;

namespace AutomaticInversionBot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            startIdx.Value = Properties.Settings.Default.progress;
        }

        Bot bot;
        private void part1btn_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Properties.Settings.Default.progress = (int) startIdx.Value;
            Properties.Settings.Default.Save();
            if (bot == null)
            {
                bot = new Bot(true);
                bot.CurrentFile = (int)startIdx.Value;
                bot.MainHandle = this.Handle;
            }
            bot.Start();
        }

        private void part2btn_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Properties.Settings.Default.progress = (int)startIdx.Value;
            Properties.Settings.Default.Save();
            if (bot == null)
            {
                bot = new Bot(false);
                bot.CurrentFile = (int)startIdx.Value;
                bot.MainHandle = this.Handle;
            }
            bot.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (bot != null)
            {
                bot.Stop(true);
            }
        }
    }
}
