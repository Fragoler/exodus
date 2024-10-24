using Content.Shared.Exodus.BattleDash;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Player;


namespace Content.Client.Exodus.BattleDash;

public sealed partial class BattleDashController : SharedBattleController
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InputBattleComponent, UpdateIsPredictedEvent>(OnUpdatePredicted);

        SubscribeLocalEvent<InputBattleComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<InputBattleComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnUpdatePredicted(Entity<InputBattleComponent> entity, ref UpdateIsPredictedEvent args)
    {
        // Enable prediction if an entity is controlled by the player
        if (entity.Owner == _playerManager.LocalEntity)
            args.IsPredicted = true;
    }

    private void OnPlayerAttached(Entity<InputBattleComponent> entity, ref PlayerAttachedEvent args)
    {
        SetBattleInput(entity, BattleButtons.None);
    }

    private void OnPlayerDetached(Entity<InputBattleComponent> entity, ref PlayerDetachedEvent args)
    {
        SetBattleInput(entity, BattleButtons.None);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        if (_playerManager.LocalEntity is not { Valid: true } player)
            return;

        if (!BattleMoverQuery.TryComp(player, out var battle))
            return;

        HandleMobBattle((player, battle));
    }
}
