using Microsoft.Extensions.Logging;
using P3.Driver.Matter;
using P3.Driver.Matter.Commissioning;
using P3.Driver.Matter.DataModel.Clusters;
using P3.Driver.Matter.Storage;

// Demo bridge with a light, a dimmer, a plug and a blind. State is kept in matter-bridge.json.
//   dotnet run -- [state-file] [port]
var statePath = args.Length > 0 ? args[0] : "matter-bridge.json";
var port = args.Length > 1 ? int.Parse(args[1]) : 5540;

using var loggerFactory = LoggerFactory.Create(b => b
    .AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss.fff "; })
    .SetMinimumLevel(Environment.GetEnvironmentVariable("MATTER_TRACE") == "1" ? LogLevel.Trace : LogLevel.Debug));
var logger = loggerFactory.CreateLogger("Matter");

await using var bridge = new MatterBridge(new MatterNodeOptions { Port = port, DeviceName = "Automatica Demo Bridge" }, new FileMatterStorage(statePath), logger);
await bridge.InitializeAsync();

var light = bridge.Add(new BridgedOnOffDevice("demo-light", "Demo Light"));
light.SwitchRequested = on =>
{
    logger.LogInformation("Demo Light -> {State}", on ? "ON" : "OFF");
    return Task.CompletedTask;
};

var dimmer = bridge.Add(new BridgedDimmableLight("demo-dimmer", "Demo Dimmer"));
dimmer.SwitchRequested = on =>
{
    logger.LogInformation("Demo Dimmer -> {State}", on ? "ON" : "OFF");
    return Task.CompletedTask;
};
dimmer.LevelRequested = percent =>
{
    logger.LogInformation("Demo Dimmer -> {Percent}%", percent);
    dimmer.SetState(true, percent);
    return Task.CompletedTask;
};

var plug = bridge.Add(new BridgedOnOffDevice("demo-plug", "Demo Plug", OnOffDeviceKind.PlugInUnit));
plug.SwitchRequested = on =>
{
    logger.LogInformation("Demo Plug -> {State}", on ? "ON" : "OFF");
    return Task.CompletedTask;
};

var blind = bridge.Add(new BridgedWindowCovering("demo-blind", "Demo Blind"));
var blindPosition = 0;
blind.SetState(blindPosition, CoveringMovement.Stopped);
blind.CommandRequested = async (command, target) =>
{
    logger.LogInformation("Demo Blind -> {Command} {Target}", command, target);
    var goal = command switch
    {
        CoveringCommand.Open => 0,
        CoveringCommand.Close => 100,
        CoveringCommand.GoToPosition => target ?? blindPosition,
        _ => blindPosition
    };
    _ = Task.Run(async () =>
    {
        // simulate the motor
        var movement = goal < blindPosition ? CoveringMovement.Opening : CoveringMovement.Closing;
        while (blindPosition != goal)
        {
            blindPosition += Math.Sign(goal - blindPosition) * Math.Min(10, Math.Abs(goal - blindPosition));
            blind.SetState(blindPosition, blindPosition == goal ? CoveringMovement.Stopped : movement);
            await Task.Delay(300);
        }
    });
    await Task.CompletedTask;
};

await bridge.StartAsync();

Console.WriteLine();
Console.WriteLine($"  Manual pairing code: {SetupPayload.FormatManualCode(bridge.Node.ManualPairingCode)}");
Console.WriteLine($"  QR code payload:     {bridge.Node.QrCodePayload}");
Console.WriteLine($"  Paired fabrics:      {bridge.Node.CommissionedFabricCount}");
Console.WriteLine();
Console.WriteLine("Keys: t = toggle light (local), o = open pairing window, r = remove all pairings, q = quit");

var exit = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    exit.TrySetResult();
};

if (Console.IsInputRedirected)
{
    await exit.Task;
}
else
{
    while (!exit.Task.IsCompleted)
    {
        if (!Console.KeyAvailable)
        {
            await Task.WhenAny(exit.Task, Task.Delay(100));
            continue;
        }

        switch (Console.ReadKey(true).KeyChar)
        {
            case 't':
                light.SetOnOff(!light.IsOn);
                logger.LogInformation("Light locally switched to {State}", light.IsOn);
                break;
            case 'o':
                bridge.Node.OpenCommissioningWindow(TimeSpan.FromMinutes(15));
                logger.LogInformation("Commissioning window open for 15 minutes");
                break;
            case 'r':
                bridge.Node.RemoveAllFabrics();
                break;
            case 'q':
                exit.TrySetResult();
                break;
        }
    }
}
