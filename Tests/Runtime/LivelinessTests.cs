using NUnit.Framework;

namespace ZenohDotNet.Unity.Tests
{
    /// <summary>
    /// Tests for ZenohDotNet.Unity Liveliness functionality.
    /// </summary>
    public class LivelinessTests
    {
        [Test]
        public void LivelinessToken_Declare_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var token = session.DeclareLivelinessToken("test/unity/liveliness/token");

            Assert.IsNotNull(token);
            Assert.AreEqual("test/unity/liveliness/token", token.KeyExpression);
        }

        [Test]
        public void LivelinessSubscriber_Declare_Succeeds()
        {
            using var session = new ZenohDotNet.Native.Session();
            using var subscriber = session.DeclareLivelinessSubscriber(
                "test/unity/liveliness/**", 
                (keyExpr, isAlive) => { });

            Assert.IsNotNull(subscriber);
            Assert.AreEqual("test/unity/liveliness/**", subscriber.KeyExpression);
        }
    }
}
