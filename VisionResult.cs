using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FactorialApp
{
    public class VisionMark
    {
        public double X { get; set; }
        public double Y { get; set; }

        [JsonPropertyName("machine_x")]
        public double Machine_X { get; set; }

        [JsonPropertyName("machine_y")]
        public double Machine_Y { get; set; }

        public double Angle { get; set; }
        public double Area { get; set; }
    }

    public class VisionResult
    {
        public bool Success { get; set; }

        [JsonPropertyName("inspection_pass")]
        public bool InspectionPass { get; set; }

        public int Count { get; set; }

        public List<VisionMark> Marks { get; set; }
            = new List<VisionMark>();

        public string? Message { get; set; }

        [JsonPropertyName("annotated_image_base64")]
        public string? AnnotatedImageBase64 { get; set; }
    }
}