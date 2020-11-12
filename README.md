# Flare.Tcp

[![nuget badge](https://badgen.net/nuget/v/Flare.Tcp)](https://www.nuget.org/packages/Flare.Tcp/)
[![Unlicense](https://img.shields.io/github/license/OpenByteDev/Flare.Tcp)](./UNLICENSE)

A basic sync and async multi-client message-based event-driven tcp server (and client). 

## Using the BasicTcpServer

```csharp
// create a new server that listens on port 4269
using var server = new FlareTcpServer(4269);

// attach event handlers
server.ClientConnected += clientId => {
    Console.WriteLine($"Client {clientId} connected.");
};
server.ClientDisconnected += clientId => {
    Console.WriteLine($"Client {clientId} disconnected.");
};
server.MessageReceived += (clientId, message) => {
    // echo message back to client
    server.EnqueueMessage(clientId, message.ToArray());
};

// wait for incoming connections
await server.ListenAsync();
```

## Using the BasicTcpClient

```csharp
// create a new client
using var client = new FlareTcpClient();

// attach message listener
client.MessageReceived += message => {
    // print message to console
    Console.WriteLine(Encoding.UTF8.GetString(message));
};

// connect to localhost
await client.ConnectAsync(IPAddress.Loopback, 4269);

// send test message
await client.SendMessageAsync(Encoding.UTF8.GetBytes("Anyone there?"));

// read next message from server
await client.ReadMessageAsync();

// disconnect
client.Disconnect();
```

