using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using WindowsInput;

namespace AutomaticInversionBot
{
    public class Bot
    {
        public IntPtr MainHandle { get; set; }
        public int CurrentFile { get; set; }
        public int Crashes { get; set; }
        public Thread TrimBot { get; set; }
        public Thread ManageBot { get; set; }
        public Thread WebBot { get; set; }
        public bool StopAtNext { get; set; }
        public bool DoTrim { get; set; }
        public Stopwatch Timer { get; set; }
        public List<long> TimeIntervals { get; set; }
        HttpListener listener;

        #region imports and winAPI stuff
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        void CloseWindow(IntPtr hwnd)
        {
            SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        private Process checkForProcess(string name)
        {
            Process[] p = Process.GetProcessesByName(name);
            if (p.Count() > 0)
            {
                return p.First();
            }
            else
            {
                return null;
            }
        }

        private const UInt32 WM_CLOSE = 0x0010;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int BASIC_INTERVAL = 150; //bot veryfies every 150ms for new action
        private const string SAVETOINV = "C:\\Diplomarbeit_Geoelektrik\\daten_inversed\\";
        private const string SAVETOTRIM = "C:\\Diplomarbeit_Geoelektrik\\daten_trimmed\\";
        private const string SAVETOTRIMINV = "C:\\Diplomarbeit_Geoelektrik\\daten_trimmed_inversed\\";
        private const string SAVETOXYZ = "C:\\Diplomarbeit_Geoelektrik\\daten_xyz\\";
        #endregion

        public Bot(bool trim)
        {
            ManageBot = new Thread(new ThreadStart(manageTrimmers));
            DoTrim = trim;
            CurrentFile = 0;
            Crashes = 0;
            StopAtNext = false;
            restart = false;
            Timer = Stopwatch.StartNew();
            TimeIntervals = new List<long>();
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:80/");
        }

        public void Start()
        {
            MyConsole.WriteLine("Starting Bot...");
            if (!ManageBot.IsAlive)
            {
                ManageBot = new Thread(new ThreadStart(manageTrimmers));
                ManageBot.Start();
            }
            if (WebBot == null)
            {
                WebBot = new Thread(new ThreadStart(httplisten));
                WebBot.Start();
            }
        }

        public void Stop(bool web)
        {
            TrimBot.Abort();
            ManageBot.Abort();
            if (web)
            {
                WebBot.Abort();
            }
        }

        private void manageTrimmers()
        {
            if (DoTrim)
            {
                TrimBot = new Thread(new ThreadStart(trimData));
            } else
            {
                TrimBot = new Thread(new ThreadStart(createxyz));
            }
            TrimBot.Start();
            while (Properties.Settings.Default.progress < 100)
            {
                Thread.Sleep(2000);
                if (!TrimBot.IsAlive)
                {
                    TrimBot.Abort();
                    if (DoTrim)
                    {
                        TrimBot = new Thread(new ThreadStart(trimData));
                    }
                    else
                    {
                        TrimBot = new Thread(new ThreadStart(createxyz));
                    }
                    CurrentFile = Properties.Settings.Default.progress;
                    TrimBot.Start();
                }
                /*if (!WebBot.IsAlive)
                {
                    WebBot.Abort();
                    WebBot = new Thread(new ThreadStart(httplisten));
                    WebBot.Start();
                }*/
            }
        }

        private void createxyz()
        {
            Timer.Reset();
            Timer.Start();
            string[] files = Directory.GetFiles("C:\\Diplomarbeit_Geoelektrik\\daten_trimmed\\");
            Process p = checkForProcess("RES2DINV_3.59.118");
            if (p == null)
            {
                p = Process.Start("C:\\Diplomarbeit_Geoelektrik\\daten von usb-stick\\RES2DINV_3.59.118.exe");
            }
            try
            {
                Thread.Sleep(500);
                IntPtr res2dHandle = p.MainWindowHandle;
                ShowWindow(res2dHandle, SW_MAXIMIZE); //show window maximized

                SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "System Resources", 5)); //set focus to start dialog and wait for it
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                //change settings: read inversion parameters from .inv
                InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                InputSimulator.SimulateTextEntry("C:\\Diplomarbeit_Geoelektrik\\daten von usb-stick\\Gresten Modelblock-fix.ivp");
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                for (int i = CurrentFile; i < files.Length; i++) //run through all files in given directory from startAtFile to endAtFile
                {
                    if (!Timer.IsRunning)
                    {
                        Timer.Reset();
                        Timer.Start();
                    }
                    TaskbarProgress.SetValue(MainHandle, CurrentFile, files.Length - 1); //progress on taskbar

                    if (StopAtNext)
                    {
                        StopAtNext = false;
                        ManageBot.Abort();
                        Stop(false);
                    }
                    MyConsole.WriteLine("#" + i + " started.");

                    //files to be saved
                    string invtrimfilename = SAVETOTRIMINV + files[i].Split('.')[0].Split('\\').Last() + ".inv"; //Split for filename and add .inv
                    string xyzfilename = SAVETOXYZ + files[i].Split('.')[0].Split('\\').Last() + ".xyz"; //Split for filename and add .xyz

                    //if already exist, delete
                    if (File.Exists(invtrimfilename))
                    {
                        MyConsole.WriteLine("#" + i + " (" + invtrimfilename.Split('\\').Last() + ") existed, so it was removed.");
                        File.Delete(invtrimfilename);
                    }
                    if (File.Exists(xyzfilename))
                    {
                        Console.WriteLine("#" + i + " (" + xyzfilename.Split('\\').Last() + ") existed, so it was removed.");
                        File.Delete(xyzfilename);
                    }

                    //read .dat file
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    SleepHelper.WaitForWindow(BASIC_INTERVAL, "Input 2D resistivity data file", 5);
                    InputSimulator.SimulateTextEntry(files[i]);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Message", 5));
                    if (SleepHelper.CheckForWindow(new string[] { "Data Error", "Convergence Warning" }))
                    {
                        MyConsole.WriteLine("#" + i + " skipped due to error.");
                        CurrentFile = i + 1;
                        Properties.Settings.Default.progress = i + 1;
                        Properties.Settings.Default.Save();
                        throw new NotIntendedException(i, "Data Error or RMS error too high.");
                    }
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    //least squares inversion
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "File Name for Inversion Results", 5));
                    Thread.Sleep(1500);
                    InputSimulator.SimulateTextEntry(invtrimfilename);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Enter Additional Iterations", 2000));
                    if (SleepHelper.CheckForWindow(new string[] { "Data Error", "Convergence Warning" }))
                    {
                        MyConsole.WriteLine("#" + i + " skipped due to error.");
                        CurrentFile = i + 1;
                        Properties.Settings.Default.progress = i + 1;
                        Properties.Settings.Default.Save();
                        throw new NotIntendedException(i, "Data Error or RMS error too high.");
                    }
                    InputSimulator.SimulateTextEntry("0");
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(150);
                    SleepHelper.WaitForCursor(BASIC_INTERVAL, Cursors.Arrow); //window has a slight delay
                    Thread.Sleep(150);

