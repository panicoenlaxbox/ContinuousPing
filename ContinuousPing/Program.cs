using CommandLine;
using System.Net.NetworkInformation;

Options? options = null;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(o =>
    {
        options = o;
    });

if (options is not null)
{
    await Main(options);
}

async Task Main(Options options)
{    
    while (true)
    {
        var networkInterface = GetActiveNetworkInterface();
        var reply = await PingAsync(options.HostName);
        var message = BuildMessage(networkInterface, reply);
        await WriteMessageAsync(options.Path, message);
        await WaitAsync(options.Interval);
    }
}

async Task WriteMessageAsync(string path, string message)
{
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

string BuildMessage(NetworkInterface networkInterface, PingReply reply)
{
    return $"{DateTime.Now:G} {networkInterface.Name}({networkInterface.Description}) {reply.Status}";
}

class Options
{
    [Option("hostname", Required = true)]
    public string HostName { get; set; }
    [Option("path", Required = true)]
    public string Path { get; set; }
    [Option("interval", Required = true, HelpText = "Seconds")]
    public int Interval { get; set; }
}