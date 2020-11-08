﻿using BenchmarkDotNet.Attributes;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Basic.Tcp.Benchmark {
    public class MessageRoundtripSyncBenchmark {

        private BasicTcpServer server;
        private BasicTcpClient client;
        private byte[] data;

        [Params(1, 10, 100, 1000)]
        public int MessageCount;

        [GlobalSetup]
        public void Setup() {
            var random = new Random();
            data = new byte[1000];
            random.NextBytes(data);

            server = new BasicTcpServer(8888);
            client = new BasicTcpClient();
            server.MessageReceived += (clientId, message) => {
                server.EnqueueMessage(clientId, message.ToArray());
            };
            Task.Run(() => server.Listen());
            client.Connect(IPAddress.Loopback, 8888);
        }

        [Benchmark]
        public void MessageRoundtrip() {
            for (var i = 0; i < MessageCount; i++) {
                client.SendMessage(data);
                client.ReadMessage();
            }
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
