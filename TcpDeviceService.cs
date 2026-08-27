using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FactorialApp
{
    public class TcpDeviceService : IDeviceService
    {
        private readonly string ip;
        private readonly int port;

        public TcpDeviceService(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public string ReadRegister(string command)
        {
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect(ip, port);

                    NetworkStream stream = client.GetStream();
                    byte[] messageBytes = Encoding.ASCII.GetBytes(command);
                    stream.Write(messageBytes, 0, messageBytes.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    client.Close();
                    return response;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        return "Error: " + ex.Message;
                    }
                    Thread.Sleep(500);
                }
            }

            return "Error: Max retries exceeded";
        }
    }
}