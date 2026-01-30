using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace ZenohDotNet.Native.Tests
{
    public class QuerierTests
    {
        [Fact(Skip = "Requires runtime isolation - nested Tokio runtime issue")]
        public void DeclareQuerier_ValidKeyExpr_ReturnsQuerier()
        {
            using var session = new Session();
            using var querier = session.DeclareQuerier("querier/test/**");
            
            Assert.NotNull(querier);
        }

        [Fact(Skip = "Requires runtime isolation - nested Tokio runtime issue")]
        public void Querier_Get_ReceivesReplies()
        {
            var keyExpr = "querier/get/" + Guid.NewGuid().ToString("N");
            var replies = new List<Sample>();
            var received = new ManualResetEventSlim(false);

            using var session = new Session();
            using var queryable = session.DeclareQueryable(keyExpr, query =>
            {
                query.Reply(keyExpr, "Hello from queryable");
            });
            using var querier = session.DeclareQuerier(keyExpr);

            Thread.Sleep(100);

            querier.Get(sample =>
            {
                replies.Add(sample);
                received.Set();
            });

            Assert.True(received.Wait(TimeSpan.FromSeconds(5)));
            Assert.Single(replies);
            Assert.Equal("Hello from queryable", replies[0].GetPayloadAsString());
        }

        [Fact(Skip = "Requires runtime isolation - nested Tokio runtime issue")]
        public void Querier_MultipleGet_ReceivesReplies()
        {
            var keyExpr = "querier/multi/" + Guid.NewGuid().ToString("N");
            int queryCount = 0;

            using var session = new Session();
            using var queryable = session.DeclareQueryable(keyExpr, query =>
            {
                queryCount++;
                query.Reply(keyExpr, $"Reply {queryCount}");
            });
            using var querier = session.DeclareQuerier(keyExpr);

            Thread.Sleep(100);

            for (int i = 0; i < 3; i++)
            {
                var replies = new List<Sample>();
                var received = new ManualResetEventSlim(false);

                querier.Get(sample =>
                {
                    replies.Add(sample);
                    received.Set();
                });

                Assert.True(received.Wait(TimeSpan.FromSeconds(5)));
                Assert.Single(replies);
            }

            Assert.Equal(3, queryCount);
        }
    }
}
