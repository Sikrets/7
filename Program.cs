using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    internal class Program
    {
        const int MAX_CLIENTS = 3;
        static List<TcpClient> clients = new List<TcpClient>();
        static List<string> names = new List<string>();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            TcpListener listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
            Console.WriteLine("=== TCP CHAT SERVER STARTED (Port 8888) ===");
            Console.WriteLine($"Max clients: {MAX_CLIENTS}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                if (clients.Count >= MAX_CLIENTS)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] rejectMsg = Encoding.UTF8.GetBytes("SERVER: Connection rejected. Chat is full.");
                    await stream.WriteAsync(rejectMsg, 0, rejectMsg.Length);
                    client.Close();
                    Console.WriteLine("Rejected a client (chat full).");
                    continue;
                }

                clients.Add(client);
                NetworkStream clientStream = client.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                string name = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                names.Add(name);

                Console.WriteLine($"Client '{name}' connected. Total: {clients.Count}");

                _ = Task.Run(() => HandleClient(client, name));
            }
        }

        static async Task HandleClient(TcpClient client, string name)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string messageText = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    
                    string fullMessage = $"[{DateTime.Now:HH:mm:ss}] {name}: {messageText}";
                    Console.WriteLine(fullMessage);

                    await Broadcast(fullMessage, client);
                }
            }
            catch { }
            finally
            {
                clients.Remove(client);
                names.Remove(name);
                client.Close();
                Console.WriteLine($"Client '{name}' disconnected. Total: {clients.Count}");
                
                string exitMsg = $"SERVER: {name} left the chat.";
                await Broadcast(exitMsg, null);
            }
        }

        static async Task Broadcast(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            foreach (var client in clients.ToArray())
            {
                try
                {
                    if (client != sender)
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch { }
            }
        }
    }
}
