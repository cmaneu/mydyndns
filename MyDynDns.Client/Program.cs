using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyDynDns.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("My DynDNS Updater. Starting update.");

            HttpClient httpClient = new HttpClient();

            int autoUpdateFrequency = int.Parse(Environment.GetEnvironmentVariable("AUTOPUDATE_REFRESH")) *1000;

            do
            {
                await httpClient.GetAsync(Environment.GetEnvironmentVariable("AUTOUPDATE_ENDPOINT"));
                Console.WriteLine($"Status updated on {DateTime.Now:G}");
                await Task.Delay(autoUpdateFrequency);
            } while (true);
        }
    }
}
