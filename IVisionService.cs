using System.Threading.Tasks;

namespace FactorialApp
{
    public interface IVisionService
    {
        Task<VisionResult?> DetectAsync();
    }
}