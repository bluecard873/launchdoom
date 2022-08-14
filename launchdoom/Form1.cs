using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LaunchpadNET;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace launchdoom
{
    public partial class Form1 : Form
    {
        Interface inf = new Interface();
        Bitmap last;
        Dictionary<uint, Color> pal = new Dictionary<uint, Color>();
        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rectangle rect);
        public Form1()
        {
            
            InitializeComponent();
            Interface.LaunchpadDevice device = inf.getConnectedLaunchpads()[0];
            inf.connect(device);
            StreamReader sr = new StreamReader("pal.txt");
            while (true)
            {
                string val = sr.ReadLine();
                if (val == null)
                {
                    break;
                }
                val = val.Replace(",", "").Replace(";", "");
                string[] vals = val.Split(' ');
                pal.Add(uint.Parse(vals[0]), Color.FromArgb(int.Parse(vals[1]) * 4, int.Parse(vals[2]) * 4, int.Parse(vals[3]) * 4));
                
            }


        }

        public uint getVelo(Color target)
        {
            var colors = pal.Values.ToList();
            var col = colors[closestColor2(colors, target)];
            foreach (var kp in pal)
            {
                if (kp.Value == col)
                {
                    return kp.Key;
                }
            }
            return 0;
        }
        int closestColor2(List<Color> colors, Color target)
        {
            var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
            return colors.FindIndex(n => ColorDiff(n, target) == colorDiffs);
        }
        int ColorDiff(Color c1, Color c2)
        {
            return (int)Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
                                   + (c1.G - c2.G) * (c1.G - c2.G)
                                   + (c1.B - c2.B) * (c1.B - c2.B));
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Process p = Process.Start("gzdoom.exe");
            SetForegroundWindow(p.MainWindowHandle);
            Size = new Size(250, 250);
            Thread.Sleep(1000);
            button1.Dispose();
            while (true)
                {
                    Rectangle rect = new Rectangle();
                    IntPtr error = GetWindowRect(p.MainWindowHandle, ref rect);

                    // sometimes it gives error.
                    while (error == (IntPtr)0)
                    {
                        error = GetWindowRect(p.MainWindowHandle, ref rect);
                    }

                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                    Graphics graphics = Graphics.FromImage(bitmap);

                    graphics.CopyFromScreen(rect.Left,
                                             rect.Top,
                                             0,
                                             0,
                                             new Size(width, height),
                                             CopyPixelOperation.SourceCopy);

                    graphics.Save();

                    Bitmap resizeImage = new Bitmap(ResizeImage(bitmap, 8, 8));
                    last = new Bitmap(resizeImage);
                    resizeImage.RotateFlip(RotateFlipType.Rotate270FlipY);
                    
                    for (int x=0; x < 8; x++)
                    {
                        for (int y=0; y < 8; y++)
                        {
                            inf.setLED(x, y, (int)getVelo(resizeImage.GetPixel(x, y)));
                        }
                            
                    }
                    Invalidate();
                    Thread.Sleep(33);
                }
            
        }
        private void Form1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            
        }
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        
    }
}
