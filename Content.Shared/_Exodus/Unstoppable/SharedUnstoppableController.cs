using System.Numerics;
using Content.Shared.Conveyor;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;


namespace Content.Shared.Exodus.Unstoppable;

public abstract class SharedUnstoppableController : VirtualController
{
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private ValueList<EntityUid> _ents = new();
    private HashSet<Entity<ConveyorComponent>> _conveyors = new();

    public override void Initialize()
    {
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        UpdatesAfter.Add(typeof(SharedMoverController));

        base.Initialize();
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<UnstoppableComponent, TransformComponent, PhysicsComponent>();
        _ents.Clear();

        while (query.MoveNext(out var uid, out var comp, out var xform, out var physics))
        {
            if (GetRemainingDistance(uid, comp) <= comp.EndDistance /*||
                !IsTimeRemaining(uid, comp)*/)
            {
                _ents.Add(uid);
                continue;
            }

            if (!TryMove((uid, comp, physics, xform), prediction, frameTime))
            {
                _ents.Add(uid);
                continue;
            }
        }

        foreach (var ent in _ents)
        {
            RemComp<UnstoppableComponent>(ent);
        }
    }

    private bool TryMove(Entity<UnstoppableComponent, PhysicsComponent, TransformComponent> entity, bool prediction, float frameTime)
    {
        var unstop = entity.Comp1;
        var physics = entity.Comp2;
        var xform = entity.Comp3;

        var worldTargetPoint = TransformSystem.ToMapCoordinates(unstop.Coords);
        var worldBeginPoint = TransformSystem.GetMapCoordinates(xform);

        // Target and Ent are on different maps
        if (worldBeginPoint.MapId != worldTargetPoint.MapId)
        {
            Log.Error("Target is at the map {worldTargetPoint.MapId}, not at {worldBeginPoint.MapId}");
            return false;
        }

        // Client moment
        if (!physics.Predict && prediction)
            return true;

        if (physics.BodyType == BodyType.Static)
            return false;

        Vector2 direction;
        if (xform.GridUid == null)
            direction = worldTargetPoint.Position - worldBeginPoint.Position;
        else
        {
            var relXform = Transform(xform.GridUid.Value);
            direction = Vector2.Transform(worldTargetPoint.Position, relXform.InvLocalMatrix) -
                        Vector2.Transform(worldBeginPoint.Position, relXform.InvLocalMatrix);
        }

        var localPos = xform.LocalPosition;

        localPos += Move(direction.Normalized(), unstop.Speed, frameTime);

        TransformSystem.SetLocalPosition(entity, localPos, xform);

        // Force it awake for collisionwake reasons.
        Physics.SetAwake((entity, physics), true);
        Physics.SetSleepTime(physics, 0f);

        return true;
    }

    private static Vector2 Move(Vector2 direction, float speed, float frameTime)
    {
        if (speed == 0 || direction.Length() == 0)
            return Vector2.Zero;

        var velocity = direction * speed;
        return velocity * frameTime;
    }

    private void EndUnstoppableMove(EntityUid uid, UnstoppableComponent unstop)
    {
        RemCompDeferred<UnstoppableComponent>(uid);
    }

    private float GetRemainingDistance(EntityUid uid, UnstoppableComponent unstop)
    {
        return (_transform.GetWorldPosition(uid) - _transform.ToMapCoordinates(unstop.Coords).Position).Length();
    }

    private bool IsTimeRemaining(EntityUid uid, UnstoppableComponent unstop)
    {
        return unstop.EndTime > _timing.CurTime;
    }
}
