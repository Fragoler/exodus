using Content.Server.Administration;
using Content.Server.Exodus.Utils;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Exodus.Fly;
using Content.Shared.Maps;
using Content.Shared.Movement.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;


namespace Content.Server.Exodus.Fly;

public sealed class FlySystem : SharedFlySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly ExodusUtilsSystem _utils = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlyableComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<FlyableComponent, FootstepsSoundAttemtEvent>(OnFootstepsSound);

        SubscribeLocalEvent<TriggerOnTakeoffComponent, MovedToAirMessage>(OnTakeoffTrigger);
        SubscribeLocalEvent<TriggerOnLandingComponent, MovedToGroundMessage>(OnLandingTrigger);

        SubscribeLocalEvent<FlyableComponent, MovedToAirMessage>(OnMovedToAirMessage);
        SubscribeLocalEvent<FlyableComponent, MovedToGroundMessage>(OnMovedToGroundMessage);
        SubscribeLocalEvent<FlyableComponent, MovedFromAirMessage>(OnMovedFromAirMessage);
        SubscribeLocalEvent<FlyableComponent, MovedFromGroundMessage>(OnMovedFromGroundMessage);

        _console.RegisterCommand("landentity", LandEntityCommand);
        _console.RegisterCommand("takeoffentity", TakeoffEntityCommand);
    }


    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FlyableComponent>();
        while (query.MoveNext(out var uid, out var flyComp))
        {
            if (flyComp.AnimationTimeEnd <= Timing.CurTime)
            {
                switch (flyComp.State)
                {
                    case FlyableEntityState.TakingOff:
                        var toAirEv = new MovedToAirMessage() { Entity = GetNetEntity(uid) };
                        RaiseLocalEvent(uid, toAirEv);
                        RaiseNetworkEvent(toAirEv);
                        flyComp.State = FlyableEntityState.InAir;
                        break;
                    case FlyableEntityState.Landing:
                        var toGroundEv = new MovedToGroundMessage() { Entity = GetNetEntity(uid) };
                        RaiseLocalEvent(uid, toGroundEv);
                        RaiseNetworkEvent(toGroundEv);
                        flyComp.State = FlyableEntityState.OnGroud;
                        break;
                    case FlyableEntityState.OnGroud:
                        if (flyComp.InstantTakeoff)
                            TryTakeoff(uid);
                        break;
                    case FlyableEntityState.InAir:
                        if (flyComp.InstantLand)
                            TryLand(uid);
                        break;
                    default:
                        break;
                }
            }
        }
    }


    #region Commands
    [AdminCommand(AdminFlags.Fun)]
    private void LandEntityCommand(IConsoleShell shell, string argstr, string[] args)
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

        TryLand(uid.Value);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void TakeoffEntityCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-takeoffentity-invalid"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
        {
            shell.WriteLine($"No entity found with netUid {uidNet}");
            return;
        }

        TryTakeoff(uid.Value);
    }
    #endregion


    #region Events
    private void OnMapInit(EntityUid uid, FlyableComponent component, MapInitEvent ev)
    {
        if (component.InitInAir)
        {
            component.State = FlyableEntityState.InAir;

            if (SpawnGroundEffect(uid, ref component.Effect))
                _transform.SetParent(component.Effect, uid);

            _utils.MakeInvisible(uid);
            _utils.MakeUntouchable(uid);
        }
    }

    private void OnFootstepsSound(EntityUid uid, FlyableComponent component, FootstepsSoundAttemtEvent ev)
    {
        if (component.State == FlyableEntityState.OnGroud)
            return;

        ev.Cancel();
    }

    private void OnTakeoffTrigger(EntityUid uid, TriggerOnTakeoffComponent component, MovedToAirMessage ev)
    {
        _trigger.Trigger(uid);
    }

    private void OnLandingTrigger(EntityUid uid, TriggerOnLandingComponent component, MovedToGroundMessage ev)
    {
        _trigger.Trigger(uid);
    }

    private void OnMovedFromAirMessage(EntityUid uid, FlyableComponent component, MovedFromAirMessage ev)
    {
    }

    private void OnMovedFromGroundMessage(EntityUid uid, FlyableComponent component, MovedFromGroundMessage ev)
    {
        if (SpawnGroundEffect(uid, ref component.Effect))
            _transform.SetParent(component.Effect, uid);

        _utils.MakeInvisible(uid);
        _utils.MakeUntouchable(uid);
    }

    private void OnMovedToAirMessage(EntityUid uid, FlyableComponent component, MovedToAirMessage ev)
    {
        if (component.DespawnInAir)
        {
            if (!Deleted(component.Effect))
                QueueDel(component.Effect);
            QueueDel(uid);
        }
    }

    private void OnMovedToGroundMessage(EntityUid uid, FlyableComponent component, MovedToGroundMessage ev)
    {
        if (!Deleted(component.Effect))
            QueueDel(component.Effect);

        if (component.DespawnOnGround)
            QueueDel(uid);

        _utils.MakeVisible(uid);
        _utils.MakeTouchable(uid);
    }
    #endregion


    #region Public
    public void TryTakeoff(EntityUid uid, FlyableComponent? component = null, bool doAnimation = true, bool playSound = true)
    {
        if (!Resolve(uid, ref component))
            return;

        if (CanTakeoff(uid, component))
            TakeOff(uid, component, doAnimation, playSound);
    }

    public void TryLand(EntityUid uid, FlyableComponent? component = null, bool doAnimation = true, bool playSound = true)
    {
        if (!Resolve(uid, ref component))
            return;

        if (CanLand(uid, component))
            Land(uid, component, doAnimation, playSound);
    }
    #endregion


    #region Private
    private void TakeOff(EntityUid uid, FlyableComponent component, bool doAnimation = true, bool playSound = true)
    {
        if (playSound)
            Audio.PlayPvs(component.SoundTakeoff, uid);

        var fromGroundEv = new MovedFromGroundMessage() { Entity = GetNetEntity(uid) };
        RaiseLocalEvent(uid, fromGroundEv);
        RaiseNetworkEvent(fromGroundEv);

        component.State = FlyableEntityState.TakingOff;

        if (!doAnimation)
        {
            component.AnimationTimeEnd = Timing.CurTime;
        }
        else
        {
            component.AnimationTimeEnd = Timing.CurTime + TimeSpan.FromSeconds(component.TakeoffTime);
            var takeOffAnimEv = new TakeoffAnimationMessage() { Entity = GetNetEntity(uid) };
            RaiseNetworkEvent(takeOffAnimEv);
        }
    }

    private void Land(EntityUid uid, FlyableComponent component, bool doAnimation = true, bool playSound = true)
    {
        if (playSound)
            Audio.PlayPvs(component.SoundTakeoff, uid);

        var fromAirEv = new MovedFromAirMessage() { Entity = GetNetEntity(uid) };
        RaiseLocalEvent(uid, fromAirEv);
        RaiseNetworkEvent(fromAirEv);

        component.State = FlyableEntityState.Landing;

        if (!doAnimation)
        {
            component.AnimationTimeEnd = Timing.CurTime;
        }
        else
        {
            component.AnimationTimeEnd = Timing.CurTime + TimeSpan.FromSeconds(component.TakeoffTime);
            var takeOffAnimEv = new LandAnimationMessage() { Entity = GetNetEntity(uid) };
            RaiseNetworkEvent(takeOffAnimEv);
        }
    }

    private bool CanTakeoff(EntityUid uid, FlyableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (Container.IsEntityInContainer(uid) ||
            comp.State != FlyableEntityState.OnGroud)
            return false;

        var xform = Transform(uid);

        if (xform.Anchored)
            return false;

        return true;
    }

    private bool CanLand(EntityUid uid, FlyableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) ||
            comp.State != FlyableEntityState.InAir)
            return false;

        var xform = Transform(uid);

        if (xform.GridUid != null &&
            TryComp<MapGridComponent>(xform.GridUid, out var grid) &&
            _map.TryGetTileRef(uid, grid, xform.Coordinates, out var tileRef))
        {
            if (tileRef.Tile.IsEmpty == true || _turf.IsTileBlocked(tileRef, Shared.Physics.CollisionGroup.Impassable))
            {
                Log.Error("Not free tile");
                return false;
            }
        }

        return true;
    }

    private bool SpawnGroundEffect(EntityUid entity, ref EntityUid groundEffect)
    {
        var xform = Transform(entity);

        if (Deleted(entity) ||
            !TryComp<FlyableComponent>(entity, out var flyComp) ||
            flyComp.GroundEffectEntity == null)
            return false;

        groundEffect = Spawn(flyComp.GroundEffectEntity, xform.Coordinates);
        return true;
    }
    #endregion
}
