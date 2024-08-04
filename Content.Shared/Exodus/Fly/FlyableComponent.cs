using Content.Shared.Fluids.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Exodus.Fly;

/// <summary>
/// Marks an entity as able to flying
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FlyableComponent : Component
{

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float LandAngle = (float)-Math.PI / 8;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float TakeoffAngle = (float)Math.PI / 8;


    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float LandTime = 0.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float TakeoffTime = 0.5f;

    [DataField("groundEffectEntity")]
    public string? GroundEffectEntity = null;


    [ViewVariables(VVAccess.ReadWrite), DataField("soundTakeoff")]
    public SoundSpecifier? SoundTakeoff = new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundLand")]
    public SoundSpecifier? SoundLand = new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg");


    [DataField]
    public bool InitInAir = false;


    [DataField]
    public bool InstantLand = false;

    [DataField]
    public bool InstantTakeoff = false;


    [DataField]
    public bool DespawnInAir = false;

    [DataField]
    public bool DespawnOnGround = false;



    [ViewVariables]
    public FlyableEntityState State;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AnimationTimeEnd = TimeSpan.Zero;

    /// <summary>
    /// Effect entity to delete upon removing the component. Only matters clientside.
    /// </summary>
    [ViewVariables]
    public EntityUid Effect = EntityUid.Invalid;
}

public enum FlyableEntityState
{
    OnGroud,
    InAir,
    TakingOff,
    Landing
}
