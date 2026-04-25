using Robust.Shared.Configuration;

namespace Content.Shared._Sandwich.CCVar;

[CVarDefs]
public sealed partial class SandwichCCVars
{
    /// <summary>
    /// Automatically starts a recall vote when the emergency shuttle is auto-called.
    /// Requires auto voting to be enabled.
    /// </summary>
    public static readonly CVarDef<bool> EvacAutoVoteEnabled =
        CVarDef.Create("sandwich.vote.evac_autovote_enabled", false, CVar.SERVERONLY);

    /// <summary>
    /// Countdown duration in minutes for automatically called emergency evac.
    /// </summary>
    public static readonly CVarDef<int> EvacAutoCallCountdownMinutes =
        CVarDef.Create("sandwich.shuttle.evac_auto_call_countdown_minutes", 15, CVar.SERVERONLY);

    /// <summary>
    /// Client-side jukebox volume multiplier.
    /// </summary>
    public static readonly CVarDef<float> JukeboxVolume =
        CVarDef.Create("sandwich.audio.jukebox_volume", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Base dB offset applied to all jukebox songs on playback.
    /// </summary>
    public static readonly CVarDef<float> JukeboxBaseVolume =
        CVarDef.Create("sandwich.audio.jukebox_base_volume", 0f, CVar.SERVERONLY,
            desc: "Base dB offset applied to all jukebox songs.");
}
