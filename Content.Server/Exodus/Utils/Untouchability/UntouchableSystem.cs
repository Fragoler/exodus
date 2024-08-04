using System.Linq;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Exodus.Utils.Untouchability;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.Stories.Lib.Incorporeal;

public sealed class UntouchableSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UntouchableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<UntouchableComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, UntouchableComponent component, ref ComponentStartup args)
    {
        var fixtures = Comp<FixturesComponent>(uid);
        var fixture = fixtures.Fixtures.First();
        component.CollisionLayerBefore = fixture.Value.CollisionLayer;
        component.CollisionMaskBefore = fixture.Value.CollisionMask;

        _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int)CollisionGroup.None, fixtures);
        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int)CollisionGroup.None, fixtures);

        Dirty(uid, component);
    }

    private void OnShutdown(EntityUid uid, UntouchableComponent component, ref ComponentShutdown args)
    {
        var fixtures = Comp<FixturesComponent>(uid);
        var fixture = fixtures.Fixtures.First();

        _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, component.CollisionLayerBefore, fixtures);
        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, component.CollisionMaskBefore, fixtures);
    }
}
