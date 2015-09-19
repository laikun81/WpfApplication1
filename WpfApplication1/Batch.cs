using ArkWrap;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfApplication1
{
    public class BatchCG : AArkWork
    {
        string temporary = @"Z:\ImageTemp";
        FileSystemInfo target;
        DirectoryInfo parent;
        Task task;

        public bool IsEnd { get; private set; }
        private int progress;
        private int progressMax;
        public override double Progress { get { return Math.Round((double)(progress * 100) / progressMax); } }

        static string[] deltag = { "[ev only]", "[Jpg]", "[Full Rip]", "[bmp]" };
        static string[] delfilename = { "BlogAcg.info_", "BlogaAcg.info_", "girlcelly@" };
        static string[] filterfile = { "blogacg.info.jpg", "NemuAndHaruka.png", "BlogAcg.info.jpg" };
        static long[] filtersize = { 69983, 86056 };
        static string[] filterext = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".gif" };

        public BatchCG()
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, true);
        }

        protected override bool FileOpen()
        {
            // 파일 명 필터링
            if (filterfile.Any(x => x.Equals(Path.GetFileName(UnArchiveFileName), StringComparison.OrdinalIgnoreCase)) || !filterext.Contains(Path.GetExtension(UnArchiveFileName)))
            {
                progressMax--;
                return false;
            }

            return true;
        }

        protected override bool CreateFolder()
        {
            progressMax--;
            return false;
        }

        protected override bool StreamClose()
        {
            if (UnArchiveFileStream == null || UnArchiveFileStream.Length == 0)
                return true;

            // 파일 사이즈 필터링
            if (filtersize.Contains(UnArchiveFileStream.Length))
                return true;

            // 익명함수의 스코프 문제
            var name = UnArchiveFileName;
            var stream = new MemoryStream();
            UnArchiveFileStream.CopyTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
            
            // 이미지변환 스레드 기동 
            task = Task.Factory.StartNew(() =>
            {
                name = BatchUtil.ReplaceMulti(name, delfilename);
                name = BatchUtil.ReplaceMulti(name, deltag);
                name = Regex.Replace(name, "^[^\n]+\\\\", "");
                name = temporary + Path.DirectorySeparatorChar + Path.ChangeExtension(name, ".jpg");

                // jpg변환
                using (var jpg = FluxJpeg.Core.Image.ConvertStreamJPG(stream))
                {
                    // 변환된 메모리를 파일에 쓰기
                    using (var file = new FileStream(name, FileMode.CreateNew, FileAccess.Write))
                    {
                        jpg.Seek(0, SeekOrigin.Begin);
                        jpg.CopyTo(file);
                    }
                }
                progress++;
            });

            return true;
        }        

        public void Run(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException();

            target = Path.HasExtension(path) ? new FileInfo(path) as FileSystemInfo : new DirectoryInfo(path) as FileSystemInfo;
            parent = target is FileInfo ? (target as FileInfo).Directory : (target as DirectoryInfo).Parent;

            // 압축해제 준비
            if (target is DirectoryInfo)
            {
                var ev = (target as DirectoryInfo).GetFiles("*.*", SearchOption.AllDirectories);
                this.Load(ev.Length == 1 ? ev[0].FullName : ev.First(x => x.Name.Contains("ev")).FullName);
            }
            else
                this.Load(path);

            progressMax = this.FileItems.Count();

            // 임시폴더 생성
            Directory.CreateDirectory(temporary);

            this.progress = 0;
            // 압축해제
            this.Extract();

            task.ContinueWith(x =>
            {
                var arcname = parent.FullName + Path.DirectorySeparatorChar + (target is DirectoryInfo ? target.Name + ".zip" : Path.ChangeExtension(target.Name, ".zip"));
                if (File.Exists(arcname))
                    BatchUtil.GoRecycle(arcname);

                // 폴더 압축
                System.IO.Compression.ZipFile.CreateFromDirectory(temporary, arcname, System.IO.Compression.CompressionLevel.NoCompression, false);

                // 원본삭제
                BatchUtil.GoRecycle(target.FullName);

                // 임시폴더 삭제
                if (Directory.Exists(temporary))
                    Directory.Delete(temporary, true);

                this.IsEnd = true;
            });
        }
    }

    public class BatchUtil
    {
        /// <summary>
        /// 파일명에서 특정 단어를 제거
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string GetPatternRemove(ref string input, string pattern)
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
        public static string ReplaceMulti(string str, string[] patterns)
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
        public static bool GoRecycle(string target)
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
