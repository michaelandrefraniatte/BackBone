using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.Server;
using Microsoft.Web.WebView2.Core;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;

namespace BackBone
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
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
        private async void Form1_Shown(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            this.Size = new System.Drawing.Size(width, height);
            this.Location = new System.Drawing.Point(0, 0);
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-web-security", "--allow-file-access-from-files", "--allow-file-access");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webView21.EnsureCoreWebView2Async(environment);
            webView21.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", "assets", CoreWebView2HostResourceAccessKind.DenyCors);
            webView21.CoreWebView2.Settings.AreDevToolsEnabled = false;
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
            jpegEncoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, 255);
            findwindow = FindWindow(null, windowtitle);
            GetWindowRect(findwindow, out rc);
            bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            gfxBmp = Graphics.FromImage(bmp);
        }
        private async void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                bitmap = PrintWindow(findwindow);
                bitmap = new Bitmap(bitmap, new Size(bitmap.Width / 4, bitmap.Height / 4));
                if (type == "1")
                {
                    bitmap = new Bitmap(bitmap, new Size(bitmap.Width / 2, bitmap.Height / 2));
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
            int rgb;
            Color c;
            for (int y = 0; y < Bmp.Height; y++)
            {
                for (int x = 0; x < Bmp.Width; x++)
                {
                    c = Bmp.GetPixel(x, y);
                    rgb = (int)Math.Round(.299 * c.R + .587 * c.G + .114 * c.B);
                    Bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            return Bmp;
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
        }
    }
}