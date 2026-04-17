using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Content.Server._NF.CryoSleep; // Frontier

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AGhostCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override string Command => "aghost";
    public override string Help => "aghost [-h|--hidden]";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new[] { "-h", "--hidden" };
            return CompletionResult.FromHintOptions(options.Concat(_playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name)), LocalizationManager.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var hidden = false;
        string? targetPlayer = null;

        // Parse arguments
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--hidden")
                hidden = true;
            else
                targetPlayer = arg;
        }

        // Check stealth permission for the hidden flag
        if (hidden && shell.Player != null && !_adminManager.HasAdminFlag(shell.Player, AdminFlags.Stealth))
        {
            shell.WriteError("You do not have permission to use stealth mode.");
            return;
        }

        var player = shell.Player;
        var self = player != null;
        if (player == null)
        {
            // If you are not a player, you require a player argument.
            if (targetPlayer == null)
            {
                shell.WriteError(LocalizationManager.GetString("shell-need-exactly-one-argument"));
                return;
            }

            var didFind = _playerManager.TryGetSessionByUsername(targetPlayer, out player);
            if (!didFind)
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        // If you are a player and a username is provided, a lookup is done to find the target player.
        if (targetPlayer != null)
        {
            var didFind = _playerManager.TryGetSessionByUsername(targetPlayer, out player);
            if (!didFind)
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        var mindSystem = _entities.System<SharedMindSystem>();
        var metaDataSystem = _entities.System<MetaDataSystem>();
        var ghostSystem = _entities.System<SharedGhostSystem>();
        var transformSystem = _entities.System<TransformSystem>();
        var gameTicker = _entities.System<GameTicker>();
        var eyeSystem = _entities.System<SharedEyeSystem>();
        var visibilitySystem = _entities.System<VisibilitySystem>();
        var cryoSystem = _entities.System<CryoSleepSystem>(); // Frontier

        if (!mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            shell.WriteError(self
                ? LocalizationManager.GetString("aghost-no-mind-self")
                : LocalizationManager.GetString("aghost-no-mind-other"));
            return;
        }

        if (mind.VisitingEntity != default && _entities.TryGetComponent<GhostComponent>(mind.VisitingEntity, out var oldGhostComponent))
        {
            mindSystem.UnVisit(mindId, mind);
            // If already an admin ghost, then return to body.
            if (oldGhostComponent.CanGhostInteract)
            {
                shell.WriteLine($"Player '{player?.Name}' stopped being an admin ghost.");
                return;
            }
        }

        var canReturn = mind.CurrentEntity != null
                        && !_entities.HasComponent<GhostComponent>(mind.CurrentEntity);
        var coordinates = player!.AttachedEntity != null
            ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
            : gameTicker.GetObserverSpawnPoint();
        var ghost = _entities.SpawnEntity(GameTicker.AdminObserverPrototypeName, coordinates);
        transformSystem.AttachToGridOrMap(ghost, _entities.GetComponent<TransformComponent>(ghost));

        // Set default visibility mask for admin ghosts
        if (_entities.TryGetComponent(ghost, out EyeComponent? eye))
        {
            eyeSystem.SetVisibilityMask(ghost, 7, eye);
        }

        // Set visibility layer based on hidden flag
        if (_entities.TryGetComponent(ghost, out VisibilityComponent? visibility))
        {
            visibilitySystem.SetLayer((ghost, visibility), (ushort)(hidden ? 4 : 2));
        }

        // Set ghost color to light blue when hidden
        if (hidden && _entities.TryGetComponent(ghost, out GhostComponent? ghostComp))
        {
            ghostSystem.SetColor((ghost, ghostComp), new Color(0.5f, 0.5f, 1f, 0.8f));
        }

        if (canReturn)
        {
            // TODO: Remove duplication between all this and "GamePreset.OnGhostAttempt()"...
            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                metaDataSystem.SetEntityName(ghost, mind.CharacterName);
            else if (!string.IsNullOrWhiteSpace(player.Name))
                metaDataSystem.SetEntityName(ghost, player.Name);

            mindSystem.Visit(mindId, ghost, mind);
            shell.WriteLine($"Player '{player.Name}' became an admin ghost{(hidden ? " (hidden)" : "")}. Use aghost again to return to your body.");
        }
        else
        {
            metaDataSystem.SetEntityName(ghost, player.Name);
            mindSystem.TransferTo(mindId, ghost, mind: mind);
            shell.WriteLine($"Player '{player.Name}' became an admin ghost{(hidden ? " (hidden)" : "")}.");
        }

        var comp = _entities.GetComponent<GhostComponent>(ghost);
        ghostSystem.SetCanReturnToBody((ghost, comp), canReturn);
        ghostSystem.SetCanReturnFromCryo(comp, cryoSystem.HasCryosleepingBody(player.UserId)); // Frontier

        if (!hidden && (player.Name == "FuelRat" || player.Name == "localhost@FuelRat"))
        {
            ghostSystem.SetRainbowCycle((ghost, comp), false);
            ghostSystem.SetColor((ghost, comp), Color.FromHex("#00FF00FF"));
            metaDataSystem.SetEntityDescription(ghost, "Its the master-in-chief themselves!");
            metaDataSystem.SetEntityName(ghost, "Yuuchan");
        }
    }
}
