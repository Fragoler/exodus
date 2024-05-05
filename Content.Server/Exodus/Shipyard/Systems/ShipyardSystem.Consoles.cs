using Content.Server.Popups;
using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Exodus.Shipyard.Events;
using Content.Shared.Exodus.Shipyard.BUI;
using Content.Shared.Exodus.Shipyard.Prototypes;
using Content.Shared.Access.Systems;
using Content.Shared.Exodus.Shipyard.Components;
using Content.Shared.Exodus.Shipyard.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Exodus.Shipyard.Systems;

public sealed class ShipyardConsoleSystem : SharedShipyardSystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ShipyardSystem _shipyard = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public void InitializeConsole()
    {
        SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsolePurchaseMessage>(OnPurchaseMessage);
        SubscribeLocalEvent<ShipyardConsoleComponent, BoundUIOpenedEvent>(OnConsoleUIOpened);
    }

    private void OnPurchaseMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsolePurchaseMessage args)
    {
        if (args.Actor is not { Valid : true } player)
        {
            return;
        }

        if (!_access.IsAllowed(player, uid))
        {
            ConsolePopup(args.Actor, Loc.GetString("comms-console-permission-denied"));
            PlayDenySound(uid, component);
            return;
        }


        if (!_prototypeManager.TryIndex(args.Vessel, out VesselPrototype? vessel) || vessel is null)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
            PlayDenySound(uid, component);
            return;
        }

        if (!component.AllowedGroup.Contains(vessel.Group))
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
            PlayDenySound(uid, component);
            return;
        }

        var station = _station.GetOwningStation(uid);

        if (!GetBankAccount(station, out var bank)) return;
        if (vessel.Price <= 0) return;

        if (bank.Balance <= vessel.Price)
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryPurchaseVessel(station!.Value, vessel, out var _))
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
            PlayDenySound(uid, component);
            return;
        }

        var channel = _prototypeManager.Index<RadioChannelPrototype>("Command");
        _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking", ("vessel", vessel.Name.ToString())), channel, uid);

        _cargo.DeductFunds(bank, vessel.Price);
        PlayConfirmSound(uid, component);

        var newState = new ShipyardConsoleInterfaceState(
            bank.Balance,
            true,
            component.AllowedGroup);

        _ui.SetUiState(uid, ShipyardConsoleUiKey.Shipyard, newState);
    }

    private void OnConsoleUIOpened(EntityUid uid, ShipyardConsoleComponent component, BoundUIOpenedEvent args)
    {
        var station = _station.GetOwningStation(uid);

        if (!GetBankAccount(station, out var bank)) return;

        var newState = new ShipyardConsoleInterfaceState(
            bank.Balance,
            true,
            component.AllowedGroup);

        _ui.SetUiState(uid, ShipyardConsoleUiKey.Shipyard, newState);
    }

    private void ConsolePopup(EntityUid player, string text)
    {
        _popup.PopupEntity(text, player, player);
    }

    private void PlayDenySound(EntityUid uid, ShipyardConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
    }

    private void PlayConfirmSound(EntityUid uid, ShipyardConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ConfirmSound), uid);
    }

    private bool TryPurchaseVessel(EntityUid stationUid, VesselPrototype vessel, [NotNullWhen(true)] out ShuttleComponent? deed)
    {
        return _shipyard.TryPurchaseShuttle(stationUid, vessel, out deed);
    }

    public bool GetBankAccount(EntityUid? uid, [NotNullWhen(true)] out StationBankAccountComponent? bankAccount)
    {
        return TryComp(uid, out bankAccount);
    }
}
