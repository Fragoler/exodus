using Content.Shared.Damage.Systems;
using Linguini.Bundle.Errors;

namespace Content.Server.Damage;

public sealed partial class StaminaSystem : SharedStaminaSystem
{
    protected override void UpdateHud(EntityUid uid)
    {
        var ev = new SyncStaminaEvent();
        RaiseNetworkEvent(ev, uid);
    }
}
