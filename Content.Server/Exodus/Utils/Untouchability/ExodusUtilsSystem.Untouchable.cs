using Content.Shared.Exodus.Utils.Untouchability;

namespace Content.Server.Exodus.Utils;

/// <summary>
/// A system that combines common methods from systems made by Exodus
/// And containing shortcuts for Space Wizards code
/// </summary>
public sealed partial class ExodusUtilsSystem
{
    public void MakeUntouchable(EntityUid uid)
    {
        if (IsUntouchable(uid))
            return;

        EnsureComp<UntouchableComponent>(uid);
    }

    public void MakeTouchable(EntityUid uid)
    {
        if (!IsUntouchable(uid))
            return;

        RemCompDeferred<UntouchableComponent>(uid);
    }

    public bool IsUntouchable(EntityUid uid)
    {
        return HasComp<UntouchableComponent>(uid);
    }
}
