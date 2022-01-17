using CommandLine;
using System.Net.NetworkInformation;

Options? options = null;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(o => options = o)
    .WithNotParsed(errors => Environment.Exit(-1));

if (string.IsNullOrWhiteSpace(options.ErrorPath))
{
    options.ErrorPath = Path.Combine(Path.GetDirectoryName(options.Path), Path.GetFileNameWithoutExtension(options.Path) + "_error" + Path.GetExtension(options.Path));
}

await Main(options);

async Task Main(Options options)
{
    while (true)
    {
        var networkInterface = GetActiveNetworkInterface();
        var reply = await PingAsync(options.HostName);
        var message = GetMessage(networkInterface, reply);
        var failure = reply.Status != IPStatus.Success;
        var path = !failure ? options.Path : options.ErrorPath;
        await WriteAsync(path, message, failure);
        await WaitAsync(options.Interval);
    }
}

async Task WriteAsync(string path, string message, bool failure)
{
    if (failure)
    {
        Console.ForegroundColor = ConsoleColor.Red;
    }
    else
    {
        Console.ResetColor();
    }
    Console.WriteLine(message);
    using var writer = new StreamWriter(path, append: true);
    await writer.WriteLineAsync(message);
}

NetworkInterface GetActiveNetworkInterface()
{
    return NetworkInterface.GetAllNetworkInterfaces().First(ni => ni.OperationalStatus == OperationalStatus.Up);
}

async Task<PingReply> PingAsync(string hostNameOrAddress)
{
    using var ping = new Ping();
    return await ping.SendPingAsync(hostNameOrAddress);
}

Task WaitAsync(int interval)
{
    return Task.Delay(TimeSpan.FromSeconds(interval));
}

string GetMessage(NetworkInterface networkInterface, PingReply reply)
{
    return $"{DateTime.Now:G} {networkInterface.Name}({networkInterface.Description}) {reply.Status}";
}

class Options
{
    [Option("hostname", Required = true)]
    public string HostName { get; set; }
    [Option("path", Required = true)]
    public string Path { get; set; }
    [Option("errorpath", Required = false)]
    public string ErrorPath { get; set; }
    [Option("interval", Required = true, HelpText = "Seconds")]
    public int Interval { get; set; }
}