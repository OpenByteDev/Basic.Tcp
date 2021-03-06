﻿using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Flare.Tcp.Test {
    [TestFixture]
    public static class ConcurrentFlareTcpServerTests {
        [Test]
        public static void FreesSocket() {
            var port = Utils.GetRandomClientPort();
            using var server = new ConcurrentFlareTcpServer();
            var clientTask = Task.Run(() => {
                using var client = new FlareTcpClient();
                client.Connect(IPAddress.Loopback, port);
                client.Disconnect();
            });
            server.ClientDisconnected += _ => {
                server.Shutdown();
            };
            server.Listen(port);
            Assert.IsTrue(clientTask.Wait(TimeSpan.FromSeconds(5)), "Client Task did not complete successfully.");
            Assert.IsFalse(Utils.IsPortInUse(port), "Port is still in use after server shutdown.");
        }

        [Test]
        public static void ConnectedEventRaised() {
            var port = Utils.GetRandomClientPort();
            using var clientConnectedEvent = new ManualResetEventSlim();

            using var server = new ConcurrentFlareTcpServer();
            server.ClientConnected += _ => {
                clientConnectedEvent.Set();
            };
            var listenTask = Task.Run(() => server.ListenAsync(port));

            using var client = new FlareTcpClient();
            client.Connect(IPAddress.Loopback, port);
            Assert.IsTrue(clientConnectedEvent.Wait(TimeSpan.FromSeconds(5)));

            client.Disconnect();
            server.Shutdown();
            Assert.IsTrue(listenTask.Wait(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public static void DisconnectedEventRaised() {
            var port = Utils.GetRandomClientPort();
            using var clientDisconnectedEvent = new ManualResetEventSlim();

            using var server = new ConcurrentFlareTcpServer();
            server.ClientDisconnected += _ => {
                clientDisconnectedEvent.Set();
            };
            var listenTask = Task.Run(() => server.ListenAsync(port));

            using var client = new FlareTcpClient();
            client.Connect(IPAddress.Loopback, port);
            Assert.IsFalse(clientDisconnectedEvent.IsSet);

            client.Disconnect();
            Assert.IsTrue(clientDisconnectedEvent.Wait(TimeSpan.FromSeconds(5)));
            server.Shutdown();
            Assert.IsTrue(listenTask.Wait(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public static void CanReceiveMessage() {
            var port = Utils.GetRandomClientPort();
            byte[] testMessage = Encoding.UTF8.GetBytes("Test");
            using var messageReceivedEvent = new ManualResetEventSlim();

            using var server = new ConcurrentFlareTcpServer();
            server.MessageReceived += (_, message) => {
                Assert.AreEqual(message.Span.ToArray(), testMessage);
                messageReceivedEvent.Set();
                message.Dispose();
            };
            var listenTask = Task.Run(() => server.ListenAsync(port));

            using var client = new FlareTcpClient();
            client.Connect(IPAddress.Loopback, port);
            Assert.IsFalse(messageReceivedEvent.IsSet);
            client.WriteMessage(testMessage);
            client.Disconnect();
            Assert.IsTrue(messageReceivedEvent.Wait(TimeSpan.FromSeconds(5)));
            server.Shutdown();
            Assert.IsTrue(listenTask.Wait(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public static void CanSendMessage() {
            var port = Utils.GetRandomClientPort();
            byte[] testMessage = Encoding.UTF8.GetBytes("Test");

            using var server = new ConcurrentFlareTcpServer();
            server.ClientConnected += clientId => {
                server.EnqueueMessage(clientId, testMessage);
            };
            var listenTask = Task.Run(() => server.ListenAsync(port));

            using var client = new FlareTcpClient();
            client.Connect(IPAddress.Loopback, port);
            using var message = client.ReadNextMessage();
            Assert.AreEqual(message.Span.ToArray(), testMessage);
            client.Disconnect();
            server.Shutdown();
            Assert.IsTrue(listenTask.Wait(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public static async Task CanSendMessageAsync() {
            var port = Utils.GetRandomClientPort();
            byte[] testMessage = Encoding.UTF8.GetBytes("Test");
            ValueTask messageWriteTask = default;

            using var server = new ConcurrentFlareTcpServer();
            server.ClientConnected += clientId => {
                messageWriteTask = server.EnqueueMessageAsync(clientId, testMessage);
            };
            var listenTask = Task.Run(() => server.ListenAsync(port));

            using var client = new FlareTcpClient();
            client.Connect(IPAddress.Loopback, port);
            await Utils.WithTimeout(messageWriteTask, TimeSpan.FromSeconds(5));
            using var message = client.ReadNextMessage();
            Assert.AreEqual(message.Span.ToArray(), testMessage);
            client.Disconnect();
            server.Shutdown();
            await Utils.WithTimeout(listenTask, TimeSpan.FromSeconds(5));
        }

        [Test]
        public static async Task CanSendMessageAndFree() {
            var port = Utils.GetRandomClientPort();
            byte[] testMessage = Encoding.UTF8.GetBytes("Test");

            using var server = new ConcurrentFlareTcpServer();
            server.MessageReceived += (clientId, message) => {
                server.EnqueueMessage(clientId, message);
            };
            var listenTask = Task.Run(() => server.ListenAsync(port));

            using var client = new FlareTcpClient();
            client.Connect(IPAddress.Loopback, port);
            client.WriteMessage(testMessage);
            using var message = client.ReadNextMessage();
            Assert.AreEqual(message.Span.ToArray(), testMessage);
            client.Disconnect();
            server.Shutdown();
            await Utils.WithTimeout(listenTask, TimeSpan.FromSeconds(5));
        }

        [Test]
        public static async Task CanSendMessageAndWaitAsync() {
            var port = Utils.GetRandomClientPort();
            using var messageReceivedEvent = new ManualResetEventSlim();
            byte[] testMessage = Encoding.UTF8.GetBytes("Test");
            Task messageWriteTask = null;

            using var server = new ConcurrentFlareTcpServer();
            server.ClientConnected += clientId => {
                messageWriteTask = server.EnqueueMessageAndWaitUntilSentAsync(clientId, testMessage);
                messageReceivedEvent.Set();
            };
            var listenTask = Task.Run(() => server.ListenAsync(port));
            using var client = new FlareTcpClient();
            client.Connect(IPAddress.Loopback, port);
            Assert.IsTrue(messageReceivedEvent.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsNotNull(messageWriteTask);
            await Utils.WithTimeout(messageWriteTask, TimeSpan.FromSeconds(5));
            using var message = client.ReadNextMessage();
            Assert.AreEqual(message.Span.ToArray(), testMessage);
            client.Disconnect();
            server.Shutdown();
            await Utils.WithTimeout(listenTask, TimeSpan.FromSeconds(5));
        }
    }
}
