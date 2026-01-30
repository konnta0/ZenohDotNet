using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ZenohDotNet.Unity.Tests
{
    /// <summary>
    /// Tests for ZenohDotNet.Unity Session functionality.
    /// </summary>
    public class SessionTests
    {
        [Test]
        public void Session_OpenAndClose_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            Assert.IsNotNull(session);
        }

        [Test]
        public void Session_DeclarePublisher_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var publisher = session.DeclarePublisher("test/unity/demo");

            Assert.IsNotNull(publisher);
            Assert.AreEqual("test/unity/demo", publisher.KeyExpression);
        }

        [Test]
        public void Session_DeclareSubscriber_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var subscriber = session.DeclareSubscriber("test/unity/sub", sample => { });

            Assert.IsNotNull(subscriber);
            Assert.AreEqual("test/unity/sub", subscriber.KeyExpression);
        }

        [Test]
        public void Session_GetZenohId_ReturnsValidId()
        {
            using var session = new ZenohDotNet.Native.Session();
            var zid = session.GetZenohId();

            Assert.IsNotNull(zid);
            Assert.IsNotEmpty(zid);
            Assert.GreaterOrEqual(zid.Length, 32);
        }

        [Test]
        public void Session_Put_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            Assert.DoesNotThrow(() => session.Put("test/unity/direct", "Hello"));
        }

        [Test]
        public void Session_Delete_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            Assert.DoesNotThrow(() => session.Delete("test/unity/delete"));
        }
    }
}
