using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ArkWrap
{
    public class ArkWork
    {
        #region instance
        private ArkWork() { }

        public static ArkWork Init()
        {
            ArkEvent.Init();
            if (!Ark.IsCreated())
            {
                Ark.ARKERR err = Ark.Init();
                if (err != Ark.ARKERR._NOERR)
                    DebugMsg(err.ToString());
            }
            return (_instance = new ArkWork());
        }
        private static ArkWork _instance;
        public static ArkWork Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ArkWork();
                return _instance;
            }
        }

        public delegate void Debug(string msg);
        public Debug _Debug;
        public static void DebugMsg(string txt)
        {
            if (ArkWork.Instance._Debug != null)
                ArkWork.Instance._Debug(txt);
            Console.WriteLine(txt);
        }
        #endregion

        public const string Temporary = @"z:\";
        private DirectoryInfo temp;
        private FileInfo originFile;

        IntPtr arcPtr;

        public Dictionary<int, Ark.SArkFileItem> ArchivedFiles;
        public string[] ArchivedFileNames { private set; get; }
        public MemoryStream[] ExtractedStreams { private set; get; }
        public void RenameFiles(string pattern, string replace)
        {
            for (int i = 0; i < ArchivedFileNames.Length; i++)
            {
                ArchivedFileNames[i] = ArchivedFileNames[i].Replace(pattern, replace);
            }
        }
        public Dictionary<string, MemoryStream> ImageStreams
        {
            get
            {
                return ExtractedStreams.Select((x, y) => new { y, x })
                    .Where(p => p.x != null && GetImageFormat(p.x.ToArray()) != ImageFormat.unknown)
                    .ToDictionary(p => ArchivedFileNames[p.y], p => p.x);
            }
        }
        public List<string> ArchivedFolders = new List<string>();
        public int ArchivedFilesCount { private set; get; }

        #region NonStatic
        public void LoadArchive(string path, string pass = null)
        {
            ArkWork.DebugMsg("ArkWork::LoadArchive : " + path);

            originFile = new FileInfo(path);
            try
            {
                if (!Ark.Open(path, pass))
                {
                    DebugMsg(Ark.GetLastErrorArk().ToString());
                    return;
                }
            }
            catch (Exception e)
            {
                DebugMsg(e.ToString());
                throw e;
            }

            loadArchive();
        }
        public void LoadArchive(byte[] bytes)
        {
            ArkWork.DebugMsg("ArkWork::LoadArchive(byte)");
            ArkWork.Init();
            arcPtr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, arcPtr, bytes.Length);

            if (!Ark.OpenStream(arcPtr, bytes.Length))
            {
                DebugMsg(Ark.GetLastErrorArk().ToString());
                return;
            }
            loadArchive();
        }
        void loadArchive()
        {
            if (Ark.IsBrokenArchive())
            {
                DebugMsg("IsBrokenArchive");
                return;
            }

            ArchivedFilesCount = Ark.GetFileItemCount();
            ArchivedFiles = new Dictionary<int, Ark.SArkFileItem>(ArchivedFilesCount);
            ArchivedFileNames = new string[ArchivedFilesCount];
            ExtractedStreams = new MemoryStream[ArchivedFilesCount];

            ArchivedFolders.Add(Path.GetFileNameWithoutExtension(originFile.Name));
            for (int i = 0; i < ArchivedFilesCount; i++)
            {
                IntPtr ptr = Ark.GetFileItem(i);
                ArchivedFiles.Add(i, (Ark.SArkFileItem)Marshal.PtrToStructure(ptr, typeof(Ark.SArkFileItem)));
                ArchivedFileNames[i] = Regex.Replace(ArchivedFiles[i].Filename, "^[^\n]+\\\\", "");
                if ((ArchivedFiles[i].attrib & Ark.ARK_FILEATTR_DIRECTORY) == Ark.ARK_FILEATTR_DIRECTORY)
                {
                    ArchivedFolders.Add(ArchivedFileNames[i]);
                }
            }
        }

        public void ExtractToStream()
        {
            int index = -1;

            ArkOutStream.Instance.Open = x =>
            {
                index++;
                ExtractedStreams[index] = new MemoryStream();
                return true;
            };

            ArkOutStream.Instance.CreateFolder = x =>
            {
                index++;
                ExtractedStreams[index] = null;
                return true;
            };

            //ArkOutStream.Instance.SetSize = x => true;

            ArkOutStream.Instance.Write = (p, c) =>
            {
                try
                {
                    byte[] buffer = new byte[c];
                    Marshal.Copy(p, buffer, 0, buffer.Length);
                    ExtractedStreams[index].SetLength(ExtractedStreams[index].Length + c);
                    ExtractedStreams[index].Write(buffer, 0, buffer.Length);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            };

            ArkOutStream.Instance.Close = () =>
            {
                if (ExtractedStreams[index] != null)
                    ExtractedStreams[index].Seek(0, SeekOrigin.Begin);
                Console.WriteLine("Extract {0:d4} : {1}", index, ArchivedFileNames[index]);
                return true;
            };

            Ark.ExtractAllToStream();
            if (arcPtr != null)
                Marshal.FreeHGlobal(arcPtr);

            Ark.Close();
        }
        public byte[] ExtractToBytes(int index)
        {
            byte[] buffer = new byte[ArchivedFiles[index].uncompressedSize];
            IntPtr ptr = Marshal.AllocHGlobal(buffer.Length);

            ArkEvent.Init();
            if (!Ark.ExtractOneToBytes(index, ptr, buffer.Length))
            {
                DebugMsg(Ark.GetLastErrorArk().ToString());
                return null;
            }
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(ptr);
            return buffer;
        }
        public string ExtractAll(string dest = null)
        {
            DebugMsg("ArkWork::ExtractAll : " + dest);
            dest = !string.IsNullOrEmpty(dest) ? dest : Temporary + ArchivedFolders.OrderByDescending(x => x.Length).First();
            Ark.ExtractAllTo(dest);
            return dest;
        }

        public void LoadTarget(string path)
        {
            ArkWork.DebugMsg("ArkWork::LoadTarget : " + path);
            originFile = new FileInfo(path);

            ArkWork.DebugMsg("ArkWork::LoadTarget : [" + originFile.Name + "] Is " + originFile.Attributes.ToString());
            if (originFile.Attributes == FileAttributes.Archive)
            {
                ArkWork.Instance.LoadArchive(originFile.FullName);
                // blogacg
                if (Ark.IsEncryptedArchive()) Ark.SetPassword("http://blogacg.info");

                var arc = ArkWork.Instance.ArchivedFiles.Where(x => Regex.IsMatch(x.Value.Filename, "^[^\n]+.(?i)(zip|rar|7z)$"));
                if (arc.Count() > 0)
                {
                    try
                    {
                        ArkWork.Instance.LoadArchive(ArkWork._instance.ExtractToBytes(arc.Count() == 1 ? 0 :
                            arc.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Value.Filename).Equals("ev", StringComparison.OrdinalIgnoreCase)).Key));
                    }
                    catch (Exception e)
                    {
                        DebugMsg(Ark.GetLastErrorArk().ToString());
                        throw e;
                    }
                }
                ArkWork.Instance.ExtractToStream();
            }
            else if (originFile.Attributes == FileAttributes.Directory)
            {
                ArkWork.Instance.ArchivedFolders.Add(originFile.Name);
                var files = new DirectoryInfo(originFile.FullName).GetFiles("*", SearchOption.AllDirectories);
                var images = new Dictionary<string, MemoryStream>();
                var arcs = new List<FileInfo>();
                try
                {
                    for (int i = files.Length - 1; i >= 0; i--)
                    {
                        var file = files[i];
                        if (GetImageFormat(file.FullName) != ImageFormat.unknown)
                            images.Add(file.FullName, new MemoryStream(File.ReadAllBytes(file.FullName)));
                        else if (file.Attributes == FileAttributes.Archive)
                            arcs.Add(file);
                        else if (file.Attributes == FileAttributes.Directory)
                            ArchivedFolders.Add(file.Name);
                    }
                    if (arcs.Count() > 0)
                    {
                        ArchivedFolders.AddRange(arcs.Select(x => Path.GetFileNameWithoutExtension(x.Name)));
                        images = images.Concat(ArkWork.ExtractToStreams(arcs.Count() == 1 ? arcs[0].FullName :
                            arcs.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Name).Equals("ev", StringComparison.OrdinalIgnoreCase)).FullName))
                            .ToDictionary(p => p.Key, p => p.Value);
                    }
                }
                catch (Exception)
                {
                    var err = Ark.GetLastErrorArk();
                    throw new Exception(err.ToString());
                }
                ArchivedFileNames = images.Keys.Select(x => Regex.Replace(x, "^[^\n]+\\\\", "")).ToArray();
                ExtractedStreams = images.Values.ToArray();
            }
        }

        public string ConvertStreamToJpg(string dest = null)
        {
            temp = new DirectoryInfo(!string.IsNullOrEmpty(dest) ? dest : Temporary + @"\" + ArchivedFolders.OrderByDescending(x => x.Length).First());
            temp.Create();

            var images = ImageStreams;
            ConvertStreamToJpg(temp.FullName, images.Keys.ToArray(), images.Values.ToArray());

            return temp.FullName;
        }

        public void CreateArchive(string name = null, string dest = null)
        {
            DebugMsg("ArkWork::CreateArchive : " + name + "  " + dest);
            name = (!string.IsNullOrEmpty(name) ? name : ArchivedFolders.OrderByDescending(x => x.Length).First()) + ".zip";
            dest = !string.IsNullOrEmpty(dest) ? dest : originFile.Directory.FullName;

            FolderToArchive(temp.FullName, dest, name);
        }
        #endregion

        public static Ark.SArkFileItem[] GetArchivedFileInfos(string path, string pass = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            DebugMsg("ArkWork::GetArchivedFileInfos : " + path);

            Init();
            if (!Ark.Open(path, pass))
            {
                DebugMsg(Ark.GetLastErrorArk().ToString());
                return null;
            }

            if (Ark.IsBrokenArchive())
            {
                DebugMsg("IsBrokenArchive");
                return null;
            }

            ArkEvent.Instance.ErrorAction = (e, f, b) =>
            {
                DebugMsg(f.Filename + " = " + e.ToString());
                return true;
            };

            return Enumerable.Range(0, Ark.GetFileItemCount()).Select(x => (Ark.SArkFileItem)Marshal.PtrToStructure(Ark.GetFileItem(x), typeof(Ark.SArkFileItem))).ToArray();
        }

        public static Stream[] ExtractToStream(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            DebugMsg("ArkWork::ExtractToStream : " + path);

            Init();
            if (!Ark.Open(path))
            {
                DebugMsg(Ark.GetLastErrorArk().ToString());
                return null;
            }

            return extractToStream();
        }
        public static Stream[] ExtractToStream(byte[] fileStream)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentNullException("fileStream");

            DebugMsg("ArkWork::ExtractToStream : " + fileStream.Length);

            Init();
            IntPtr ptr = new IntPtr(fileStream.Length);
            Marshal.Copy(fileStream, 0, ptr, fileStream.Length);
            if (!Ark.OpenStream(ptr, fileStream.Length))
            {
                DebugMsg(Ark.GetLastErrorArk().ToString());
                return null;
            }

            var result = extractToStream();
            Marshal.FreeHGlobal(ptr);

            return result;
        }
        private static Stream[] extractToStream()
        {
            var ext = new MemoryStream[Ark.GetFileItemCount()];

            int index = -1;
            ArkOutStream.Instance.Open = x =>
            {
                DebugMsg("ArkWork::ExtractToStream : Open = " + x);
                ext[index++] = new MemoryStream(0);
                return true;
            };

            ArkOutStream.Instance.CreateFolder = x =>
            {
                DebugMsg("ArkWork::ExtractToStream : CreateFolder = " + x);
                ext[index++] = null;
                return true;
            };

            ArkOutStream.Instance.Write = (p, c) =>
            {
                try
                {
                    byte[] buffer = new byte[c];
                    Marshal.Copy(p, buffer, 0, buffer.Length);
                    ext[index].SetLength(ext[index].Length + c);
                    ext[index].Write(buffer, 0, buffer.Length);
                    return true;
                }
                catch (Exception e)
                {
                    DebugMsg(e.ToString());
                    return false;
                }
            };

            ArkOutStream.Instance.Close = () =>
            {
                if (ext[index] != null)
                    ext[index].Seek(0, SeekOrigin.Begin);
                Console.WriteLine("Extract {0:d4} : {1}", index, ext[index]);
                return true;
            };

            Ark.ExtractAllToStream();

            Ark.Close();

            return ext;
        }

        public static Dictionary<string, MemoryStream> ExtractToStreams(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            ArkEvent.Init();
            if (!Ark.IsCreated())
                Ark.Init();

            ArkWork.DebugMsg("ArkWork::ExtractToStreams : " + path);

            Ark.Open(path);
            ArkOutStream.Init();
            var images = new Dictionary<string, MemoryStream>();
            var name = "";
            var stream = new MemoryStream(0);
            ArkOutStream.Instance.Open = f =>
            {
                if (!string.IsNullOrEmpty(name) && stream.Length > 0)
                    images.Add(name, stream);
                name = f; stream = new MemoryStream(0); return true;
            };
            ArkOutStream.Instance.CreateFolder = f => false;
            ArkOutStream.Instance.Write = (p, c) =>
            {
                byte[] buffer = new byte[c];
                Marshal.Copy(p, buffer, 0, buffer.Length);
                stream.SetLength(stream.Length + c);
                stream.Write(buffer, 0, buffer.Length);
                return true;
            };
            Ark.ExtractAllToStream();
            Ark.Release();

            return images;
        }

        public static void ReSize(string path, int width, int height)
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = @"C:\Users\Lai\Documents\XnView\nconvert.exe -ratio -rtype lanczos -rflag decr -resize ";
            process.StartInfo.Arguments = width + " " + height + " " + path + @"\*.*";
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// 기본 라이브러리를 사용하는 이미지 변환. 현재 사용하지 않음
        /// </summary>
        /// <param name="path"></param>
        /// <param name="names"></param>
        /// <param name="imageStreams"></param>
        public static void ConvertStreamToJpg(string path, string[] names, Stream[] imageStreams)
        {
            DebugMsg("ArkWork::ConvertStreamToJpg : " + path);

            var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
            var qlt = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
            var prm = new System.Drawing.Imaging.EncoderParameters(1);
            prm.Param[0] = qlt;

            for (int i = 0; i < imageStreams.Length; i++)
            {
                var stream = imageStreams[i];
                var format = GetImageFormat(stream);

                if (format == ImageFormat.unknown)
                    continue;

                var name = path + @"\" + names[i] + ".jpg";
                if (GetImageFormat(stream) == ImageFormat.jpeg)
                {
                    var file = new FileStream(name, FileMode.Create, FileAccess.Write);
                    stream.CopyTo(file);
                    file.Close();
                }
                else
                    new System.Drawing.Bitmap(stream).Save(name, encoder, prm);

                stream.Dispose();
                DebugMsg("ArkWork::ConvertStreamToJpg : " + name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder">압축할 대상 폴더</param>
        /// <param name="dest">압축 파일이 저장되는 폴더</param>
        public static void FolderToArchive(string folder, string dest, string name)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(dest))
                throw new ArgumentException();

            ArkWork.DebugMsg("Create Folder to Archive : " + folder + " to " + dest);

            ArkEvent.Init();
            if (!Ark.IsCreated())
                Ark.Init();
            Ark.CompressorInit();

            var target = new DirectoryInfo(folder);

            foreach (var file in target.GetFiles())
            {
                Ark.AddFileItem(file.Attributes != FileAttributes.Directory ? file.FullName : null, file.FullName.Replace(target.FullName + Path.DirectorySeparatorChar, ""), true);
            }

            if (!Ark.CreateArchive(dest + Path.DirectorySeparatorChar + name + ".zip"))
            {
                throw new Exception(Ark.GetLastErrorCompressor().ToString());
            }
            Ark.CompressorRelease();
            Ark.Release();

            ArkWork.DebugMsg("Create Archive : " + dest + Path.DirectorySeparatorChar + name + ".zip");

            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(folder, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }

        public static string JustExtract(string path, string dest = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException();

            dest = !string.IsNullOrEmpty(dest) ? dest : Temporary + Path.GetFileNameWithoutExtension(new FileInfo(path).Name);

            ArkEvent.Init();
            if (!Ark.IsCreated())
                Ark.Init();

            Ark.Open(path);
            Ark.ExtractAllTo(dest);
            Ark.Destroy();

            return dest;
        }

        #region ImageFormat
        public enum ImageFormat
        {
            bmp,
            jpeg,
            gif,
            tiff,
            png,
            unknown
        }
        public static ImageFormat GetImageFormat(string file)
        {
            var bytes = new byte[8];
            var fs = File.OpenRead(file);
            fs.Read(bytes, 0, bytes.Length);
            return GetImageFormat(bytes);
        }
        public static ImageFormat GetImageFormat(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var bytes = Enumerable.Range(0, 8).Select(x => (byte)stream.ReadByte()).ToArray();
            stream.Seek(0, SeekOrigin.Begin);
            return GetImageFormat(bytes);
        }
        public static ImageFormat GetImageFormat(byte[] bytes)
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

        #endregion
    }
}
