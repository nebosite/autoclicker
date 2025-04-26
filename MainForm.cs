using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoClicker
{
    public partial class MainForm : Form
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, IntPtr Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("User32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, IntPtr Msg, IntPtr wParam, StringBuilder lParam);

        //[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        //public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData,  IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll", EntryPoint = "WindowFromPoint",  CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr WindowFromPoint(Point point);



        // Win32 functions that have all been used in previous blogs.
        [DllImport("User32.Dll")]
        private static extern void GetClassName(IntPtr hWnd, StringBuilder s, IntPtr nMaxCount);

        // The GetWindowRect function takes a handle to the window as the first parameter. The second parameter
        // must include a reference to a Rectangle object. This Rectangle object will then have it's values set
        // to the window rectangle properties.
        [DllImport("user32.dll")]
        public static extern long GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;
        const UInt32 BN_CLICKED = 245;
        List<RateRecord> _rateRecords = new List<RateRecord>();

        string[] numberGroups = [
            "",
            "thousand",
            "million",
            "billion",
            "trillion",
            "quadrillion",
            "quintillion",
            "sextillion",
            "septillion",
            "octillion",
            "nonillion",
            "decillion",
            "undecillion",
            "duodecillion",
            "tredecillion",
            "quattuordecillion",
            "quindecillion",
            "sexdecillion",
           ];


        class RateRecord
        {
            public DateTime timeStamp = DateTime.Now;
            public double count = 0;
            public RateRecord(double count)
            {
                this.count = count;
            }
        }

        public MainForm()
        {
            InitializeComponent();
            buttonTimer.Start();
        }

        private void buttonTimer_Tick(object sender, EventArgs e)
        {
            var count = GetCurrentCookieCount();
            if (count > 0)
            {
                // Remove old records
                for(int i = _rateRecords.Count - 1; i >= 0; i--)
                {
                    if ((DateTime.Now - _rateRecords[i].timeStamp).TotalSeconds > 30 )
                    {
                        _rateRecords.RemoveRange(i, 1);
                    }
                    else
                    {
                        break;
                    }
                }

                _rateRecords.Insert(0, new RateRecord(count));
            }

            if(_rateRecords.Count > 2)
            {
                var newest = _rateRecords[0];
                var oldest = _rateRecords[_rateRecords.Count - 1];
                var deltaCount = newest.count - oldest.count;

                // If we buy stuff the rate looks negative, 
                // so forget the records and start again.
                if(deltaCount < 0)
                {
                    _rateRecords.Clear();
                    return;
                }

                var deltaTime_s = (newest.timeStamp - oldest.timeStamp).TotalSeconds;
                var rate = deltaCount / deltaTime_s;
                var numberGroup = 0;
                while (rate > 1000 && numberGroup < numberGroups.Length - 1)
                {
                    rate /= 1000;
                    numberGroup++;
                }
                this.textBoxRate.Text = String.Format("{0:#,0.000}", rate) + " " + numberGroups[numberGroup];
            }



        }

        void ClickButton(string windowName, string buttonName)
        {
            var hwnd = FindWindowByCaption(IntPtr.Zero, windowName);
            if (hwnd == IntPtr.Zero) return;

            var hwndChild = FindWindowEx(hwnd, IntPtr.Zero, "Button", buttonName);
            if (hwndChild == IntPtr.Zero) return;

            SendMessage(hwndChild, new IntPtr(BN_CLICKED), IntPtr.Zero, IntPtr.Zero);
            AddSomeText("  Clicked " + buttonName + " on " + windowName);
        }

        void AddSomeText(string text)
        {
            string boxText = this.textBoxStatus.Text;
            if(boxText.Length > 10000)
            {
                boxText = boxText.Substring(0,4000);
            }
            this.textBoxStatus.Text = DateTime.Now.ToString("ddd HH:mm:ss") + ": " + text + "\r\n" + boxText;
        }
 
        public static IntPtr FindWindowByText(string partialTitle)
        {
            IntPtr foundHandle = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string windowTitle = sb.ToString();

                    if (windowTitle.ToString().Contains(partialTitle))
                    {
                        foundHandle = hWnd;
                        return false; // Stop enumeration
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);

            return foundHandle;
        }


        public static double GetCurrentCookieCount()
        {
            double output = 0;

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string windowTitle = sb.ToString();

                    if (windowTitle.ToString().Contains("cookies - Cookie"))
                    {
                        var parts = windowTitle.ToString().Split(' ');
                        try
                        {
                            output = double.Parse(parts[0]);

                        }
                        catch (Exception)
                        {
                            return true;
                        }

                        switch (parts[1].ToLower())
                        {
                            case "thousand": output *= 1E3; break;
                            case "million": output *= 1E6; break;
                            case "billion": output *= 1E9; break;
                            case "trillion": output *= 1E12; break;
                            case "quadrillion": output *= 1E15; break;
                            case "quintillion": output *= 1E18; break;
                            case "sextillion": output *= 1E21; break;
                            case "septillion": output *= 1E24; break;
                            case "octillion": output *= 1E27; break;
                            case "nonillion": output *= 1E30; break;
                            case "decillion": output *= 1E33; break;
                            case "undecillion": output *= 1E36; break;
                            case "duodecillion": output *= 1E39; break;
                            case "tredecillion": output *= 1E42; break;
                            case "quattuordecillion": output *= 1E45; break;
                            case "quindecillion": output *= 1E48; break;
                            case "sexdecillion": output *= 1E51; break;
                            default:
                                Console.WriteLine("Unknown number group: " + parts[0]);
                                break;
                        }
                        return true;
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);

            return output;
        }



        IntPtr _cookieHandle = IntPtr.Zero;
        IntPtr _cookieButton = IntPtr.Zero;


        private void buttonCookieClicker_Click(object sender, EventArgs e)
        {
            AddSomeText("Attempting to start clicking");
            Rectangle windowRect = new Rectangle();

            if (_cookieHandle == IntPtr.Zero)
            {
                _cookieHandle = FindWindowByText(" - Cookie");
                if (_cookieHandle != IntPtr.Zero)
                {
                    Debug.WriteLine("Found it: " + _cookieHandle);
                    AddSomeText( "Found a cookie clicker window!");
                    GetWindowRect(_cookieHandle, ref windowRect);
                    _cookieButton = WindowFromPoint(new Point(windowRect.X + 20, windowRect.Y + 20));
                } else
                {
                    Debug.WriteLine("Couldn't find a window");
                    AddSomeText( "Couldn't find a cookie clicker window.");
                    return;
                }
            }

            void SafeAddText(string text)
            {
                textBoxStatus.Invoke(new Action(() =>
                {
                    AddSomeText(text);
                }));
            }

            this.buttonCookieClicker.Enabled = false;
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var rand = new Random();
                GetWindowRect(_cookieHandle, ref windowRect);
                GetClientRect(_cookieHandle, out var clientRect);
                var clientHeight = clientRect.Height - 125;
                var middleAreaWidth = (clientRect.Width - 480) * .7;
                var cookieAreaWidth = (clientRect.Width - 300 - 30 - middleAreaWidth);
                uint centerX = (uint)(windowRect.X + cookieAreaWidth / 2 + 3);
                uint centerY = (uint)(windowRect.Y + 118 + clientHeight * .395);
                var pauseCount = 1;
                var nextSweep = DateTime.Now;


                Point point = new Point();

                var lastCount = GetCurrentCookieCount();

                string RunMe()
                {
                    for (int j = 0; j < 200000000; j++)
                    {
                        if (pauseCount > 0)
                        {
                            Thread.Sleep(3);
                            pauseCount--;
                            continue;
                        }
                        var theta = j * .19;// rand.NextDouble() * Math.PI * 2;
                        var radius = 128 * Math.Sin(j * .53);// rand.NextDouble() * 10;
                        var clickX = (int)(centerX + Math.Cos(theta) * radius);
                        var clickY = (int)(centerY + Math.Sin(theta) * radius);

                        // Once in a while try to click the golden cookie
                        if (DateTime.Now > nextSweep)
                        {
                            nextSweep = DateTime.Now.AddSeconds(30);
                            for (int x = 120; x < clientRect.Width - 340; x += 80)
                            {
                                for (int y = 270; y < clientRect.Height - 30; y += 80)
                                {
                                    clickX = x + windowRect.X;
                                    clickY = y + windowRect.Y;
                                    SetCursorPos(clickX, clickY);
                                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)clickX, (uint)clickY, 1, IntPtr.Zero);
                                    Thread.Sleep(2);

                                    GetCursorPos(ref point);

                                    if (Math.Abs(point.X - clickX) > 5 || Math.Abs(point.Y - clickY) > 5)
                                    {
                                        return "mouse moved";
                                    }

                                }
                            }
                        }
                        SetCursorPos(clickX, clickY);

                        // Click a bunch of times at the current click spot
                        for (int i = 0; i < 10; i++)
                        {
                            //SendMessage(_cookieButton, new IntPtr(BN_CLICKED), IntPtr.Zero, IntPtr.Zero);

                            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)clickX, (uint)clickY, 1, IntPtr.Zero);
                            Thread.Sleep(2);
                        }
                        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)clickX, (uint)clickY, 1, IntPtr.Zero);
                        GetCursorPos(ref point);


                        // bail out if the user moves the mouse deliverately
                        if (Math.Abs(point.X - clickX) > 5 || Math.Abs(point.Y - clickY) > 5)
                        {
                            return "mouse moved";
                        }

                        // Pause on mousemove to help prevent trouble
                        if (point.X != clickX || point.Y != clickY)
                        {
                            SafeAddText("Pausing because of mouse move");
                            pauseCount = 100;
                        }
                    }
                    return "finished";
                }

                var quitReason = RunMe();
                SafeAddText("Quit because: " + quitReason);

                this.buttonCookieClicker.Invoke(new Action(() => this.buttonCookieClicker.Enabled = true));


            }).Start();



        }

    }
}
