using NUnit.Framework;

namespace ZenohDotNet.Unity.Tests
{
    /// <summary>
    /// Tests for ZenohDotNet.Unity Publisher functionality.
    /// </summary>
    public class PublisherTests
    {
        [Test]
        public void Publisher_Put_String_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var publisher = session.DeclarePublisher("test/unity/pub");

            Assert.DoesNotThrow(() => publisher.Put("Hello from Unity Test"));
        }

        [Test]
        public void Publisher_Put_ByteArray_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var publisher = session.DeclarePublisher("test/unity/pub");

            Assert.DoesNotThrow(() => publisher.Put(new byte[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void Publisher_Put_EmptyData_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var publisher = session.DeclarePublisher("test/unity/pub");

            Assert.DoesNotThrow(() => publisher.Put(new byte[0]));
        }

        [Test]
        public void Publisher_Delete_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var publisher = session.DeclarePublisher("test/unity/delete");

            Assert.DoesNotThrow(() => publisher.Delete());
        }

        [Test]
        public void Publisher_WithOptions_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            var options = new ZenohDotNet.Native.PublisherOptions
            {
                CongestionControl = ZenohDotNet.Native.CongestionControl.Block,
                Priority = ZenohDotNet.Native.Priority.RealTime,
                IsExpress = true
            };

            using var publisher = session.DeclarePublisher("test/unity/qos", options);

            Assert.IsNotNull(publisher);
            Assert.AreEqual("test/unity/qos", publisher.KeyExpression);
        }
    }
}