                    //check if everything went right
                    if (!File.Exists(invtrimfilename))
                    {
                        throw new NotIntendedException(i, "Least squares inversion: File not generated");
                    }

                    //display - show results
                    Thread.Sleep(150);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    //save xyz
                    Thread.Sleep(2000);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Choose iteration number", 5));
                    Thread.Sleep(150);
                    if(!SleepHelper.CheckForWindow(new string[] { "Choose iteration number" }))
                    {
                        throw new NotIntendedException(i, "Iteration number selection failed");
                    }

                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(150);
                    InputSimulator.SimulateTextEntry(xyzfilename);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Message", 5));
                    if (!SleepHelper.CheckForWindow(new string[] { "Message" }))
                    {
                        throw new NotIntendedException(i, "Save .xyz: No Message Window");
                    }

                    //close display window
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    Thread.Sleep(1000); //file needs some time until it is saved
                    //check if everything is right
                    if (!File.Exists(xyzfilename))
                    {
                        throw new NotIntendedException(i, "Save .xyz: File not generated");
                    }

                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT); //change output path? NO. (sometimes not needed)
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    MyConsole.WriteLine("#" + i + " finished.");
                    //update progress
                    CurrentFile = i + 1;
                    Properties.Settings.Default.progress = i + 1; //progress in settings
                    Properties.Settings.Default.Save();
                    Timer.Stop();
                    TimeIntervals.Add(Timer.ElapsedMilliseconds);

