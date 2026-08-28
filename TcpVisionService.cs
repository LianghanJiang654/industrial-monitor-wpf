using System;
using System.IO;
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

        public async Task<VisionResult?> DetectAsync(
            string imageName,
            string thresholdMode,
            int threshold,
            double minArea,
            double maxArea,
            double positionTolerance,
            double angleTolerance,
            double areaTolerancePercent)
        {
            try
            {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(_ip, _port);

                using NetworkStream stream = client.GetStream();

                var requestObject = new
                {
                    command = "detect",
                    image_name = imageName,
                    threshold_mode = thresholdMode,
                    threshold = threshold,
                    min_area = minArea,
                    max_area = maxArea,
                    position_tolerance = positionTolerance,
                    angle_tolerance = angleTolerance,
                    area_tolerance_percent = areaTolerancePercent
                };

                byte[] requestBytes =
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestObject));

                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                using MemoryStream responseBuffer = new MemoryStream();
                byte[] buffer = new byte[8192];

                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (read == 0)
                        break;

                    responseBuffer.Write(buffer, 0, read);
                }

                string responseJson =
                    Encoding.UTF8.GetString(responseBuffer.ToArray());

                return JsonSerializer.Deserialize<VisionResult>(
                    responseJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                return new VisionResult
                {
                    Success = false,
                    InspectionPass = false,
                    Message = ex.Message
                };
            }
        }
    }
}
