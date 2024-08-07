using Content.Server.Weapons.Ranged.Systems;
using Content.Server.NPC.HTN.Preconditions;
using Content.Server.NPC;
using Content.Server.Exodus.Fly;
using Content.Shared.Exodus.Fly;

namespace Content.Server.Exodus.NPC.HTN.Preconditions;

/// <summary>
/// Gets ammo for this NPC's selected gun; either active hand or itself.
/// </summary>
public sealed partial class FlyStatePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("currentState")]
    public FlyableEntityState CurrentState { get; set; } = FlyableEntityState.OnGroud;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var flySystem = _entManager.System<FlySystem>();

        return flySystem.IsFlyState(owner, CurrentState);
    }
}

