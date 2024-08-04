using Content.Shared.Exodus.Utils.Invisibility;

namespace Content.Server.Exodus.Utils;

/// <summary>
/// A system that combines common methods from systems made by Exodus
/// And containing shortcuts for Space Wizards code
/// </summary>
public sealed partial class ExodusUtilsSystem
{
    public void MakeInvisible(EntityUid uid)
    {
        if (IsVisible(uid))
            EnsureComp<InvisibleComponent>(uid);
    }

    public void MakeVisible(EntityUid uid)
    {
        if (!IsVisible(uid))
            RemCompDeferred<InvisibleComponent>(uid);
    }

    public bool IsVisible(EntityUid uid)
    {
        return !HasComp<InvisibleComponent>(uid);
    }

}
