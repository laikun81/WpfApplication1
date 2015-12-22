using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using ArkWrap;
using System.Text.RegularExpressions;

namespace WpfApplication1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Console.WriteLine(e);
            DoWork.Instance.Main(e.Args.Length != 0 ? e.Args.GetValue(0).ToString() : null);
        }
    }

    public class DoWork
    {
        private DoWork() { }
        private static DoWork _instance;
        public static DoWork Instance { get { return _instance = _instance == null ? new DoWork() : _instance; } }

        public enum Mode { Normal, Thumbnail }
        private Mode _mode;
        public Mode ViewMode { get { return _mode; } set { _mode = value; } }

        MainWindow main;
        ImageWindow imageWindow;

        ImageFrame[] frames;
        BitmapImage[] bmps;
        const long bmpLimit = 1024 * 1024 * 1024;

        int customWidth = 0;
        public int FrameLength { get { return frames.Length; } }

        public void Main(string file = null)
        {
            if (string.IsNullOrEmpty(file))
            {
                main = new MainWindow();
            }
            else
            {
                //main.Dispatcher.BeginInvoke((Action)delegate { ArkWork.Instance.LoadArchive(file); }).Completed += (x, y) => { LoadFrames(); };
            }
        }

        int[] imageIndex;
        public void LoadFrames()
        {
            imageWindow = new ImageWindow();

            string pattern = "^?.(bmp|gif|png|jpeg|jpg|tif)$";
            //imageIndex = ArkWork.Instance.ArchivedFiles.Where(x => Regex.IsMatch(x.Value.Filename, pattern, RegexOptions.IgnoreCase))
            //    .OrderBy(x => x.Value.fileNameW).Select(x => x.Key).ToArray();

            frames = new ImageFrame[imageIndex.Length];
            bmps = new BitmapImage[imageIndex.Length];

            imageWindow.KeyDown += image_KeyDown;

            for (int i = 0; i < imageIndex.Length; i++)
            {
                frames[i] = new ImageFrame();
                frames[i].KeyDown += image_KeyDown;
                //frames[i].lbl_name.Content = ArkWork.Instance.ArchivedFileNames[imageIndex[i]];
                imageWindow.panel.Children.Add(frames[i]);
            }
            var spl = new System.Windows.Controls.Separator();
            var spr = new System.Windows.Controls.Separator();
            spl.Width = SystemParameters.MaximizedPrimaryScreenWidth;
            spr.Width = SystemParameters.MaximizedPrimaryScreenWidth;
            spl.Height = 0;
            spr.Height = 0;
            imageWindow.panel.Children.Add(spl);
            imageWindow.panel.Children.Insert(0, spr);

            //new Thread(() => { ArkWork.Instance.ExtractToStream(); }).Start();

            if (main != null)
                main.Close();

            imageWindow.Show();
            Cursor = 0;
        }

        void imageOpen(int index)
        {
            if (index >= frames.Length || index < 0)
                return;

            System.Windows.Controls.Image image = frames[index].img;

            if (image.Source != null && (customWidth == 0 || customWidth == (int)Math.Ceiling(image.Source.Width)))
                return;

            //while (ArkWork.Instance.ExtractedStreams[imageIndex[index]] == null)
            //{
            //    Console.WriteLine("Stream null :{0}({1})", index, imageIndex[index]);
            //    Thread.Sleep(100);
            //}

            //var source = new BitmapImage();
            //var bmp = new System.Drawing.Bitmap(ArkWork.Instance.ExtractedStreams[imageIndex[index]]);
            //var bmpStream = new MemoryStream(bmp.Width * bmp.Height);

            //bmp.SetResolution(96, 96);
            //bmp.Save(bmpStream, System.Drawing.Imaging.ImageFormat.Bmp);
            //bmpStream.Seek(0, SeekOrigin.Begin);

            //source.BeginInit();
            //source.StreamSource = bmpStream;

            //if (bmp.Size.Height >= SystemParameters.MaximizedPrimaryScreenHeight)
            //    source.DecodePixelHeight = (int)SystemParameters.MaximizedPrimaryScreenHeight;
            //if (customWidth != 0 && bmp.Width > source.PixelWidth)
            //    source.DecodePixelWidth = customWidth;

            //image.Source = source;
        }

        void imageClose(int index)
        {
            if (index >= frames.Length || index < 0)
                return;

            frames[index].img.Source = null;
        }

        Point frameCenter(int index)
        {
            if (frames[index].img.Source == null)
                imageOpen(index);
            return new Point((int)(frames[index].img.Source.Width / 2), (int)(frames[index].img.Source.Height) / 2);
        }

        Point frameOffSet(int index)
        {
            if (frames[index].img.Source == null)
                imageOpen(index);
            var point = frames[index].TranslatePoint(new Point(), imageWindow.panel);
            //var point = VisualTreeHelper.GetOffset(frames[index]);
            return new Point(point.X, point.Y);
        }

        System.Windows.Threading.DispatcherOperation caching;

        int _cursor;
        int _cache = 30;
        int Cursor
        {
            get { return _cursor; }
            set
            {
                _cursor = Math.Max(Math.Min(value, frames.Length - 1), 0);
                imageWindow.scroll.ScrollToHorizontalOffset(frameOffSet(_cursor).X + frameCenter(_cursor).X - SystemParameters.MaximizedPrimaryScreenWidth / 2);

                int f = _cursor;
                int c = _cache;
                for (int i = 1; i < frames.Length; i++)
                {
                    f = f + i * (i % 2 != 0 ? 1 : -1);
                    if (f < 0 || f >= frames.Length)
                        continue;
                    if (c > 0)
                    {
                        imageWindow.Dispatcher.BeginInvoke((Action)delegate { imageOpen(f); });
                        c--;
                    }
                    else
                        imageWindow.Dispatcher.BeginInvoke((Action)delegate { imageClose(f); });
                    ;
                }
                //caching.Priority = System.Windows.Threading.DispatcherPriority.Background;
            }
        }

        void image_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Left)
            {
                Cursor += 1;
            }
            else if (e.Key == System.Windows.Input.Key.Right)
            {
                Cursor -= 1;
            }
            else if (e.Key == System.Windows.Input.Key.PageUp)
            {
                Cursor -= 10;
            }
            else if (e.Key == System.Windows.Input.Key.PageDown)
            {
                Cursor += 10;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                caching.Abort();
                imageWindow.Close();
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                frames[_cursor].img.Opacity = 50;
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                frames[_cursor].img.Opacity = 0;
            }
            Thread.Sleep(300);
        }

        enum ImageFormat
        {
            bmp,
            jpeg,
            gif,
            tiff,
            png,
            unknown
        }

        static ImageFormat GetImageFormat(byte[] bytes)
        {
            // see http://www.mikekunz.com/image_file_header.html  
            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon

            if (bmp.SequenceEqual(bytes.Take(bmp.Length)))
                return ImageFormat.bmp;

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
                return ImageFormat.gif;

            if (png.SequenceEqual(bytes.Take(png.Length)))
                return ImageFormat.png;

            if (tiff.SequenceEqual(bytes.Take(tiff.Length)))
                return ImageFormat.tiff;

            if (tiff2.SequenceEqual(bytes.Take(tiff2.Length)))
                return ImageFormat.tiff;

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
                return ImageFormat.jpeg;

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
                return ImageFormat.jpeg;

            return ImageFormat.unknown;
        }

        //System.Diagnostics.Process process = new System.Diagnostics.Process();
        //process.StartInfo.FileName = @"C:\Program Files\XnViewMP\xnview.exe";
        //process.StartInfo.Arguments = '"' + dest.ToString() + '"';
        //process.Start();
        //process.WaitForExit();
    }

}