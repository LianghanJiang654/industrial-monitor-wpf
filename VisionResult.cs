using System.Collections.Generic;

namespace FactorialApp
{
    public class VisionMark
    {
        public double X { get; set; }
        public double Y { get; set; }

        public double Machine_X { get; set; }
        public double Machine_Y { get; set; }

        public double Angle { get; set; }
        public double Area { get; set; }
    }

    public class VisionResult
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<VisionMark> Marks { get; set; } = new List<VisionMark>();
        public string? Message { get; set; }
    }
}