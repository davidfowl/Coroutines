using System;
using System.Text;
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
            var scheduler = new Scheduler(inputs.Length);
            var random = new Random();

            while (!scheduler.AllDone)
            {
                // This isn't very random
                int next = random.Next(inputs.Length - 1);
                for (int i = 0; i < inputs.Length; ++i)
                {
                    if (!scheduler.IsFinished(next))
                    {
                        break;
                    }
                    next = (next + 1) % inputs.Length;
                }

                Scheduler.CurrentSlot = next;

                var continuation = scheduler.GetContinuation(next);

                if (continuation == null)
                {
                    var input = inputs[next];
                    Produce(input, scheduler);
                }
                else
                {
                    continuation();
                }

                if (!scheduler.HasContinuation(next))
                {
                    // Done!
                    scheduler.MarkCompleted(next);
                }
            }
        }

        private static async void Produce(SocketInput input, Scheduler scheduler)
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
            int at = 0;
            int increment = 1;

            while (at < data.Length)
            {
                var iter = input.IncomingStart();

                int length = Math.Min(increment, data.Length - at);

                iter.CopyFrom(data, at, length);

                input.IncomingComplete(iter, completed: false, error: null);
                at += increment;

                // Give up our quantum
                await scheduler;
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
