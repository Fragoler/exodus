using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Exodus.Fly;

/// <summary>
/// Provides extraction devices that teleports the attached entity after <see cref="FultonDuration"/> elapses to the linked beacon.
/// </summary>
public abstract partial class SharedFlySystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;


    public bool IsFlyState(EntityUid uid, FlyableEntityState curState = FlyableEntityState.InAir, FlyableComponent? comp = null)
    {
        return curState == GetFlyState(uid, comp);
    }

    public FlyableEntityState GetFlyState(EntityUid uid, FlyableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return FlyableEntityState.OnGroud;

        return comp.State;
    }

    protected abstract class FlyEvents : EntityEventArgs
    {
        public NetEntity Entity;
    }

    [Serializable, NetSerializable]
    protected sealed class LandAnimationMessage : FlyEvents
    {
    }

    [Serializable, NetSerializable]
    protected sealed class TakeoffAnimationMessage : FlyEvents
    {
    }


    [Serializable, NetSerializable]
    protected sealed class MovedToGroundMessage : FlyEvents
    {
    }
    [Serializable, NetSerializable]
    protected sealed class MovedToAirMessage : FlyEvents
    {
    }


    [Serializable, NetSerializable]
    protected sealed class MovedFromGroundMessage : FlyEvents
    {
    }
    [Serializable, NetSerializable]
    protected sealed class MovedFromAirMessage : FlyEvents
    {
    }
}
