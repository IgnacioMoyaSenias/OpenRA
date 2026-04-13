using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChallengeRunner
{
    class ChallengeGatewayClient
    {
        private readonly string matchId;
        private readonly string botType;
        private ClientWebSocket? webSocket;
        private readonly Uri gatewayUri;

        public Action<string>? OnMessage { get; set; }

        public ChallengeGatewayClient(string matchId, string botType)
        {
            this.matchId = matchId;
            this.botType = botType;
            gatewayUri = new Uri($"ws://127.0.0.1:8080/{matchId}/{botType}");
        }

        public async Task ConnectAsync()
        {
            webSocket = new ClientWebSocket();

            try
            {
                Console.WriteLine($"Connecting to {gatewayUri}...");
                await webSocket.ConnectAsync(gatewayUri, CancellationToken.None);
                Console.WriteLine("Connected to gateway");

                // Start listening for messages
                _ = ListenForMessagesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                throw;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task ListenForMessagesAsync()
        {
            var buffer = new byte[4096];

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        OnMessage?.Invoke(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }
        }

        public async Task DisconnectAsync()
        {
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
            }
        }
    }
}
