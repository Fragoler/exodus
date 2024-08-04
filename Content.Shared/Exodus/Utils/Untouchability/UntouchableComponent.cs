using Robust.Shared.GameStates;

namespace Content.Shared.Exodus.Utils.Untouchability;

/// <summary>
/// Component which indicates is entity incorporeal
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UntouchableComponent : Component
{
    [DataField("collisionMaskBefore"), ViewVariables(VVAccess.ReadOnly)]
    public int CollisionMaskBefore;

    [DataField("collisionLayerBefore"), ViewVariables(VVAccess.ReadOnly)]
    public int CollisionLayerBefore;
}
