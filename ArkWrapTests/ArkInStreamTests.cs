using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArkWrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace ArkWrap.Tests
{
    [TestClass()]
    public class ArkInStreamTests
    {
        [TestMethod()]
        public void LoadFileTest()
        {
            Ark.Init();
            using (var file = new FileStream(@"C:\Users\Lai\Desktop\test.zip", FileMode.Open))
            {
                var buffer = new Byte[file.Length];
                file.Read(buffer, 0, buffer.Length);

                var ptr = Marshal.AllocHGlobal(sizeof(Byte));
                ArkInStream.Instance.LoadFile(@"C:\Users\Lai\Desktop\test.zip");

            }
        }
    }
}