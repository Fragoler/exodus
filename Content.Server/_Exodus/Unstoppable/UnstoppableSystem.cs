using Content.Server.Administration;
using Content.Shared.Exodus.Unstoppable;
using Content.Shared.Administration;
using Robust.Shared.Console;


namespace Content.Server.Exodus.Unstoppable;

public sealed partial class UnstoppableSystem : SharedUnstoppableSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;

    public override void Initialize()
    {
        base.Initialize();

        _console.RegisterCommand("unstoppable", UnstoppableCommand);
        _console.RegisterCommand("stopunstopable", StopUnstoppableCommand);
    }


    [AdminCommand(AdminFlags.Fun)]
    private void UnstoppableCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("cmd-landentity-invalid"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
        {
            shell.WriteLine($"No entity found with netUid {uidNet}");
            return;
        }

        if (!NetEntity.TryParse(args[1], out var coordEntNet) ||
            !TryGetEntity(coordEntNet, out var coordUid))
        {
            shell.WriteLine($"No entity found with netUid {coordEntNet}");
            return;
        }

        if (!float.TryParse(args[2], out var speed))
        {
            shell.WriteLine($"{speed} isnt float");
        }

        var coords = Transform(coordUid.Value).Coordinates;
        SetUnstoppableMove(uid.Value, coords, speed);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void StopUnstoppableCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-landentity-invalid"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
        {
            shell.WriteLine($"No entity found with netUid {uidNet}");
            return;
        }

        EndUnstoppableMove(uid.Value);
    }
}
