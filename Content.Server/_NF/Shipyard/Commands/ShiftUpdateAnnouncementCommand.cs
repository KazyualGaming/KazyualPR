using Content.Server.Administration;
using Content.Server._Sandwich.Shipyard.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._NF.Shipyard.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed class ShiftUpdateAnnouncementCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "update_ready";
    public string Description => "Sends the update scheduled announcement and repeats it every 15 minutes until round end.";
    public string Help => "Usage: update_ready";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteLine(Help);
            return;
        }

        var system = _entityManager.System<ShiftUpdateAnnouncementSystem>();
        var startedLoop = system.AnnounceNowAndEnsureLoop();

        shell.WriteLine(startedLoop
            ? "Server message sent. Repeating every 15 minutes until round end."
            : "Server message sent.");
    }
}
