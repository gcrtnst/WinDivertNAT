using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using WinDivertNAT;

namespace WinDivertNATTests
{
    [TestClass]
    public class SafeHandleReferenceTests
    {
        [TestMethod]
        public void Ctor_NullHandle_SetsInvalid()
        {
            var invalid = (IntPtr)(-1);
            using var href = new SafeHandleReference(null, invalid);
            Assert.AreEqual(invalid, href.RawHandle);
        }

        [TestMethod]
        public void Ctor_ValidHandle_SetsHandle()
        {
            using var handle = new SafeTestHandle();
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            Assert.AreEqual(handle.DangerousGetHandle(), href.RawHandle);
        }

        [TestMethod]
        public void Dispose_CallTwice_NoException()
        {
            using var handle = new SafeTestHandle();
            var href = new SafeHandleReference(handle, (IntPtr)(-1));
            href.Dispose();
            href.Dispose();
        }
    }

    internal class SafeTestHandle : SafeHandle
    {
        public bool Released = false;
        public override bool IsInvalid => false;

        public SafeTestHandle() : base((IntPtr)1, true) { }

        protected override bool ReleaseHandle()
        {
            Released = true;
            return true;
        }
    }
}
