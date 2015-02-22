﻿using System;
using System.Diagnostics;
using System.Threading;
using Foundatio.Tests.Utility;
using Foundatio.Messaging;
using Xunit;

namespace Foundatio.Tests.Messaging {
    public abstract class MessageBusTestBase {
        protected virtual IMessageBus GetMessageBus() {
            return null;
        }

        public virtual void CanSendMessage() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                var resetEvent = new AutoResetEvent(false);
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Trace.WriteLine("Got one!");
                    Assert.Equal("Hello", msg.Data);
                    resetEvent.Set();
                });
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                });

                bool success = resetEvent.WaitOne(1000);
                Assert.True(success, "Failed to receive message.");
            }
        }

        public virtual void CanSendDelayedMessage() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                var resetEvent = new AutoResetEvent(false);
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    resetEvent.Set();
                });

                var sw = new Stopwatch();
                sw.Start();
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                }, TimeSpan.FromMilliseconds(100));

                bool success = resetEvent.WaitOne(100200);
                sw.Stop();

                Assert.True(success, "Failed to receive message.");
                Assert.True(sw.Elapsed > TimeSpan.FromMilliseconds(100));
            }
        }

        public virtual void CanSendMessageToMultipleSubscribers() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                var latch = new CountDownLatch(3);
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    latch.Signal();
                });
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    latch.Signal();
                });
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    latch.Signal();
                });
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                });

                bool success = latch.Wait(1000);
                Assert.True(success, "Failed to receive all messages.");
            }
        }

        public virtual void CanTolerateSubscriberFailure() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                var latch = new CountDownLatch(2);
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    throw new ApplicationException();
                });
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    latch.Signal();
                });
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    latch.Signal();
                });
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                });

                bool success = latch.Wait(2000);
                Assert.True(success, "Failed to receive all messages.");
            }
        }

        public virtual void WillOnlyReceiveSubscribedMessageType() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                var resetEvent = new AutoResetEvent(false);
                messageBus.Subscribe<SimpleMessageB>(msg => {
                    Assert.True(false, "Received wrong message type.");
                });
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    resetEvent.Set();
                });
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                });

                bool success = resetEvent.WaitOne(100);
                Assert.True(success, "Failed to receive message.");
            }
        }

        public virtual void WillReceiveDerivedMessageTypes() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                var latch = new CountDownLatch(2);
                messageBus.Subscribe<ISimpleMessage>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    latch.Signal();
                });
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                });
                messageBus.Publish(new SimpleMessageB {
                    Data = "Hello"
                });
                messageBus.Publish(new SimpleMessageC {
                    Data = "Hello"
                });

                bool success = latch.Wait(100);
                Assert.True(success, "Failed to receive all messages.");
            }
        }

        public virtual void CanSubscribeToAllMessageTypes() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                var latch = new CountDownLatch(3);
                messageBus.Subscribe<object>(msg => {
                    latch.Signal();
                });
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                });
                messageBus.Publish(new SimpleMessageB {
                    Data = "Hello"
                });
                messageBus.Publish(new SimpleMessageC {
                    Data = "Hello"
                });

                bool success = latch.Wait(1000);
                Assert.True(success, "Failed to receive all messages.");
            }
        }

        public virtual void WontKeepMessagesWithNoSubscribers() {
            var messageBus = GetMessageBus();
            if (messageBus == null)
                return;

            using (messageBus) {
                messageBus.Publish(new SimpleMessageA {
                    Data = "Hello"
                });

                Thread.Sleep(1000);
                var resetEvent = new AutoResetEvent(false);
                messageBus.Subscribe<SimpleMessageA>(msg => {
                    Assert.Equal("Hello", msg.Data);
                    resetEvent.Set();
                });

                bool success = resetEvent.WaitOne(1000);
                Assert.False(success, "Messages are building up.");
            }
        }
    }
}
