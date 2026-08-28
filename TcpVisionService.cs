using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FactorialApp
{
    public class TcpVisionService : IVisionService
    {
        private readonly string _ip;
        private readonly int _port;

        public TcpVisionService(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public async Task<VisionResult?> DetectAsync()
        {
            try
            {
                using TcpClient client = new TcpClient();

                await client.ConnectAsync(_ip, _port);

                using NetworkStream stream = client.GetStream();

                byte[] request = Encoding.UTF8.GetBytes("detect");
                await stream.WriteAsync(request, 0, request.Length);

                byte[] buffer = new byte[4096];
                int length = await stream.ReadAsync(buffer, 0, buffer.Length);

                string json = Encoding.UTF8.GetString(buffer, 0, length);

                return JsonSerializer.Deserialize<VisionResult>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
            }
            catch (Exception ex)
            {
                return new VisionResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}