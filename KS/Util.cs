using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluxJpeg;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/// <summary>
/// 인생코딩
/// </summary>
namespace KS
{
    public abstract class SingletonBase<T> where T : SingletonBase<T>
    {
        #region Members

        /// <summary>
        /// Static instance. Needs to use lambda expression
        /// to construct an instance (since constructor is private).
        /// </summary>
        private static readonly Lazy<T> sInstance = new Lazy<T>(() => CreateInstanceOfT());

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance of this singleton.
        /// </summary>
        public static T Instance { get { return sInstance.Value; } }

        #endregion

        #region Methods

        /// <summary>
        /// Creates an instance of T via reflection since T's constructor is expected to be private.
        /// </summary>
        /// <returns></returns>
        private static T CreateInstanceOfT()
        {
            return Activator.CreateInstance(typeof(T), true) as T;
        }

        #endregion
    }

    public class Bounce
    {
        private enum bounceDircect
        {
            Up,
            Bottom
        }
        public delegate bool BounceOff();
        private BounceOff upBounce;
        private BounceOff bottomBounce;
        private BounceOff check;
        private bounceDircect direct;

        public Bounce()
        {
            direct = bounceDircect.Up;
        }

        public void SetBounceUp(BounceOff fnc)
        {
            upBounce = fnc;
        }

        public void SetBounceBottom(BounceOff fnc)
        {
            bottomBounce = fnc;
        }

        public bool BounceCheck()
        {
            if (check())
            {
                if (direct == bounceDircect.Up)
                {
                    check = bottomBounce;
                    direct = bounceDircect.Bottom;
                }
                else
                {
                    check = upBounce;
                    direct = bounceDircect.Up;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    #region ImageFormat
    public static class ImageFormat
    {

    public enum Type
    {
        bmp,
        jpeg,
        gif,
        tiff,
        png,
        unknown
    }
    public static Type GetImageFormat(string file)
    {
        var bytes = new byte[8];
        var fs = File.OpenRead(file);
        fs.Read(bytes, 0, bytes.Length);
        return GetImageFormat(bytes);
    }
    public static Type GetImageFormat(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var bytes = Enumerable.Range(0, 8).Select(x => (byte)stream.ReadByte()).ToArray();
        stream.Seek(0, SeekOrigin.Begin);
        return GetImageFormat(bytes);
    }
    public static Type GetImageFormat(byte[] bytes)
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
            return Type.bmp;

        if (gif.SequenceEqual(bytes.Take(gif.Length)))
            return Type.gif;

        if (png.SequenceEqual(bytes.Take(png.Length)))
            return Type.png;

        if (tiff.SequenceEqual(bytes.Take(tiff.Length)))
            return Type.tiff;

        if (tiff2.SequenceEqual(bytes.Take(tiff2.Length)))
            return Type.tiff;

        if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
            return Type.jpeg;

        if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
            return Type.jpeg;

        return Type.unknown;
    }
    }

    #endregion

    public class Util
    {
        public static void DebugMSG(string msg)
        {
            Console.WriteLine(msg);
        }

        public static FluxJpeg.Core.Image FromStream(Stream inStream)
        {
            using (Bitmap bmp = new Bitmap(Bitmap.FromStream(inStream)))
            {
                return FromBitmap(bmp);
            }
        }

        public static FluxJpeg.Core.Image FromFile(string filePath)
        {
            using (Bitmap bmp = new Bitmap(Bitmap.FromFile(filePath)))
            {
                return FromBitmap(bmp);
            }
        }

        public static FluxJpeg.Core.Image FromBitmap(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int bands = 3;
            byte[][,] raster = new byte[bands][,];

            for (int i = 0; i < bands; i++)
            {
                raster[i] = new byte[width, height];
            }

            BitmapData bd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int[] pixels = new int[bd.Width * bd.Height];
            Marshal.Copy(bd.Scan0, pixels, 0, pixels.Length);

            bitmap.UnlockBits(bd);

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    int pixel = pixels[width * row + column];
                    raster[0][column, row] = (byte)(pixel >> 16);
                    raster[1][column, row] = (byte)(pixel >> 8);
                    raster[2][column, row] = (byte)pixel;
                }
            }

            FluxJpeg.Core.ColorModel model = new FluxJpeg.Core.ColorModel { colorspace = FluxJpeg.Core.ColorSpace.RGB };

            return new FluxJpeg.Core.Image(model, raster);
        }

        public static MemoryStream ConvertStreamJPG(Stream stream, int q = 90)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var encode = new FluxJpeg.Core.DecodedJpeg(FluxJpeg.Core.Image.FromStream(stream));
            stream.Close();

            var jpg = new MemoryStream();
            new FluxJpeg.Core.Encoder.JpegEncoder(encode, q, jpg).Encode();
            return jpg;
        }

        public static void MemoryToDisk(Stream stream, string path, string name)
        {
            if (stream == null || String.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException();

            using (var file = new FileStream(path + Path.DirectorySeparatorChar + name, FileMode.CreateNew, FileAccess.Write))
            {
                try
                {
                    stream.CopyToAsync(file);
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 파일명에서 특정 단어를 제거
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string WordsPatternRemove(ref string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                input = input.Replace(match.Value, "").Trim();
                return match.Value;
            }
            return null;
        }

        /// <summary>
        /// 여러개의 특정 단어를 제거
        /// </summary>
        /// <param name="str"></param>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public static string WordsReplaceMulti(string str, string[] patterns)
        {
            foreach (var item in patterns)
            {
                str = str.Replace(item, "");
            }
            str.Trim();
            return str;
        }

        /// <summary>
        /// 휴지통으로 보내기
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool FileGoRecycle(string target)
        {
            if (string.IsNullOrEmpty(target))
                throw new ArgumentNullException();

            if (File.Exists(target))
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            else if (Directory.Exists(target))
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            else
                throw new FileNotFoundException();

            return true;
        }

    }
}
