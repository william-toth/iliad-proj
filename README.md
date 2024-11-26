# Decagon Challenge

Hello! For this challenge, I have opted to use C# for it's ease of multithreading and speed.

## Running the Code
To run my code, please do the following

1. Have .NET framework installed (link [here](https://dotnet.microsoft.com/en-us/download))
2. be in the root directory of the repo
3. Run this command in your terminal: `dotnet run {maxDegreeOfParallelism}`

## Notes
- The core logic for my submission is located in `Program.cs`
- I added the `maxDegreeOfParrelism` flag to allow the user to set the number of threads that the program will use. This is because every machine is different and will run into resources contention / memory issues at different thread counts. On my machine (2021 M1), for example, I could get up to about 3500 threads before running into issues

## Results

Sum of all scores: 9641308394

Fastest run: 32.5 seconds at ~3600 max degreee of parallelism


Because the runtime of the program depends on the machine, I believe I could reach much faster times (potentially sub 20 seconds) on a newer computer with more CPU/Memory, such as a 2024 M4 Macbook Pro, but would probably hit slower times on a less powerful machine

## Stretegies

1. Parallelize POST requests. Because the average latency of the endpoint was about 4.2 seconds, it would take about a day to run the program if we awaited the request for every line. By multithreading
2. Cache previously seen lines. There are ~20k lines and ~18k unique lines, so there is no need to send a POST request for a line we've seen before (lots of empty lines). The efficiency boost ended up being pretty trivial from this, but it is still an important strategy to 1) not make wasteful requests to our server and 2) save a lot more time with hypothetically bigger files that have lots of repeated lines
