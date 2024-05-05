using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Exodus.Shipyard.Systems;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Shared.Exodus.CCVar;
using Robust.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Exodus.Shipyard.Components;
using Content.Shared.Exodus.Shipyard.Prototypes;
using Robust.Shared.Map.Components;

namespace Content.Server.Exodus.Shipyard.Systems;

public sealed partial class ShipyardSystem : SharedShipyardSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly ShipyardConsoleSystem _shipyardConsole = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public MapId? _shipyardMap { get; private set; }
    private float _shuttleIndex;
    private const float ShuttleSpawnBuffer = 1f;
    private ISawmill _sawmill = default!;
    private bool _enabled;

    public override void Initialize()
    {
        _enabled = _configManager.GetCVar(CCVars.Shipyard);
        _configManager.OnValueChanged(CCVars.Shipyard, SetShipyardEnabled);
        _sawmill = Logger.GetSawmill("shipyard");
        _shipyardConsole.InitializeConsole();

        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentInit>(OnShipyardStartup);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnShipyardStartup(EntityUid uid, ShipyardConsoleComponent component, ComponentInit args)
    {
        if (!_enabled)
            return;

        SetupShipyard();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _configManager.UnsubValueChanged(CCVars.Shipyard, SetShipyardEnabled);
        CleanupShipyard();
    }

    private void SetShipyardEnabled(bool value)
    {
        if (_enabled == value)
            return;

        _enabled = value;

        if (value)
        {
            SetupShipyard();
        }
        else
        {
            CleanupShipyard();
        }
    }

    /// <summary>
    /// Adds a ship to the shipyard, calculates its price, and attempts to ftl-dock it to the given station
    /// </summary>
    /// <param name="stationUid">The ID of the station to dock the shuttle to</param>
    /// <param name="vessel">The proto shuttle's vessel that will be buy</param>
    /// <returns>Return if the shuttle has been called</returns>
    public bool TryPurchaseShuttle(EntityUid stationUid, VesselPrototype vessel, [NotNullWhen(true)] out ShuttleComponent? shuttle)
    {
        shuttle = null;

        if (!TryComp<StationDataComponent>(stationUid, out var stationData))
            return false;

        var shuttlePath = vessel.ShuttlePath.ToString();
        var targetGrid = _station.GetLargestGrid(stationData);

        if (targetGrid is null ||
            !TryAddShuttle(shuttlePath, out var shuttleGrid) ||
            !TryComp(shuttleGrid, out shuttle))
            return false;

        _metaData.SetEntityName(shuttleGrid.Value, vessel.Name);

        _sawmill.Info($"Shuttle {shuttlePath} was purchased at {ToPrettyString(stationUid)} for {vessel.Price:f2}");
        _shuttle.FTLToDock(shuttleGrid.Value, shuttle, targetGrid.Value);
        return true;
    }

    /// <summary>
    /// Loads a shuttle into the ShipyardMap from a file path
    /// </summary>
    /// <param name="shuttlePath">The path to the grid file to load. Must be a grid file!</param>
    /// <param name="shuttleGrid">Returns the EntityUid of the shuttle</returns>
    private bool TryAddShuttle(string shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleGrid)
    {
        UpdateShipyard();

        shuttleGrid = null;
        if (!_enabled) return false;


        var loadOptions = new MapLoadOptions() { Offset = new Vector2(500f + _shuttleIndex, 1f) };

        if (!_map.TryLoad(_shipyardMap!.Value, shuttlePath, out var gridList, loadOptions) ||
            !TryComp<MapGridComponent>(gridList[0], out var mapGrid))
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
            return false;
        }

        _shuttleIndex += mapGrid.LocalAABB.Width + ShuttleSpawnBuffer;

        //only dealing with 1 grid at a time for now, until more is known about multi-grid drifting
        if (gridList.Count == 1)
        {
            shuttleGrid = gridList[0];
            return true;
        }

        if (gridList.Count < 1)
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, no grid found in file");

        if (gridList.Count > 1)
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, too many grids present in file");

            foreach (var grid in gridList)
            {
                _mapManager.DeleteGrid(grid);
            }
        }

        return false;
    }

    private void UpdateShipyard()
    {
        if (_enabled)
        {
            if (_shipyardMap is null || !_mapManager.MapExists(_shipyardMap.Value))
                SetupShipyard();
        }
        else
        {
            if (_shipyardMap is not null && _mapManager.MapExists(_shipyardMap.Value))
                CleanupShipyard();
        }
    }

    private void CleanupShipyard()
    {
        if (_shipyardMap == null || !_mapManager.MapExists(_shipyardMap.Value))
        {
            _shipyardMap = null;
            return;
        }

        _mapManager.DeleteMap(_shipyardMap.Value);
    }

    private void SetupShipyard()
    {
        if (_shipyardMap is not null &&
            _mapManager.MapExists(_shipyardMap.Value))
            return;

        _shipyardMap = _mapManager.CreateMap();
        _mapManager.SetMapPaused(_shipyardMap.Value, false);
    }
}
