using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Exodus.Unstoppable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnstoppableComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityCoordinates Coords = new();

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Speed = 5.0f;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float EndDistance = 1.0f;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

}