                    throw new RestartException(i);
                }
            }
            catch (RestartException rsexc)
            {
                p.Kill();
                p.Close();
                TrimBot.Abort();
            }
            catch (NotIntendedException niexc)
            {
                MyConsole.WriteLine(niexc);
                Crashes++;
                p.Kill();
                p.Close();
                TrimBot.Abort();
            } catch (ThreadAbortException taexc)
            {
                MyConsole.WriteLine("#" + Properties.Settings.Default.progress + ": Trimbot aborted.");
                Timer.Stop();
            }
            catch (Exception exc)
            {
                MyConsole.WriteLine("#" + Properties.Settings.Default.progress + ":\n" + exc);
                Crashes++;
                p.Kill();
                p.Close();
                TrimBot.Abort();
            }
        }

        private bool restart;
        private void trimData()
        {
            Timer.Reset();
            Timer.Start();
            string[] files = Directory.GetFiles("C:\\Diplomarbeit_Geoelektrik\\daten_mit_topography\\");
            Process p = checkForProcess("RES2DINV_3.59.118");
            if (p == null)
            {
                p = Process.Start("C:\\Diplomarbeit_Geoelektrik\\daten von usb-stick\\RES2DINV_3.59.118.exe");
            }
            try
            {
                Thread.Sleep(500);
                IntPtr res2dHandle = p.MainWindowHandle;
                ShowWindow(res2dHandle, SW_MAXIMIZE); //show window maximized

                SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "System Resources", 5)); //set focus to start dialog and wait for it
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                //change settings: read inversion parameters from .inv
                InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                InputSimulator.SimulateTextEntry("C:\\Diplomarbeit_Geoelektrik\\daten von usb-stick\\Gresten Modelblock-fix.ivp");
                InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                for (int i = CurrentFile; i < files.Length; i++) //run through all files in given directory from startAtFile to endAtFile
                {
                    if(!Timer.IsRunning)
                    {
                        Timer.Reset();
                        Timer.Start();
                    }
                    TaskbarProgress.SetValue(MainHandle, CurrentFile, files.Length - 1); //progress on taskbar
                    if (i % 6 == 0 && restart)
                    {
                        throw new RestartException(i);
                    }
                    else
                    {
                        restart = true;
                    }

                    if (StopAtNext)
                    {
                        StopAtNext = false;
                        ManageBot.Abort();
                        Stop(false);
                    }
                    MyConsole.WriteLine("#" + i + " started.");

                    //files to be saved
                    string invfilename = SAVETOINV + files[i].Split('.')[0].Split('\\').Last() + ".inv"; //Split for filename and add .inv
                    string datfilename = SAVETOTRIM + files[i].Split('\\').Last(); //Split for filename

                    //if already exist, delete
                    if (File.Exists(invfilename))
                    {
                        MyConsole.WriteLine("#" + i + " (" + invfilename.Split('\\').Last() + ") existed, so it was removed.");
                        File.Delete(invfilename);
                    }
                    if (File.Exists(datfilename))
                    {
                        Console.WriteLine("#" + i + " (" + datfilename.Split('\\').Last() + ") existed, so it was removed.");
                        File.Delete(datfilename);
                    }

                    //read .dat file
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    SleepHelper.WaitForWindow(BASIC_INTERVAL, "Input 2D resistivity data file", 5);
                    InputSimulator.SimulateTextEntry(files[i]);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Message", 5));
                    if (SleepHelper.CheckForWindow(new string[] { "Data Error", "Convergence Warning" }))
                    {
                        MyConsole.WriteLine("#" + i + " skipped due to error.");
                        CurrentFile = i + 1;
                        Properties.Settings.Default.progress = i + 1;
                        Properties.Settings.Default.Save();
                        throw new NotIntendedException(i, "Data Error or RMS error too high.");
                    }
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    /*SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Incomplete Gauss-Newton method", 5));
                    Thread.Sleep(150);
                    //check
                    if (!SleepHelper.GetActiveWindowTitle().Contains("Incomplete Gauss-Newton method"))
                    {
                        throw new NotIntendedException(i, "Read .dat file");
                    }*/
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    //least squares inversion
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(1000);
                    SleepHelper.WaitForWindow(BASIC_INTERVAL, "File Name for Inversion Results", 5);
                    InputSimulator.SimulateTextEntry(invfilename);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Enter Additional Iterations", 1400));
                    if (SleepHelper.CheckForWindow(new string[] { "Data Error", "Convergence Warning" }))
                    {
                        MyConsole.WriteLine("#" + i + " skipped due to error.");
                        CurrentFile = i + 1;
                        Properties.Settings.Default.progress = i + 1;
                        Properties.Settings.Default.Save();
                        throw new NotIntendedException(i, "Data Error or RMS error too high.");
                    }
                    InputSimulator.SimulateTextEntry("0");
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    Thread.Sleep(150);
                    SleepHelper.WaitForCursor(BASIC_INTERVAL, Cursors.Arrow); //window has a slight delay
                    Thread.Sleep(150);

                    //check if everything went right
                    if (!File.Exists(invfilename))
                    {
                        throw new NotIntendedException(i, "Least squares inversion: File not generated");
                    }

                    //display - show results
                    Thread.Sleep(150);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    //edit data - rms statistics
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Message", 5));
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    IntPtr test2 = SleepHelper.WaitForWindow(BASIC_INTERVAL, "RMS Error Analysis Window", 5);
                    SetForegroundWindow(test2);
                    Thread.Sleep(300);

                    //check if everything went right
                    if (!SleepHelper.GetActiveWindowTitle().Contains("RMS Error Analysis Window"))
                    {
                        throw new NotIntendedException(i, "Edit data - RMS statistics");
                    }

                    for (int redo = 0; redo < 26; redo++)
                    {
                        InputSimulator.SimulateKeyPress(VirtualKeyCode.LEFT);
                        Thread.Sleep(10);
                    }

                    //trim data
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN); //exit
                    Thread.Sleep(30);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Trim data set", 5));
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN); //trim data?
                    Thread.Sleep(30);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Message", 5));
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN); //to trim you need to save
                    Thread.Sleep(100);
                    ShowWindow(res2dHandle, SW_MINIMIZE); //minimize maximize, to work around a bug (otherwise filesave dialog does not appear)
                    Thread.Sleep(250);
                    ShowWindow(res2dHandle, SW_MAXIMIZE);
                    SetForegroundWindow(SleepHelper.WaitForWindow(BASIC_INTERVAL, "Output edited data file", 5));
                    InputSimulator.SimulateTextEntry(datfilename);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    Thread.Sleep(1000); //file needs some time until it is saved
                    //check if everything is right
                    if (!File.Exists(datfilename))
                    {
                        throw new NotIntendedException(i, "Trim data: File not generated");
                    }

                    //close display window
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT); //change output path? NO. (sometimes not needed)
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);

                    MyConsole.WriteLine("#" + i + " finished.");
                    //update progress
                    CurrentFile = i + 1;
                    Properties.Settings.Default.progress = i + 1; //progress in settings
                    Properties.Settings.Default.Save();
                    Timer.Stop();
                    TimeIntervals.Add(Timer.ElapsedMilliseconds);
                }
            }
            catch (RestartException rsexc)
            {
                restart = false;
                MyConsole.WriteLine(rsexc);
                p.Kill();
                p.Close();
                TrimBot.Abort();
            }
            catch (ThreadAbortException taexc)
            {
                restart = false;
                MyConsole.WriteLine("#" + Properties.Settings.Default.progress + ": Trimbot aborted.");
                Timer.Stop();
            }
            catch (NotIntendedException niexc)
            {
                restart = false;
                MyConsole.WriteLine(niexc);
                Crashes++;
                p.Kill();
                p.Close();
                TrimBot.Abort();
            }
            catch (Exception exc)
            {
                restart = false;
                MyConsole.WriteLine("#" + Properties.Settings.Default.progress + ":\n" + exc);
                Crashes++;
                p.Kill();
                p.Close();
                TrimBot.Abort();
            }
        }
        public void httplisten()
        {
            try
            {
                while (true)
                {
                    listener.Start();
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    StreamReader input = new StreamReader(request.InputStream, request.ContentEncoding);
                    string kvpair = input.ReadToEnd();
                    if (kvpair.Contains("="))
                    {
                        string action = kvpair.Split('=')[0];
                        if (action == "stop")
                        {
                            StopAtNext = true;
                            MyConsole.WriteLine("#" + CurrentFile + ": Stopping Bot after dataset.");
                        }
                        else if (action == "start")
                        {
                            StopAtNext = false;
                            if (!TrimBot.IsAlive)
                            {
                                MyConsole.WriteLine("#" + CurrentFile + ": Restarting Bot.");
                                Start();
                            }
                            else
                            {
                                MyConsole.WriteLine("#" + (CurrentFile + 1) + ": Restarting Bot at dataset.");
                            }
                        }
                        else if (action == "refresh")
                        {
                            int curLine = int.Parse(kvpair.Split('=')[1]);
                            HttpListenerResponse response = context.Response;
                            byte[] buffer = Encoding.UTF8.GetBytes("{ \"text\" : \"" + MyConsole.GetHTML(curLine) + "\", \"curLine\" : " + MyConsole.Lines.Count + ", \"curSet\" : " + CurrentFile + ", \"numcrash\" : " + Crashes + ", \"time\" : \"" + calcRestTime() + "\"}");
                            response.ContentLength64 = buffer.Length;
                            Stream output = response.OutputStream;
                            output.Write(buffer, 0, buffer.Length);
                            output.Close();
                            listener.Stop();
                        }
                    }
                    else
                    {
                        HttpListenerResponse response = context.Response;
                        XmlDocument html = new XmlDocument();
                        html.LoadXml(Properties.Resources.template);
                        XmlNode curLineSpan = html.SelectSingleNode("//span[@id='curLineSpan']");
                        XmlNode dataset = html.SelectSingleNode("//p[@id='dataset']");
                        XmlNode crashes = html.SelectSingleNode("//p[@id='crashes']");
                        XmlNode console = html.SelectSingleNode("//div[@id='console']");
                        XmlNode button = html.SelectSingleNode("//div[@id='button']");
                        XmlNode time = html.SelectSingleNode("//p[@id='time']");
                        XmlNode script = html.SelectSingleNode("//script[@type='text/javascript']");
                        curLineSpan.InnerText = MyConsole.Lines.Count.ToString();
                        dataset.InnerText = "Current dataset: #" + Properties.Settings.Default.progress;
                        crashes.InnerText = "Number of crashes: " + Crashes;
                        console.InnerXml = MyConsole.GetHTML(0);
                        time.InnerText = calcRestTime();
                        XmlAttribute btnAttr1 = html.CreateAttribute("class");
                        XmlAttribute btnAttr2 = html.CreateAttribute("onclick");
                        if (StopAtNext)
                        {
                            btnAttr1.Value = "start";
                            btnAttr2.Value = "start();";
                        } else
                        {
                            btnAttr1.Value = "stop";
                            btnAttr2.Value = "stop();";
                        }
                        button.Attributes.Append(btnAttr1);
                        button.Attributes.Append(btnAttr2);
                        script.InnerText = Properties.Resources.script;
                        byte[] buffer = Encoding.UTF8.GetBytes(html.OuterXml);
                        response.ContentLength64 = buffer.Length;
                        Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        output.Close();
                        listener.Stop();
                    }
                }
            }
            catch (Exception exc)
            {
                listener.Stop();
                WebBot.Abort();
            }
        }
        
        private string calcRestTime()
        {
            double dAvg;
            if (TimeIntervals.Count > 0)
            {
                dAvg = TimeIntervals.Average(x => x);
            }
            else
            {
                dAvg = 0;
            }
            long lAvg = Convert.ToInt64(dAvg);
            int crashAvg = (Crashes / (CurrentFile == 0 ? 1 : CurrentFile)) * 3386;
            TimeSpan ts = TimeSpan.FromMilliseconds(lAvg * (3386 - CurrentFile) + (lAvg / 4) * crashAvg);
            return string.Format("{0}d {1}h", ts.Days, ts.Hours);
        }
    }
}
