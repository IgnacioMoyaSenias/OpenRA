using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChallengeRunner
{
    class ChallengeGatewayClientTCP
    {
        private readonly string matchId;
        private readonly string botType;
        private TcpClient? tcpClient;
        private StreamReader? reader;
        private StreamWriter? writer;
        private readonly string host;
        private readonly int port;

        public Action<string>? OnMessage { get; set; }

        public ChallengeGatewayClientTCP(string matchId, string botType, string host = "127.0.0.1", int port = 50051)
        {
            this.matchId = matchId;
            this.botType = botType;
            this.host = host;
            this.port = port;
        }

        public async Task ConnectAsync()
        {
            tcpClient = new TcpClient();

            try
            {
                await tcpClient.ConnectAsync(host, port);
                var stream = tcpClient.GetStream();
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                Console.WriteLine($"Connected to TCP gateway at {host}:{port}");

                // Start listening for messages
                _ = Task.Run(ListenForMessages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to TCP gateway: {ex.Message}");
                throw;
            }
        }

        private async Task ListenForMessages()
        {
            if (reader == null) return;

            try
            {
                while (tcpClient?.Connected == true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line != null)
                    {
                        OnMessage?.Invoke(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from TCP gateway: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (writer == null) return;

            try
            {
                await writer.WriteLineAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending to TCP gateway: {ex.Message}");
            }
        }

        public bool IsConnected => tcpClient?.Connected == true;

        public void Disconnect()
        {
            writer?.Close();
            reader?.Close();
            tcpClient?.Close();
        }
    }
}
