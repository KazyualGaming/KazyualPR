using System;
using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using RobustTimer = Robust.Shared.Timing.Timer;

namespace Content.Server._Sandwich.Shipyard.Systems;

public sealed class ShiftUpdateAnnouncementSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMinutes(15);
    private const string AnnouncementText = "Update found! After this shift the server will be (temporary) down for updates!";
    private const string AnnouncementDing = "/Audio/_Goobstation/Effects/ding.ogg";

    private CancellationTokenSource? _repeatToken;
    private bool _active;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        StopRepeating();
    }

    public bool AnnounceNowAndEnsureLoop()
    {
        SendAnnouncement();

        if (_active)
            return false;

        _active = true;
        _repeatToken = new CancellationTokenSource();
        RobustTimer.SpawnRepeating(RepeatInterval, () =>
        {
            if (!_active || _gameTicker.RunLevel == GameRunLevel.PostRound)
                return;

            SendAnnouncement();
        }, _repeatToken.Token);

        return true;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!_active || ev.New != GameRunLevel.PostRound)
            return;

        SendAnnouncement();
        StopRepeating();
    }

    private void StopRepeating()
    {
        _active = false;
        _repeatToken?.Cancel();
        _repeatToken?.Dispose();
        _repeatToken = null;
    }

    private void SendAnnouncement()
    {
        _chatManager.DispatchServerAnnouncement(AnnouncementText);
        _audio.PlayGlobal(AnnouncementDing, Filter.Broadcast(), true);
    }

    public bool IsActive => _active;
}
