﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    [Trait("Category", "RequiresBroker")]
    public class BlockingQueueConsumerIntegrationTest
    {
        public const string QUEUE1_NAME = "test.queue1.BlockingQueueConsumerIntegrationTests";
        public const string QUEUE2_NAME = "test.queue2.BlockingQueueConsumerIntegrationTests";

        [Fact]
        public void TestTransactionalLowLevel()
        {
            var connectionFactory = new CachingConnectionFactory("localhost");
            var admin = new RabbitAdmin(connectionFactory);
            admin.DeclareQueue(new Queue(QUEUE1_NAME));
            admin.DeclareQueue(new Queue(QUEUE2_NAME));
            var template = new RabbitTemplate(connectionFactory);
            var blockingQueueConsumer = new BlockingQueueConsumer(
                connectionFactory,
                new DefaultMessageHeadersConverter(),
                new ActiveObjectCounter<BlockingQueueConsumer>(),
                AcknowledgeMode.AUTO,
                true,
                1,
                null,
                QUEUE1_NAME,
                QUEUE2_NAME);
            var prefix = Guid.NewGuid().ToString();
            blockingQueueConsumer.TagStrategy = new TagStrategy(prefix);
            var latch = new CountdownEvent(2);
            try
            {
                blockingQueueConsumer.Start();
                var consumers = blockingQueueConsumer.CurrentConsumers();
                Assert.Equal(2, consumers.Count);
                var tags = new List<string>() { consumers[0].ConsumerTag, consumers[1].ConsumerTag };
                Assert.Contains(prefix + "#" + QUEUE1_NAME, tags);
                Assert.Contains(prefix + "#" + QUEUE2_NAME, tags);
                blockingQueueConsumer.Stop();
                Assert.Null(template.ReceiveAndConvert<object>(QUEUE1_NAME));
            }
            finally
            {
                admin.DeleteQueue(QUEUE1_NAME);
                admin.DeleteQueue(QUEUE2_NAME);
                connectionFactory.Destroy();
            }
        }

        [Fact]
        public void TestAvoidHangAMQP_508()
        {
            var connectionFactory = new CachingConnectionFactory("localhost");
            var longName = new string('x', 300);
            var blockingQueueConsumer = new BlockingQueueConsumer(
               connectionFactory,
               new DefaultMessageHeadersConverter(),
               new ActiveObjectCounter<BlockingQueueConsumer>(),
               AcknowledgeMode.AUTO,
               true,
               1,
               null,
               longName,
               "foobar");
            try
            {
                blockingQueueConsumer.Start();
                throw new Exception("Expected exception");
            }
            catch (FatalListenerStartupException e)
            {
                Assert.IsType<R.Exceptions.WireFormattingException>(e.InnerException);
            }
            finally
            {
                connectionFactory.Destroy();
            }
        }

        public class TagStrategy : IConsumerTagStrategy
        {
            public TagStrategy(string prefix)
            {
                Prefix = prefix;
            }

            public string ServiceName { get; set; } = nameof(TagStrategy);

            public string Prefix { get; }

            public string CreateConsumerTag(string queue)
            {
                return Prefix + "#" + queue;
            }
        }
    }
}
