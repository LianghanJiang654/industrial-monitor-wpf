using System;

namespace FactorialApp
{
    public class SimulatedDeviceService : IDeviceService
    {
        private Random random = new Random();

        public string ReadRegister(string command)
        {
            if (command == "READ 0")
            {
                return (20 + random.Next(0, 10)).ToString();
            }
            else if (command == "READ 1")
            {
                return (50 + random.Next(0, 30)).ToString();
            }
            else if (command == "READ 2")
            {
                return (1000 + random.Next(0, 30)).ToString();
            }
            else
            {
                return "OK";
            }
        }
    }
}