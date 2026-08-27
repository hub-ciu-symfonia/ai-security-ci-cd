using Defense;

if (args.Length != 1)
{
    Console.Error.WriteLine("Uzycie: Defense.Cli <sciezka-do-pliku-z-diffem>");
    return 2;
}

var diffText = File.ReadAllText(args[0]);
var result = BackdoorScanner.Scan(diffText);

if (result.Suspicious)
{
    Console.Error.WriteLine("BLOKADA - wykryto podejrzany wzorzec w diffie PR:");
    foreach (var reason in result.Reasons)
    {
        Console.Error.WriteLine($"  - {reason}");
    }
    return 1;
}

Console.WriteLine("Skan bezpieczenstwa diffu: OK");
return 0;
