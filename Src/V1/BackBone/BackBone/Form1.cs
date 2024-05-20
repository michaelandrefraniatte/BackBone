using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;
using PromptHandle;
using System.Collections.Generic;
using System.Diagnostics;

namespace BackBone
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll")]
        static extern bool DrawMenuBar(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        [System.Runtime.InteropServices.DllImport("User32", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out Rectangle lpRect);
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        public static int x, y;
        public WebView2 webView21 = new WebView2();
        private static int width = Screen.PrimaryScreen.Bounds.Width;
        private static int height = Screen.PrimaryScreen.Bounds.Height;
        private static string type, windowtitle, base64image;
        private static IntPtr findwindow;
        private static bool closed = false;
        private static uint PW_CLIENTONLY = 0x1;
        private static uint PW_RENDERFULLCONTENT = 0x2;
        private static uint flags = PW_CLIENTONLY | PW_RENDERFULLCONTENT;
        private Rectangle rc;
        private Bitmap bmp;
        private Graphics gfxBmp;
        private IntPtr hdcBitmap;
        private Bitmap bitmap;
        private ImageCodecInfo jpegEncoder;
        private EncoderParameters encoderParameters;
        private const int GWL_STYLE = -16;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_OVERLAPPED = 0x00000000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_TABSTOP = 0x00010000;
        private const uint WS_VISIBLE = 0x10000000;
        private static int[] wd = { 2, 2 };
        private static int[] wu = { 2, 2 };
        public static void valchanged(int n, bool val)
        {
            if (val)
            {
                if (wd[n] <= 1)
                {
                    wd[n] = wd[n] + 1;
                }
                wu[n] = 0;
            }
            else
            {
                if (wu[n] <= 1)
                {
                    wu[n] = wu[n] + 1;
                }
                wd[n] = 0;
            }
        }
        private async void Form1_Shown(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            Task.Run(() => StartWindowTitleRemover());
            this.Size = new System.Drawing.Size(width, height);
            this.Location = new System.Drawing.Point(0, 0);
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-web-security", "--allow-file-access-from-files", "--allow-file-access");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webView21.EnsureCoreWebView2Async(environment);
            webView21.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", "assets", CoreWebView2HostResourceAccessKind.DenyCors);
            webView21.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView21.KeyDown += WebView21_KeyDown;
            webView21.Source = new Uri("https://appassets/index.html");
            webView21.Dock = DockStyle.Fill;
            webView21.DefaultBackgroundColor = Color.Transparent;
            this.Controls.Add(webView21);
            using (System.IO.StreamReader file = new System.IO.StreamReader("params.txt"))
            {
                file.ReadLine();
                type = file.ReadLine();
                file.ReadLine();
                windowtitle = file.ReadLine();
            }
            List<string> listrecords = new List<string>();
            listrecords = GetWindowTitles();
            string record = windowtitle;
            windowtitle = await Form2.ShowDialog("Window Titles", "What should be the window to handle capture?", record, listrecords);
            jpegEncoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, 255);
            findwindow = FindWindow(null, windowtitle);
            GetWindowRect(findwindow, out rc);
            bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            gfxBmp = Graphics.FromImage(bmp);
        }
        private void StartWindowTitleRemover()
        {
            while (true)
            {
                valchanged(0, GetAsyncKeyState(Keys.PageDown));
                if (wu[0] == 1)
                {
                    int width = Screen.PrimaryScreen.Bounds.Width;
                    int height = Screen.PrimaryScreen.Bounds.Height;
                    IntPtr window = GetForegroundWindow();
                    SetWindowLong(window, GWL_STYLE, WS_SYSMENU);
                    SetWindowPos(window, -2, 0, 0, width, height, 0x0040);
                    DrawMenuBar(window);
                }
                valchanged(1, GetAsyncKeyState(Keys.PageUp));
                if (wu[1] == 1)
                {
                    IntPtr window = GetForegroundWindow();
                    SetWindowLong(window, GWL_STYLE, WS_CAPTION | WS_POPUP | WS_BORDER | WS_SYSMENU | WS_TABSTOP | WS_VISIBLE | WS_OVERLAPPED | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                    DrawMenuBar(window);
                }
                System.Threading.Thread.Sleep(100);
            }
        }
        public List<string> GetWindowTitles()
        {
            List<string> titles = new List<string>();
            foreach (Process proc in Process.GetProcesses())
            {
                string title = proc.MainWindowTitle;
                if (title != null & title != "")
                    titles.Add(title);
            }
            return titles;
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            OnKeyDown(keyData);
            return true;
        }
        private void WebView21_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        private void OnKeyDown(Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                const string message = "• Author: Michaël André Franiatte.\n\r\n\r• Contact: michael.franiatte@gmail.com.\n\r\n\r• Publisher: https://github.com/michaelandrefraniatte.\n\r\n\r• Copyrights: All rights reserved, no permissions granted.\n\r\n\r• License: Not open source, not free of charge to use.";
                const string caption = "About";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private async void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                bitmap = PrintWindow(findwindow);
                if (type == "1")
                {
                    bitmap = new Bitmap(bitmap, new Size(bitmap.Width / 4, bitmap.Height / 4));
                }
                if (type == "2")
                {
                    bitmap = ImageToGrayScale(bitmap);
                }
                byte[] imageArray = ImageToByteArray(bitmap);
                base64image = Convert.ToBase64String(imageArray);
                await execScriptHelper($"setBackground('{base64image.ToString()}');");
            }
            catch { }
        }
        public Bitmap PrintWindow(IntPtr hwnd)
        {
            hdcBitmap = gfxBmp.GetHdc();
            PrintWindow(hwnd, hdcBitmap, PW_CLIENTONLY | PW_RENDERFULLCONTENT);
            gfxBmp.ReleaseHdc(hdcBitmap);
            return bmp;
        }
        public static Bitmap ImageToGrayScale(Bitmap Bmp)
        {
            Bitmap newBitmap = new Bitmap(Bmp.Width, Bmp.Height);
            Graphics g = Graphics.FromImage(newBitmap);
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(Bmp, new Rectangle(0, 0, Bmp.Width, Bmp.Height), 0, 0, Bmp.Width, Bmp.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }
        public byte[] ImageToByteArray(Bitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, jpegEncoder, encoderParameters);
                return ms.ToArray();
            }
        }
        private async Task<String> execScriptHelper(String script)
        {
            var x = await webView21.ExecuteScriptAsync(script).ConfigureAwait(false);
            return x;
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            closed = true;
            gfxBmp.Dispose();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("params.txt"))
            {
                file.WriteLine("// 1: pixel art, 2: grayscale");
                file.WriteLine(type);
                file.WriteLine("// Window title");
                file.WriteLine(windowtitle);
            }
        }
    }
}