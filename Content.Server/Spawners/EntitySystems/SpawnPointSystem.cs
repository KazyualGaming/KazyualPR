using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnPointSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // TODO: Cache all this if it ends up important.
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while ( points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Delta-V: Allow setting a desired SpawnPointType
            if (args.DesiredSpawnPointType != SpawnPointType.Unset)
            {
                var isMatchingJob = spawnPoint.SpawnType == SpawnPointType.Job &&
                    (args.Job == null || spawnPoint.Job == args.Job);

                switch (args.DesiredSpawnPointType)
                {
                    case SpawnPointType.Job when isMatchingJob:
                    case SpawnPointType.LateJoin when spawnPoint.SpawnType == SpawnPointType.LateJoin:
                    case SpawnPointType.Observer when spawnPoint.SpawnType == SpawnPointType.Observer:
                        possiblePositions.Add(xform.Coordinates);
                        break;
                    default:
                        continue;
                }

                continue; // Delta-V: Don't fall through to standard spawn point logic below
            }

            if (_gameTicker.RunLevel == GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.LateJoin)
            {
                possiblePositions.Add(xform.Coordinates);
            }

            if (_gameTicker.RunLevel != GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.Job &&
                (args.Job == null || spawnPoint.Job == args.Job))
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        // Frontier: fallback - if looking for job-specific spawn points but none exist, try LateJoin points on the same station.
        if (possiblePositions.Count == 0 && args.DesiredSpawnPointType == SpawnPointType.Job)
        {
            var lateJoinPoints = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (lateJoinPoints.MoveNext(out var uid2, out var sp2, out var xform2))
            {
                if (args.Station != null && _stationSystem.GetOwningStation(uid2, xform2) != args.Station)
                    continue;

                if (sp2.SpawnType == SpawnPointType.LateJoin)
                    possiblePositions.Add(xform2.Coordinates);
            }
        }

        if (possiblePositions.Count == 0)
        {
            // Ok we've still not returned, but we need to put them /somewhere/.
            // Frontier: try any spawn point on the correct station first.
            var points2 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (points2.MoveNext(out var uid2, out _, out var xform2))
            {
                if (args.Station == null || _stationSystem.GetOwningStation(uid2, xform2) == args.Station)
                {
                    possiblePositions.Add(xform2.Coordinates);
                    break;
                }
            }
        }

        if (possiblePositions.Count == 0)
        {
            // Last resort: any spawn point at all.
            var points3 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            if (points3.MoveNext(out _, out _, out var xform3))
            {
                possiblePositions.Add(xform3.Coordinates);
            }
            else
            {
                Log.Error("No spawn points were available!");
                return;
            }
        }

        var spawnLoc = _random.Pick(possiblePositions);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station,
            session: args.Session); // Frontier
    }
}
