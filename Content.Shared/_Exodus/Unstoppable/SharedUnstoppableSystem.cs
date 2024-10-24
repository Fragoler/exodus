using Content.Shared.Movement.Systems;
using Content.Shared.Actions;
using Robust.Shared.Map;
using Robust.Shared.Timing;


namespace Content.Shared.Exodus.Unstoppable;


public abstract partial class SharedUnstoppableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnstoppableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<UnstoppableComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<UnstoppableComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }


    public void SetUnstoppableMove(EntityUid uid, EntityCoordinates coords, float speed = 5.0f, float endDistance = 1.0f, float endInterval = 1.0f)
    {
        var unstop = EnsureComp<UnstoppableComponent>(uid);

        unstop.Coords = coords;
        unstop.Speed = speed;
        unstop.EndDistance = endDistance;

        unstop.EndTime = _timing.CurTime + TimeSpan.FromSeconds(endInterval);

        Dirty(uid, unstop);
    }

    public void EndUnstoppableMove(EntityUid uid)
    {
        RemCompDeferred<UnstoppableComponent>(uid);
    }


    private void OnInit(EntityUid uid, UnstoppableComponent component, ComponentInit args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }
    private void OnRemove(EntityUid uid, UnstoppableComponent component, ComponentRemove args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, UnstoppableComponent component, RefreshMovementSpeedModifiersEvent ev)
    {
        ev.ModifySpeed(0, 0);
    }
}
