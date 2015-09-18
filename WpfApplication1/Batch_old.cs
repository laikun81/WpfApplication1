using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArkWrap;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Diagnostics;

namespace WpfApplication1
{
    class Batch_old
    {
        public static void OnlyImage(string path, string dest = null)
        {
            ArkWork.DebugMsg("Batch::OnlyImage : " + path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            var origin = new FileInfo(path);
            var temp = ArkWork.Temporary + "batchtemporary";
            Directory.CreateDirectory(temp);
            foreach (var item in ArkWork.ExtractToStreams(path))
            {
                if (ArkWork.GetImageFormat(item.Value.ToArray()) == ArkWork.ImageFormat.unknown)
                    continue;
                var file = new FileStream(temp + Path.DirectorySeparatorChar + Regex.Replace(item.Key, "^[^\n]+\\\\", ""), FileMode.CreateNew);
                item.Value.Seek(0, SeekOrigin.Begin);
                item.Value.CopyTo(file);
                file.Close();
                item.Value.Close();
            }

            deleteIt(origin.FullName);
            ArkWork.FolderToArchive(temp, !string.IsNullOrEmpty(dest) ? dest : origin.Directory.FullName, Path.GetFileNameWithoutExtension(origin.Name));
        }

        public static void ReSize(string path, string dest = null)
        {
            ArkWork.DebugMsg("Batch::ReSize : " + path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            var origin = new FileInfo(path);
            var temp = ArkWork.Temporary + "batchtemporary";
            Directory.CreateDirectory(temp);
            foreach (var item in ArkWork.ExtractToStreams(path))
            {
                if (ArkWork.GetImageFormat(item.Value.ToArray()) == ArkWork.ImageFormat.unknown)
                    continue;
                var file = new FileStream(temp + Path.DirectorySeparatorChar + Regex.Replace(item.Key, "^[^\n]+\\\\", ""), FileMode.CreateNew);
                item.Value.Seek(0, SeekOrigin.Begin);
                item.Value.CopyTo(file);
                file.Close();
                item.Value.Close();
            }
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Users\Lai\Documents\XnView\nconvert.exe ",
                    Arguments = " -D -overwrite -ratio -rtype lanczos -rflag decr -resize 2560 1600 \"" + temp + "\\*\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.OutputDataReceived += (s, e) => ArkWork.DebugMsg(e.Data);
            process.ErrorDataReceived += (s, e) => ArkWork.DebugMsg(e.Data);
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            deleteIt(origin.FullName);
            ArkWork.FolderToArchive(temp, !string.IsNullOrEmpty(dest) ? dest : origin.Directory.FullName, Path.GetFileNameWithoutExtension(origin.Name));
        }

        public static void HCG(string path, string dest = null)
        {
            ArkWork.DebugMsg("Batch::HCG : " + path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            ArkWork.Instance.LoadTarget(path);

            ArkWork.DebugMsg("Batch::HCG : Rename Process");
            ArkWork.Instance.RenameFiles(@"BlogAcg.info_", "");

            var temp = ArkWork.Instance.ConvertStreamToJpg();

            ArkWork.DebugMsg("Batch::HCG : Filtering ");
            foreach (var target in Directory.GetFiles(temp, @"blogacg.info.jpg", SearchOption.AllDirectories))
            {
                File.Delete(target);
            }

            ArkWork.Instance.CreateArchive(null, string.IsNullOrEmpty(dest) ? Directory.GetParent(path).FullName : dest);

            deleteIt(path);
        }

        public static void HCG2(string path, string dest = null)
        {
            ArkWork.DebugMsg("Batch::HCG2 : " + path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            ArkWork.Init();
            ArkWork.Instance.LoadTarget(path);

            ArkWork.DebugMsg("Batch::HCG : Rename Process");
            ArkWork.Instance.RenameFiles(@"BlogAcg.info_", "");
            ArkWork.Instance.RenameFiles(@"girlcelly@", "");

            var tempcvt = ArkWork.Temporary + "batchtemporary";
            Directory.CreateDirectory(tempcvt);
            var temp = ArkWork.Temporary + Path.GetFileNameWithoutExtension(path);
            Directory.CreateDirectory(temp);

            for (int i = ArkWork.Instance.ExtractedStreams.Length - 1; i >= 0; i--)
            {
                var format = ArkWork.GetImageFormat(ArkWork.Instance.ExtractedStreams[i]);
                if (format == ArkWork.ImageFormat.unknown)
                    continue;

                var file = new FileStream((format == ArkWork.ImageFormat.jpeg ? temp : tempcvt) 
                    + Path.DirectorySeparatorChar + ArkWork.Instance.ArchivedFileNames[i], FileMode.CreateNew);
                ArkWork.Instance.ExtractedStreams[i].CopyTo(file);
                file.Close();
            }

            ArkWork.DebugMsg("Batch::HCG :ConvertToJpg : " + temp);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Users\Lai\Documents\XnView\nconvert.exe",
                    Arguments = " -out jpeg -D -dct 2 -subsampling 2 -rtype lanczos " + tempcvt + Path.DirectorySeparatorChar + "*",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.Unicode                    
                }
            };
            process.Start();
            process.OutputDataReceived += (s, e) => ArkWork.DebugMsg(e.Data);
            process.ErrorDataReceived += (s, e) => ArkWork.DebugMsg(e.Data);
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            foreach (var file in Directory.GetFiles(tempcvt))
                File.Move(file, temp + Path.DirectorySeparatorChar + Path.GetFileName(file));

            ArkWork.DebugMsg("Batch::HCG : Filtering ");
            foreach (var target in Directory.GetFiles(temp, @"blogacg.info.jpg", SearchOption.AllDirectories))
                File.Delete(target);
            foreach (var target in Directory.GetFiles(temp, @"NemuAndHaruka.jpg", SearchOption.AllDirectories))
                File.Delete(target);

            ArkWork.FolderToArchive(temp, string.IsNullOrEmpty(dest) ? Directory.GetParent(path).FullName : dest,
                ArkWork.Instance.ArchivedFolders.OrderByDescending(x => x.Length).First());

            deleteIt(tempcvt);
            deleteIt(path);
        }

        private static void deleteIt(string path)
        {
            if (new FileInfo(path).Attributes == FileAttributes.Directory)
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            else
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }

        public static void Thumbnail(string path, string dest = null)
        {
            ArkWork.DebugMsg("Batch::Thumbnail : " + path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(path);

            var temp = ArkWork.JustExtract(path);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = @"C:\Program Files\XnViewMP\xnview.exe";
            process.StartInfo.Arguments = '"' + temp + '"';
            process.Start();
            process.WaitForExit();

            ArkWork.FolderToArchive(temp, !string.IsNullOrEmpty(dest) ? dest : Directory.GetParent(path).FullName, Path.GetFileNameWithoutExtension(new FileInfo(path).Name));

            deleteIt(path);
        }
    }
}
