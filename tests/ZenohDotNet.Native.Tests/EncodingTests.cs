using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace ZenohDotNet.Native.Tests
{
    public class EncodingTests
    {
        [Fact]
        public void Put_WithEncoding_SetsEncodingOnSample()
        {
            var keyExpr = "encoding/test/" + Guid.NewGuid().ToString("N");
            Sample? receivedSample = null;
            var received = new ManualResetEventSlim(false);

            using var session = new Session();
            using var subscriber = session.DeclareSubscriber(keyExpr, sample =>
            {
                receivedSample = sample;
                received.Set();
            });

            Thread.Sleep(100);
            session.Put(keyExpr, "{\"test\": 42}", PayloadEncoding.ApplicationJson);

            Assert.True(received.Wait(TimeSpan.FromSeconds(5)));
            Assert.NotNull(receivedSample);
            Assert.Equal(PayloadEncoding.ApplicationJson, receivedSample!.Encoding);
        }

        [Fact]
        public void Publisher_Put_WithEncoding_SetsEncodingOnSample()
        {
            var keyExpr = "encoding/publisher/" + Guid.NewGuid().ToString("N");
            Sample? receivedSample = null;
            var received = new ManualResetEventSlim(false);

            using var session = new Session();
            using var subscriber = session.DeclareSubscriber(keyExpr, sample =>
            {
                receivedSample = sample;
                received.Set();
            });
            using var publisher = session.DeclarePublisher(keyExpr);

            Thread.Sleep(100);
            publisher.Put("<xml>test</xml>", PayloadEncoding.ApplicationXml);

            Assert.True(received.Wait(TimeSpan.FromSeconds(5)));
            Assert.NotNull(receivedSample);
            Assert.Equal(PayloadEncoding.ApplicationXml, receivedSample!.Encoding);
        }
    }
}
