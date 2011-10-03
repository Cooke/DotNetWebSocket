using System.Net.Sockets;
using DotNetWebSocket.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class DataFrameTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var buffer = new byte[1000];            

            var sendFrame = new DataFrame("abc");            
            int sendFrameSize;
            sendFrame.WriteTo(buffer, out sendFrameSize);

            DataFrame receiveFrame;
            int receiveFrameSize;
            DataFrame.TryReadFrom(buffer, sendFrameSize, out receiveFrame, out receiveFrameSize);

            Assert.AreEqual(receiveFrameSize, sendFrameSize);
            Assert.AreEqual(receiveFrame.Message, sendFrame.Message);
        }
    }
}
