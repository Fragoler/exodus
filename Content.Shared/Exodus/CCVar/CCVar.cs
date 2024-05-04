using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.Exodus.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class CCVars
{
    /// <summary>
    /// Whether the Shipyard is enabled.
    /// </summary>
    public static readonly CVarDef<bool> Shipyard =
        CVarDef.Create("shuttle.shipyard", true, CVar.SERVERONLY);
}
