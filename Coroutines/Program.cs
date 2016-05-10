using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coroutines
{
    class Program
    {
        static void Main(string[] args)
        {
            var pool = new MemoryPool();
            var inputs = new SocketInput[100];
            for (int i = 0; i < inputs.Length; i++)
            {
                var input = new SocketInput(pool);
                inputs[i] = input;
                Consume(input);
            }

            RandomlyWritetoInputs(inputs);
        }
        
        private static void RandomlyWritetoInputs(SocketInput[] inputs)
        {

            var jsonData = JsonConvert.SerializeObject(new
            {
                X = 1,
                Y = 45,
                M = new[] { 1, 3, 5 },
                Z = new
                {
                    Name = "David",
                    Age = 29
                }
            });

            var data = Encoding.UTF8.GetBytes(jsonData);

            var scheduler = new Scheduler(inputs, input => Produce(input, data));
            var random = new Random();

            while (!scheduler.Completed)
            {
                int next = random.Next(inputs.Length - 1);
                for (int i = 0; i < inputs.Length; ++i)
                {
                    if (scheduler.Run(next))
                    {
                        break;
                    }
                    next = (next + 1) % inputs.Length;
                }

            }
        }

        private static async void Produce(SocketInput input, byte[] data)
        {
            int at = 0;
            int increment = 1;

            while (at < data.Length)
            {
                var iter = input.IncomingStart();

                int length = Math.Min(increment, data.Length - at);

                iter.CopyFrom(data, at, length);

                input.IncomingComplete(iter, completed: false, error: null);
                at += increment;

                // Yield
                await Task.Yield();
            }

            input.IncomingComplete(completed: true, error: null);
        }

        private static async void Consume(SocketInput input)
        {
            while (true)
            {
                await input;

                if (input.DataComplete)
                {
                    break;
                }

                var reader = new SocketInputTextReader(input, Encoding.UTF8);
                var jsonReader = new JsonTextReader(reader);
                var obj = await JObject.ReadFromAsync(jsonReader, new JsonLoadSettings() { });
                Console.WriteLine(obj);
            }
        }
    }
}
