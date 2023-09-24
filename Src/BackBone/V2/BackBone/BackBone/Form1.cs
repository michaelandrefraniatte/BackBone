using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Rectangle = System.Drawing.Rectangle;
using Bitmap = System.Drawing.Bitmap;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;

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
        private static WindowRenderTarget target;
        private static SharpDX.Direct2D1.Factory1 fact = new SharpDX.Direct2D1.Factory1();
        private static RenderTargetProperties renderProp;
        private static HwndRenderTargetProperties winProp;
        private async void Form1_Shown(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            this.Size = new System.Drawing.Size(width, height);
            this.Location = new System.Drawing.Point(0, 0);
            using (System.IO.StreamReader file = new System.IO.StreamReader("params.txt"))
            {
                file.ReadLine();
                type = file.ReadLine();
                file.ReadLine();
                windowtitle = file.ReadLine();
            }
            findwindow = FindWindow(null, windowtitle);
            GetWindowRect(findwindow, out rc);
            bmp = new Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            gfxBmp = Graphics.FromImage(bmp);
            InitDisplayCapture(this.pictureBox1.Handle);
            Task.Run(() => Start());
        }
        private static void InitDisplayCapture(IntPtr handle)
        {
            renderProp = new RenderTargetProperties()
            {
                DpiX = 0,
                DpiY = 0,
                MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_DEFAULT,
                PixelFormat = new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                Type = RenderTargetType.Hardware,
                Usage = RenderTargetUsage.None
            };
            winProp = new HwndRenderTargetProperties()
            {
                Hwnd = handle,
                PixelSize = new Size2(width, height),
                PresentOptions = PresentOptions.Immediately
            };
            target = new WindowRenderTarget(fact, renderProp, winProp);
        }
        private static void DisplayCapture(Bitmap image1)
        {
            using (var bmp = image1)
            {
                System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                SharpDX.DataStream stream = new SharpDX.DataStream(bmpData.Scan0, bmpData.Stride * bmpData.Height, true, false);
                SharpDX.Direct2D1.PixelFormat pFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);
                SharpDX.Direct2D1.BitmapProperties bmpProps = new SharpDX.Direct2D1.BitmapProperties(pFormat);
                SharpDX.Direct2D1.Bitmap result = new SharpDX.Direct2D1.Bitmap(target, new SharpDX.Size2(width, height), stream, bmpData.Stride, bmpProps);
                bmp.UnlockBits(bmpData);
                stream.Dispose();
                bmp.Dispose();
                target.BeginDraw();
                target.Clear(new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 1f));
                target.DrawBitmap(result, 1.0f, BitmapInterpolationMode.NearestNeighbor);
                target.EndDraw();
            }
        }
        private void Start()
        {
            while (!closed)
            {
                try
                {
                    bitmap = PrintWindow(findwindow);
                    if (type == "1")
                    {
                        bitmap = new Bitmap(bitmap, new Size(width / 4, height / 4));
                    }
                    if (type == "2")
                    {
                        bitmap = ImageToGrayScale(bitmap);
                    }
                    bitmap = new Bitmap(bitmap, new Size(width, height));
                    DisplayCapture(bitmap);
                }
                catch { }
                Thread.Sleep(1);
            }
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
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            closed = true;
            gfxBmp.Dispose();
        }
    }
}