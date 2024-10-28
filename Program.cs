using System;
using System.Threading.Tasks;
using TTGHotS.Discord;

namespace TTGHotS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var bot = new DiscordBot();
                await bot.InitializeAsync();

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.WriteLine("Stack Trace: " + Environment.NewLine + "\t");
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                Console.ReadKey();
            }
        }
    }
}
