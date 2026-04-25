using Content.Shared.Singularity.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Visualizers;

public sealed class RadiationCollectorSystem : VisualizerSystem<RadiationCollectorComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationCollectorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RadiationCollectorComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnComponentInit(EntityUid uid, RadiationCollectorComponent comp, ComponentInit args)
    {
        comp.ActivateAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(0.8f),
            AnimationTracks = {
                new AnimationTrackSpriteFlick() {
                    LayerKey = RadiationCollectorVisualLayers.Main,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(comp.ActivatingState, 0f)}
                },
            }
        };

        comp.DeactiveAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(0.8f),
            AnimationTracks = {
                new AnimationTrackSpriteFlick() {
                    LayerKey = RadiationCollectorVisualLayers.Main,
                    KeyFrames = {new AnimationTrackSpriteFlick.KeyFrame(comp.DeactivatingState, 0f)}
                },
            }
        };
    }

    private void UpdateVisuals(EntityUid uid, RadiationCollectorVisualState state, RadiationCollectorComponent comp, SpriteComponent sprite, AnimationPlayerComponent? animPlayer = null)
    {
        if (state == comp.CurrentState)
            return;

        TryComp(uid, out animPlayer);
        if (animPlayer != null && AnimationSystem.HasRunningAnimation(uid, animPlayer, RadiationCollectorComponent.AnimationKey))
            return;

        var targetState = state & RadiationCollectorVisualState.Active;
        var destinationState = comp.CurrentState & RadiationCollectorVisualState.Active;
        if (targetState != destinationState)
            targetState |= RadiationCollectorVisualState.Deactivating;

        comp.CurrentState = state;

        switch (targetState)
        {
            case RadiationCollectorVisualState.Activating:
                if (animPlayer != null)
                    AnimationSystem.Play((uid, animPlayer), comp.ActivateAnimation, RadiationCollectorComponent.AnimationKey);
                else
                    SpriteSystem.LayerSetRsiState((uid, sprite), RadiationCollectorVisualLayers.Main, comp.ActiveState);
                break;
            case RadiationCollectorVisualState.Deactivating:
                if (animPlayer != null)
                    AnimationSystem.Play((uid, animPlayer), comp.DeactiveAnimation, RadiationCollectorComponent.AnimationKey);
                else
                    SpriteSystem.LayerSetRsiState((uid, sprite), RadiationCollectorVisualLayers.Main, comp.InactiveState);
                break;

            case RadiationCollectorVisualState.Active:
                SpriteSystem.LayerSetRsiState((uid, sprite), RadiationCollectorVisualLayers.Main, comp.ActiveState);
                break;
            case RadiationCollectorVisualState.Deactive:
                SpriteSystem.LayerSetRsiState((uid, sprite), RadiationCollectorVisualLayers.Main, comp.InactiveState);
                break;
        }
    }

    private void OnAnimationCompleted(EntityUid uid, RadiationCollectorComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != RadiationCollectorComponent.AnimationKey)
            return;
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        if (!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state))
            state = comp.CurrentState;

        var targetState = state & RadiationCollectorVisualState.Active;

        UpdateVisuals(uid, targetState, comp, sprite, animPlayer);
    }

    protected override void OnAppearanceChange(EntityUid uid, RadiationCollectorComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<RadiationCollectorVisualState>(uid, RadiationCollectorVisuals.VisualState, out var state, args.Component))
            state = RadiationCollectorVisualState.Deactive;

        TryComp<AnimationPlayerComponent>(uid, out var animPlayer);
        UpdateVisuals(uid, state, comp, args.Sprite, animPlayer);
    }
}

public enum RadiationCollectorVisualLayers : byte
{
    Main
}
