using Content.Shared.Exodus.Unstoppable;
using Content.Shared.Input;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using System.Numerics;
using Robust.Shared.Timing;


namespace Content.Shared.Exodus.BattleDash;

public abstract partial class SharedBattleController : VirtualController
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedUnstoppableSystem _unstoppable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected EntityQuery<InputBattleComponent> BattleMoverQuery;

    public override void Initialize()
    {
        base.Initialize();

        BattleMoverQuery = GetEntityQuery<InputBattleComponent>();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.BattleDash, new BattleInputCmdHandler(this))
            .Register<SharedBattleController>();

        // SubscribeLocalEvent<BattleInputMoverComponent, ComponentInit>(OnInputInit);
        SubscribeLocalEvent<InputBattleComponent, ComponentGetState>(OnMoverGetState);
        SubscribeLocalEvent<InputBattleComponent, ComponentHandleState>(OnMoverHandleState);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownInput();
    }

    private void ShutdownInput()
    {
        CommandBinds.Unregister<SharedBattleController>();
    }


    private void OnMoverHandleState(Entity<InputBattleComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not InputBattleComponentState state)
            return;

        entity.Comp.DashDuration = state.DashDuration;

        if (entity.Comp.HeldBattleButtons != state.HeldBattleButtons)
        {
            entity.Comp.HeldBattleButtons = state.HeldBattleButtons;
            entity.Comp.DashDuration = state.DashDuration;
        }
    }

    private void OnMoverGetState(Entity<InputBattleComponent> entity, ref ComponentGetState args)
    {
        args.State = new InputBattleComponentState()
        {
            HeldBattleButtons = entity.Comp.HeldBattleButtons,
            DashDuration = entity.Comp.DashDuration,
        };

        HandleMobBattle(entity);
    }


    private void HandleBattleInput(EntityUid entity, ushort subTick, bool state)
    {
        if (!BattleMoverQuery.TryGetComponent(entity, out var moverComp))
            return;

        var battleButton = BattleButtons.BattleDash;
        SetBattleInput((entity, moverComp), subTick, state, battleButton);
    }

    private void SetBattleInput(Entity<InputBattleComponent> entity, ushort subTick, bool enabled, BattleButtons bit)
    {
        var buttons = entity.Comp.HeldBattleButtons;

        if (enabled)
            buttons |= bit;
        else
            buttons &= ~bit;

        SetBattleInput(entity, buttons);
    }

    protected void SetBattleInput(Entity<InputBattleComponent> entity, BattleButtons buttons)
    {
        if (entity.Comp.HeldBattleButtons == buttons)
            return;

        entity.Comp.HeldBattleButtons = buttons;
        Dirty(entity, entity.Comp);
    }

    private bool IsInBattleDash(InputBattleComponent comp)
    {
        return Timing.CurTime <= comp.LastDashTime + TimeSpan.FromSeconds(comp.DashDuration);
    }

    private sealed class BattleInputCmdHandler : InputCmdHandler
    {
        private readonly SharedBattleController _controller;
        private readonly Direction _dir;

        public BattleInputCmdHandler(SharedBattleController controller)
        {
            _controller = controller;
        }

        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            Logger.Info("HandleCmdMessage: " + message.State.ToString());
            if (session?.AttachedEntity == null) return false;

            _controller.HandleBattleInput(session.AttachedEntity.Value, message.SubTick, message.State == BoundKeyState.Down);
            return false;
        }
    }

    protected void HandleMobBattle(Entity<InputBattleComponent> entity)
    {
        if (!entity.Comp.HeldBattleButtons.HasFlag(BattleButtons.BattleDash) ||
            IsInBattleDash(entity.Comp))
            return;

        BattleDash(entity);
        entity.Comp.LastDashTime = Timing.CurTime;
    }

    private void BattleDash(Entity<InputBattleComponent> entity)
    {
        Logger.Info("Set Unstoppable");
        var coords = _transform.ToMapCoordinates(new EntityCoordinates(entity, new Vector2(3, 0)));
        _unstoppable.SetUnstoppableMove(entity, _transform.ToCoordinates(coords));
    }
}

[Flags]
[Serializable, NetSerializable]
public enum BattleButtons : byte
{
    None = 0,
    BattleDash = 1,
}
