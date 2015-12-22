using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArkWrap
{
    /// <summary>
    /// 
    /// </summary>
    abstract public class AArkWork
    {
        ~AArkWork()
        {
            Ark.Destroy();
        }
        // 압축을 해제할 대상파일 경로
        protected string _path;
        // 압축파일의 내용
        public Dictionary<int, Ark.SArkFileItem> FileItems { get; private set; }
        public bool IsLoaded { get; private set; }
        public double Loading { get { return FileItems == null ? 0 : Cursor / FileItems.Count(); } }
        // 현재 커서
        public int Cursor { get; private set; }
        // 현재 해제중인 파일명
        public string UnArchiveFileName { get; private set; }
        // 현재 해제중인 스트림
        public Stream UnArchiveFileStream { get; private set; }
        // 파일 스킵플래그
        protected bool skip;
        abstract public double Progress { get; }

        public void Load(string path, string pass = null)
        {
            if (!File.Exists(path))
                throw new ArgumentException("Is Not Exist File");

            _path = path;
            Ark.ARKERR err = Ark.Create();
            if (err != Ark.ARKERR._NOERR)
                throw new Exception(err.ToString());

            if (!Ark.Open(path, pass))
                throw new FileLoadException("File Open Error" + Ark.GetLastErrorArk().ToString());

            if (Ark.IsBrokenArchive())
                throw new Exception("IsBrokenArchive");

            var length = Ark.GetFileItemCount();
            FileItems = new Dictionary<int, Ark.SArkFileItem>(length);
            for (Cursor = 0; Cursor < length; Cursor++)
            {
                //IntPtr ptr = Ark.GetFileItem(Cursor);
                //FileItems.Add(Cursor, (Ark.SArkFileItem)Marshal.PtrToStructure(ptr, typeof(Ark.SArkFileItem)));
            }
            IsLoaded = true;
        }

        public void Extract()
        {
            ArkOutStream.Instance.Open = x =>
            {
                UnArchiveFileName = x;
                skip = !FileOpen();
                UnArchiveFileStream = skip ? null : new MemoryStream();
                return true;
            };

            ArkOutStream.Instance.CreateFolder = x =>
            {
                UnArchiveFileName = x;
                UnArchiveFileStream = null;
                skip = !CreateFolder();
                return true;
            };

            //ArkOutStream.Instance.SetSize = x => true;

            ArkOutStream.Instance.Write = (p, c) =>
            {
                if (skip)
                    return true;

                try
                {
                    byte[] buffer = new byte[c];
                    Marshal.Copy(p, buffer, 0, buffer.Length);
                    UnArchiveFileStream.SetLength(UnArchiveFileStream.Length + c);
                    UnArchiveFileStream.Write(buffer, 0, buffer.Length);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            };

            ArkOutStream.Instance.Close = () =>
            {
                var result = true;
                if (!skip)
                {
                    if (UnArchiveFileStream != null)
                    {
                        UnArchiveFileStream.Seek(0, SeekOrigin.Begin);
                        using (UnArchiveFileStream)
                        {
                            result = StreamClose();
                        }
                    }
                }
                Cursor++;
                return result;
            };

            Cursor = 0;
            if (!Ark.ExtractAllToStream())
            {
                throw new Exception(Ark.GetLastErrorArk().ToString());
            }

            Ark.Close();
            Ark.Destroy();
        }

        abstract protected bool FileOpen();

        abstract protected bool CreateFolder();

        abstract protected bool StreamClose();
    }
}
