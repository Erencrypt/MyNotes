using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace MyNotes.Services
{
    internal class SingleInstanceService
    {
        internal Action<string[]>? OnArgumentsReceived;

        internal bool IsFirstInstance()
        {
            if (Semaphore.TryOpenExisting(semaphoreName, out semaphore))
            {
                Task.Run(() => { SendArguments(); Environment.Exit(0); });
                return false;
            }
            else
            {
                semaphore = new Semaphore(0, 1, semaphoreName);
                Task.Run(() => ListenForArguments());
                return true;
            }
        }

        private void ListenForArguments()
        {
            TcpListener tcpListener = new(IPAddress.Parse(localHost), localPort);
            try
            {
                tcpListener.Start();
                while (true)
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Task.Run(() => ReceivedMessage(client));
                }
            }
            catch (SocketException)
            {
                tcpListener.Stop();
                throw;
            }
        }

        private void ReceivedMessage(TcpClient tcpClient)
        {
            try
            {
                using NetworkStream networkStream = tcpClient?.GetStream()!;
                string? data = null;
                byte[] bytes = new byte[256];
                int bytesCount;
                while ((bytesCount = networkStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    data += Encoding.UTF8.GetString(bytes, 0, bytesCount);
                }
                OnArgumentsReceived(data.Split(' '));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SendArguments()
        {
            try
            {
                using TcpClient tcpClient = new(localHost, localPort);
                using NetworkStream networkStream = tcpClient.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(string.Join(" ", Environment.GetCommandLineArgs()));
                networkStream.Write(data, 0, data.Length);
            }
            catch (Exception)
            {
                throw;
            }
        }

#pragma warning disable IDE0052 // Remove unread private members
        private Semaphore? semaphore;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly string semaphoreName = $"Global\\{Environment.MachineName}-myAppName{Assembly.GetExecutingAssembly().GetName().Version}-sid{Process.GetCurrentProcess().SessionId}";
        private readonly string localHost = "127.0.0.1";
        private readonly int localPort = 19191;
    }
}
