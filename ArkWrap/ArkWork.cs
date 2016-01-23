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
using Microsoft.Win32.SafeHandles;
using System.IO.MemoryMappedFiles;

/// <summary>
/// 세 가지 모드
/// 1. 디스크 리딩 -> 디스크 해제
/// 2. 디스크 리딩 -> 메모리 -> 디스크 해제 
/// 3. 디스크 리딩 -> 메모리 -> 메모리 해제 -> 디스크
/// </summary>
namespace ArkWrap
{
    /// <summary>
    /// 
    /// </summary>
    public class Item
    {
        ~Item() { if (this.data != null) this.data.Dispose(); }
        /// <summary>
        /// 압축된 상태의 원 파일 정보
        /// </summary>
        public readonly Ark.SArkFileItem info;
        /// <summary>
        /// 인덱스 추가
        /// </summary>
        private readonly Int32 index;
        private string path;
        /// <summary>
        /// 실제 작업할 이름(확장자 제외)
        /// </summary>
        private string name;
        /// <summary>
        /// 확장자
        /// </summary>
        private string ext;
        /// <summary>
        /// 메모리상(압축해제된) 데이터
        /// </summary>
        //private MemoryStream memory;
        private MemoryStream data;

        public bool IsFolder { get { return (info.attrib & Ark.ARK_FILEATTR_DIRECTORY) == Ark.ARK_FILEATTR_DIRECTORY; } }

        public string GetOriginName() { return this.info.Filename; }
        public void SetName(string str) { this.name = str; }
        public string GetName() { return this.name; }
        public void SetExt(string str) { this.ext = str; }
        public string GetExt() { return this.ext; }
        public string GetNameNExt() { return this.name + "." + this.ext; }
        public void EditMemory(Action<Stream> act)
        {
            if (this.IsFolder)
                return;

            if (this.data == null)
            {
                var size = Convert.ToInt32(this.info.uncompressedSize);
                using (var buffer = MemoryMappedFile.CreateNew(this.name, size))
                {
                    using (var view = buffer.CreateViewStream())
                    {
                        var result = Ark.ExtractOneToBytes(index, view.SafeMemoryMappedViewHandle, size);

                        this.data = new MemoryStream(size);
                        view.CopyTo(this.data);
                    }
                }
            }

            act(this.data);
        }
        public Ark.SArkFileItem GetInfo() { return this.info; }

        public Item(Int32 index, Ark.SArkFileItem info)
        {
            this.index = index;
            this.info = info;
            this.path = Path.GetFullPath(info.Filename);
            this.name = info.IsFolder() ? info.Filename : Path.GetFileNameWithoutExtension(info.Filename);
            this.ext = info.IsFolder() ? null : Path.GetExtension(info.Filename).Substring(1);
            this.data = null;
            //this.name = Regex.Replace(info.Filename, "^[^\n]+\\\\", "");
        }

        public void WriteToDisk(string path)
        {
            KS.Util.MemoryToDisk(this.data, path, this.GetNameNExt());
            this.data.Dispose();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ArkWork : SingletonBase<ArkWork>
    {
        private const string TARGET = "TARGET";

        private ArkWork() { }
        ~ArkWork()
        {
            try
            {
                var target = MemoryMappedFile.OpenExisting(TARGET);
                target.Dispose();
            }
            catch (FileNotFoundException)
            {
                //
            }
        }

        private string path;
        //private MemoryMappedFile file;  

        private List<Item> archive;
        public Item Archive(Int32 index) { return archive[index]; }

        private void load(Func<string, string, bool> act, string path, string pass = null)
        {
            KS.Util.DebugMSG("ArkWork::Load : " + path);

            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException();

            this.path = path;
            var err = Ark.Create();
            if (err != Ark.ARKERR._NOERR)
                throw new Exception(err.ToString(), new Exception(Ark.GetLastErrorArk().ToString()));
    
            var result = act(path, pass);

            archive = new List<Item>(Ark.GetFileItemCount());


        }

        public void LoadArchive(string path, string pass = null)
        {
            load((x, y) => Ark.Open(x, y), path, pass);
        }

        public void LoadArchiveInMemory(string path, string pass = null)
        {
            Func<string, string, bool> func = (x, y) =>
            {

                var file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, TARGET);
                var view = file.CreateViewStream();

                return Ark.OpenByte(view.SafeMemoryMappedViewHandle, (Int32)view.Length, pass);
            };
            load(func, path, pass);
        }

        public void SetItems()
        {
            for (int i = 0; i < Ark.GetFileItemCount(); i++)
            {
                var info = Ark.GetFileItem(i);
                archive.Add(new Item(i, Ark.SArkFileItem.PtrToItem(info)));
            }
        }
    }
}
