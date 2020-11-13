﻿using BenchmarkDotNet.Attributes;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Flare.Tcp.Benchmark {
    public class MessageRoundtripSyncBenchmark {

        private FlareTcpServer server;
        private FlareTcpClient client;
        private byte[] data;

        [Params(1, 1_000, 1_000_000)]
        public int MessageBytes;

        [GlobalSetup]
        public void Setup() {
            var random = new Random();
            data = new byte[MessageBytes];
            random.NextBytes(data);

            server = new FlareTcpServer(8888);
            client = new FlareTcpClient();
            server.MessageReceived += (clientId, message) => {
                server.EnqueueMessage(clientId, message.ToArray());
            };
            Task.Run(() => server.Listen());
            client.Connect(IPAddress.Loopback, 8888);
        }

        [Benchmark]
        public void MessageRoundtrip() {
            client.SendMessage(data);
            client.ReadMessage();
        }

        [GlobalCleanup]
        public void Cleanup() {
            client.Disconnect();
            client.Dispose();
            server.Stop();
            server.Dispose();
        }

    }
}
