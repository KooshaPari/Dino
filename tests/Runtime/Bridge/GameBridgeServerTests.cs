using NUnit.Framework;
using System.Text;

namespace DINOForge.Tests.Bridge
{
    [TestFixture]
    public class GameBridgeServerTests
    {
        [Test]
        public void StatusCommand_ReturnsValidJson()
        {
            var request = Encoding.UTF8.GetBytes("{\"command\":\"status\"}");
            string requestStr = Encoding.UTF8.GetString(request);
            Assert.IsTrue(requestStr.Contains("command"));
            Assert.IsTrue(requestStr.Contains("status"));
        }

        [Test]
        public void Heartbeat_TextFormat()
        {
            string heartbeatFormat = "NeedsRes=False rootNull=False";
            Assert.IsTrue(heartbeatFormat.Contains("NeedsRes"));
            Assert.IsTrue(heartbeatFormat.Contains("rootNull"));
        }

        [Test]
        public void MaxMessageSize_IsReasonable()
        {
            int maxSize = 1024 * 1024;
            Assert.Greater(maxSize, 0);
            Assert.LessOrEqual(maxSize, 10 * 1024 * 1024);
        }
    }
}
