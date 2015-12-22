using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArkWrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArkWrap.Tests
{
    [TestClass()]
    public class ArkTests
    {
        [TestMethod()]
        public void OpenByteTest()
        {
            ArkWork.Instance.LoadArchiveInMemory(@"C:\Users\Lai\Desktop\test.zip");

            ArkWork.Instance.Archive(20).EditMemory(x => {
                Console.WriteLine(x);
            });

            ArkWork.Instance.Archive(20).EditMemory(x => {
                Console.WriteLine(x);
            });

            Ark.Destroy();
        }
    }
}