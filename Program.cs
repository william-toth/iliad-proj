using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

class Program
{
    static long sum = 0; // Total sum
    static object sumLock = new object();
    static Dictionary<string,long> resultsCache = new Dictionary<string,long>(); // Results cache for lines we've seen before
    static object resultsCacheLock = new object();
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
                        if (resultsCache.ContainsKey(line)) {
                            lock (sumLock)
                            {
                                sum += resultsCache[line];
                            }
                        }
                        else {
                            await ProcessLineAsync(client, url, line);
                        }
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

        double elapsedTime = Math.Round(ts.TotalSeconds, 1);
        Console.WriteLine($"Sum of all scores: {sum}");
        Console.WriteLine($"RunTime: {elapsedTime} seconds" );
    }

    static async Task ProcessLineAsync(HttpClient client, string url, string line)
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
                    var deserializedResult = await JsonSerializer.DeserializeAsync<Dictionary<string, long>>(jsonResponse) ?? new Dictionary<string,long>();

                    if (deserializedResult.TryGetValue("score", out var score))
                    {
                        // Add to cache
                        if (!resultsCache.ContainsKey(line))
                        {
                            lock (resultsCacheLock)
                            {
                                resultsCache.Add(line, score);
                            }

                        }

                        // Increment sum
                        lock (sumLock)
                        {
                            sum += score;
                        }
                    }
                    success = true;
                }
            }
            catch { }
        }
    }
}