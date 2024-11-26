using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

class Program
{
    static long sum = 0; // Total sum
    static object counterLock = new object(); // Lock for changing global variables

    static Dictionary<string,long> resultsCache = new Dictionary<string,long>(); // Results cache for lines we've seen before

    static async Task Main(string[] args)
    {
        // Start Timer
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        if (args.Length == 0) {
            Console.WriteLine("Please provide a max degree of parallelism: dotnet run {maxDegree}");
            return;
        }

        var maxDegreeOfParallelism = int.Parse(args[0]);

        Console.WriteLine($"Running Solution with max degree of parallelism: {maxDegreeOfParallelism}...");

        string url = "https://interview-challenge.decagon.workers.dev/";
        string filePath = "iliad.txt"; 

        // Read file
        var lines = File.ReadAllLines(filePath);

        using (HttpClient client = new HttpClient())
        {
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            // Process lines in a thread safe way
            foreach (var line in lines)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        await ProcessLineAsync(client, url, line);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        // Stop timer and show output
        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;

        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
        ts.Hours, ts.Minutes, ts.Seconds);
        Console.WriteLine("Sum of all scores: " + sum.ToString());
        Console.WriteLine("RunTime: " + elapsedTime);
    }

    static async Task ProcessLineAsync(HttpClient client, string url, string line)
    {
        // If we've seen a line before, no need to send an HTTP request
        if (resultsCache.ContainsKey(line)) {
            lock (counterLock)
            {
                sum += resultsCache[line];
            }
        }
        else
        {
            var content = new StringContent(line, Encoding.UTF8, "application/json");

            var success = false;

            // Retry until we succeed
            while (!success) {
                try {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStreamAsync();
                        var deserializedResult = await JsonSerializer.DeserializeAsync<Dictionary<string, long>>(jsonResponse);

                        if (deserializedResult != null) {
                            if (deserializedResult.TryGetValue("score", out long score))
                            {
                                lock (counterLock)
                                {
                                    // Add to cache if seen before
                                    if (!resultsCache.ContainsKey(line)) {
                                        resultsCache.Add(line, score);
                                    }
                                    sum += score;
                                }
                            }
                        }
                        success = true;
                    }
                }
                catch {
                    success = false;
                }
            }
        }
    }
}