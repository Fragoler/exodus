using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared.Exodus.BattleDash
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class InputBattleComponent : Component
    {
        public BattleButtons HeldBattleButtons = BattleButtons.None;

        [ViewVariables(VVAccess.ReadWrite), DataField]
        public float DashDuration = 0.5f;
        public TimeSpan LastDashTime = TimeSpan.Zero;
    }

    [Serializable, NetSerializable]
    public sealed class InputBattleComponentState : ComponentState
    {
        public BattleButtons HeldBattleButtons;
        public float DashDuration;
        public bool ReleaseButtonSinceLastDash;
    }
}
