using Content.Server.Exodus.Unstoppable;
using Content.Shared.Exodus.BattleDash;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System.Numerics;


namespace Content.Server.Exodus.BattleDash;

public sealed partial class BattleDashController : SharedBattleController
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InputBattleComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<InputBattleComponent, PlayerDetachedEvent>(OnPlayerDetached);
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

        var battleQueryEnumerator = AllEntityQuery<InputBattleComponent>();
        while (battleQueryEnumerator.MoveNext(out var uid, out var battle))
        {
            HandleMobBattle((uid, battle));
        }
    }
}
