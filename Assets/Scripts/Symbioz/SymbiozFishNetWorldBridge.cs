using System;
using System.Collections.Generic;
using System.IO;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using MahjongGame.Multiplayer;
using UnityEngine;

namespace Dynasty.Legacy.Symbioz
{
    public struct SymbiozWorldBuildBroadcast : IBroadcast
    {
        public int Location;
        public int X;
        public int Y;
        public int Kind;
        public string Note;
        public bool Removed;
    }

    public struct SymbiozWorldSnapshotRequestBroadcast : IBroadcast
    {
        public int Location;
    }

    public struct SymbiozWorldSnapshotBroadcast : IBroadcast
    {
        public int Location;
        public int Revision;
        public int[] X;
        public int[] Y;
        public int[] Kind;
        public string[] Note;
    }

    [DisallowMultipleComponent]
    public sealed class SymbiozFishNetWorldBridge : MonoBehaviour
    {
        private const int MaxObjectsPerSnapshot = 4096;
        private const int GridSize = 275;
        private const int LocationCount = 4;
        private const int SlumsLocation = 0;
        private const int SignKind = 2;
        private const int StoneQuarryKind = 10;
        private const int SawmillKind = 11;
        private const int TreeKind = 12;
        private const int SmallHouseKind = 13;
        private const int StoneTowerKind = 14;
        private const int StoneKeepKind = 15;
        private const int CastleKind = 16;
        private const int TreeVariantCount = 5;
        private const int TreeWoodYield = 3;
        private const int ServerTreeMinimumPerLocation = 45;
        private const int ServerVisibleTreeMinimumPerLocation = 16;
        private const int ServerVisibleTreeRadiusCells = 28;
        private const int ServerTreeEdgeMarginCells = 5;
        private const string ServerWorldFileName = "dynasty_legacy_symbioz_dedicated_world_v1.json";
        private const string LegacySharedWorldPath = "/opt/symbiosis/backend/downloads/world-state/dynasty-symbioz-world.json";
        private const float ServerSaveDebounceSeconds = 0.75f;

        private readonly Dictionary<int, ServerWorldObject> serverWorld = new Dictionary<int, ServerWorldObject>();
        private SymbiozFlagshipPrototype prototype;
        private NetworkManager networkManager;
        private bool registeredClient;
        private bool registeredServer;
        private bool serverWorldLoaded;
        private bool serverSavePending;
        private float nextServerSaveTime;
        private int revision;

        public void Initialize(SymbiozFlagshipPrototype owner)
        {
            prototype = owner;
        }

        private void Update()
        {
            ResolveNetworkManager();
            RegisterIfReady();
            FlushServerWorldSaveIfNeeded(false);
        }

        private void OnDestroy()
        {
            FlushServerWorldSaveIfNeeded(true);

            if (networkManager == null)
                return;

            if (registeredClient && networkManager.ClientManager != null)
            {
                networkManager.ClientManager.UnregisterBroadcast<SymbiozWorldBuildBroadcast>(OnClientWorldDelta);
                networkManager.ClientManager.UnregisterBroadcast<SymbiozWorldSnapshotBroadcast>(OnClientSnapshot);
            }

            if (registeredServer && networkManager.ServerManager != null)
            {
                networkManager.ServerManager.UnregisterBroadcast<SymbiozWorldBuildBroadcast>(OnServerBuildCommand);
                networkManager.ServerManager.UnregisterBroadcast<SymbiozWorldSnapshotRequestBroadcast>(OnServerSnapshotRequest);
            }
        }

        public bool SubmitBuildCommand(int location, int x, int y, int kind, string note)
        {
            SymbiozRuntimeLog.Write("NET", $"Submit build location={location} cell={x}:{y} kind={kind} note='{note}'");
            var message = new SymbiozWorldBuildBroadcast
            {
                Location = location,
                X = x,
                Y = y,
                Kind = kind,
                Note = note ?? string.Empty,
                Removed = false
            };

            return SendOrApplyServer(message);
        }

        public bool SubmitDeleteCommand(int location, int x, int y)
        {
            SymbiozRuntimeLog.Write("NET", $"Submit delete location={location} cell={x}:{y}");
            var message = new SymbiozWorldBuildBroadcast
            {
                Location = location,
                X = x,
                Y = y,
                Kind = 0,
                Note = string.Empty,
                Removed = true
            };

            return SendOrApplyServer(message);
        }

        private void ResolveNetworkManager()
        {
            if (networkManager != null)
                return;

            if (RealtimeNetworkBootstrap.I != null)
                networkManager = RealtimeNetworkBootstrap.I.NetworkManager;

            if (networkManager == null)
                networkManager = FindAnyObjectByType<NetworkManager>();
        }

        private void RegisterIfReady()
        {
            if (networkManager == null)
                return;

            if (!registeredClient && networkManager.ClientManager != null && networkManager.ClientManager.Started)
            {
                networkManager.ClientManager.RegisterBroadcast<SymbiozWorldBuildBroadcast>(OnClientWorldDelta);
                networkManager.ClientManager.RegisterBroadcast<SymbiozWorldSnapshotBroadcast>(OnClientSnapshot);
                registeredClient = true;
                SymbiozRuntimeLog.Write("NET", "FishNet client broadcasts registered.");
                for (int location = 0; location < LocationCount; location++)
                    RequestSnapshot(location);
            }

            if (!registeredServer && networkManager.ServerManager != null && networkManager.ServerManager.Started)
            {
                networkManager.ServerManager.RegisterBroadcast<SymbiozWorldBuildBroadcast>(OnServerBuildCommand, false);
                networkManager.ServerManager.RegisterBroadcast<SymbiozWorldSnapshotRequestBroadcast>(OnServerSnapshotRequest, false);
                registeredServer = true;
                LoadServerWorld();
                EnsureServerSlumsResourceWorksites();
                EnsureServerMateriaTrees();
                SymbiozRuntimeLog.Write("NET", "FishNet server broadcasts registered.");
            }
        }

        private bool SendOrApplyServer(SymbiozWorldBuildBroadcast message)
        {
            ResolveNetworkManager();
            if (networkManager != null && networkManager.ClientManager != null && networkManager.ClientManager.Started)
            {
                SymbiozRuntimeLog.Write("NET", $"Broadcast to server removed={message.Removed} location={message.Location} cell={message.X}:{message.Y}");
                networkManager.ClientManager.Broadcast(message, Channel.Reliable);
                return true;
            }

            if (networkManager != null && networkManager.ServerManager != null && networkManager.ServerManager.Started)
            {
                SymbiozRuntimeLog.Write("NET", $"Apply local server mutation removed={message.Removed} location={message.Location} cell={message.X}:{message.Y}");
                ApplyServerMutation(message);
                BroadcastDelta(message);
                return true;
            }

            SymbiozRuntimeLog.Write("NET", $"No FishNet connection. Command rejected removed={message.Removed} location={message.Location} cell={message.X}:{message.Y}");
            return false;
        }

        private void RequestSnapshot(int location)
        {
            if (networkManager == null || networkManager.ClientManager == null || !networkManager.ClientManager.Started)
                return;

            networkManager.ClientManager.Broadcast(new SymbiozWorldSnapshotRequestBroadcast { Location = location }, Channel.Reliable);
        }

        private void OnServerBuildCommand(NetworkConnection connection, SymbiozWorldBuildBroadcast message)
        {
            if (!IsValidCell(message.X, message.Y))
                return;

            if (!message.Removed
                && !IsServerObjectFootprintFree(message.Location, new Vector2Int(message.X, message.Y), message.Kind, MakeKey(message.Location, message.X, message.Y)))
            {
                SymbiozRuntimeLog.Write("NET", $"Server rejected overlapping build location={message.Location} cell={message.X}:{message.Y} kind={message.Kind}");
                return;
            }

            ApplyServerMutation(message);
            BroadcastDelta(message);
        }

        private void ApplyServerMutation(SymbiozWorldBuildBroadcast message)
        {
            int key = MakeKey(message.Location, message.X, message.Y);
            if (message.Removed)
                serverWorld.Remove(key);
            else
                serverWorld[key] = new ServerWorldObject(message.Location, message.X, message.Y, message.Kind, message.Note);

            revision++;
            ScheduleServerWorldSave();
        }

        private void BroadcastDelta(SymbiozWorldBuildBroadcast message)
        {
            if (networkManager == null || networkManager.ServerManager == null || !networkManager.ServerManager.Started)
                return;

            networkManager.ServerManager.Broadcast(message, false, Channel.Reliable);
        }

        private void OnServerSnapshotRequest(NetworkConnection connection, SymbiozWorldSnapshotRequestBroadcast request)
        {
            if (connection == null || networkManager == null || networkManager.ServerManager == null)
                return;

            EnsureServerSlumsResourceWorksites();
            EnsureServerMateriaTreesForLocation(request.Location);
            List<ServerWorldObject> objects = new List<ServerWorldObject>();
            foreach (ServerWorldObject value in serverWorld.Values)
            {
                if (value.Location == request.Location)
                {
                    objects.Add(value);
                    if (objects.Count >= MaxObjectsPerSnapshot)
                        break;
                }
            }

            var snapshot = new SymbiozWorldSnapshotBroadcast
            {
                Location = request.Location,
                Revision = revision,
                X = new int[objects.Count],
                Y = new int[objects.Count],
                Kind = new int[objects.Count],
                Note = new string[objects.Count]
            };

            for (int i = 0; i < objects.Count; i++)
            {
                snapshot.X[i] = objects[i].X;
                snapshot.Y[i] = objects[i].Y;
                snapshot.Kind[i] = objects[i].Kind;
                snapshot.Note[i] = objects[i].Note ?? string.Empty;
            }

            networkManager.ServerManager.Broadcast(connection, snapshot, false, Channel.Reliable);
        }

        private void LoadServerWorld()
        {
            if (serverWorldLoaded)
                return;

            serverWorldLoaded = true;
            string path = GetServerWorldPath();
            if (!File.Exists(path))
            {
                SymbiozRuntimeLog.Write("NET", "Dedicated world save not found. path=" + path);
                TryImportLegacySharedWorldIfBetter(0);
                return;
            }

            try
            {
                ServerWorldSave save = JsonUtility.FromJson<ServerWorldSave>(File.ReadAllText(path));
                if (save == null || save.Objects == null)
                    return;

                serverWorld.Clear();
                revision = Mathf.Max(0, save.Revision);
                for (int i = 0; i < save.Objects.Count; i++)
                {
                    ServerWorldObjectSave item = save.Objects[i];
                    if (item == null || !IsValidCell(item.X, item.Y))
                        continue;

                    serverWorld[MakeKey(item.Location, item.X, item.Y)] = new ServerWorldObject(
                        item.Location,
                        item.X,
                        item.Y,
                        item.Kind,
                        item.Note ?? string.Empty);
                }

                SymbiozRuntimeLog.Write("NET", $"Dedicated world loaded. objects={serverWorld.Count} revision={revision} path={path}");
                TryImportLegacySharedWorldIfBetter(serverWorld.Count);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load dedicated Symbioz world: {exception.Message}");
                SymbiozRuntimeLog.Write("NET", "Dedicated world load failed: " + exception.Message);
                TryImportLegacySharedWorldIfBetter(0);
            }
        }

        private void TryImportLegacySharedWorldIfBetter(int currentObjectCount)
        {
            if (!File.Exists(LegacySharedWorldPath))
                return;

            try
            {
                LegacyWorldSave legacy = JsonUtility.FromJson<LegacyWorldSave>(File.ReadAllText(LegacySharedWorldPath));
                int legacyCount = CountLegacyObjects(legacy);
                if (legacyCount <= currentObjectCount)
                {
                    SymbiozRuntimeLog.Write("NET", $"Legacy shared world import skipped. legacy={legacyCount} dedicated={currentObjectCount}");
                    return;
                }

                serverWorld.Clear();
                if (legacy.locations != null)
                {
                    for (int i = 0; i < legacy.locations.Count; i++)
                    {
                        LegacyLocationSave location = legacy.locations[i];
                        if (location == null || location.objects == null || !TryParseLocation(location.id, out int locationValue))
                            continue;

                        for (int j = 0; j < location.objects.Count; j++)
                        {
                            LegacyObjectSave item = location.objects[j];
                            if (item == null || !IsValidCell(item.x, item.y) || !TryParseKind(item.kind, out int kindValue))
                                continue;

                            serverWorld[MakeKey(locationValue, item.x, item.y)] = new ServerWorldObject(
                                locationValue,
                                item.x,
                                item.y,
                                kindValue,
                                item.note ?? string.Empty);
                        }
                    }
                }

                revision = Mathf.Max(revision + 1, serverWorld.Count);
                serverSavePending = true;
                nextServerSaveTime = 0f;
                SymbiozRuntimeLog.Write("NET", $"Legacy shared world imported. objects={serverWorld.Count} revision={revision} path={LegacySharedWorldPath}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not import legacy Symbioz world: {exception.Message}");
                SymbiozRuntimeLog.Write("NET", "Legacy shared world import failed: " + exception.Message);
            }
        }

        private void ScheduleServerWorldSave()
        {
            if (networkManager == null || networkManager.ServerManager == null || !networkManager.ServerManager.Started)
                return;

            serverSavePending = true;
            nextServerSaveTime = Time.unscaledTime + ServerSaveDebounceSeconds;
        }

        private void FlushServerWorldSaveIfNeeded(bool force)
        {
            if (!serverSavePending || (!force && Time.unscaledTime < nextServerSaveTime))
                return;

            if (networkManager == null || networkManager.ServerManager == null || !networkManager.ServerManager.Started)
                return;

            serverSavePending = false;
            string path = GetServerWorldPath();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var save = new ServerWorldSave
                {
                    Version = 1,
                    Revision = revision,
                    Objects = new List<ServerWorldObjectSave>(serverWorld.Count)
                };

                foreach (ServerWorldObject value in serverWorld.Values)
                {
                    save.Objects.Add(new ServerWorldObjectSave
                    {
                        Location = value.Location,
                        X = value.X,
                        Y = value.Y,
                        Kind = value.Kind,
                        Note = value.Note ?? string.Empty
                    });
                }

                File.WriteAllText(path, JsonUtility.ToJson(save, true));
                SymbiozRuntimeLog.Write("NET", $"Dedicated world saved. objects={save.Objects.Count} revision={revision} path={path}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not save dedicated Symbioz world: {exception.Message}");
                SymbiozRuntimeLog.Write("NET", "Dedicated world save failed: " + exception.Message);
            }
        }

        private static string GetServerWorldPath()
        {
            return Path.Combine(Application.persistentDataPath, ServerWorldFileName);
        }

        private void OnClientWorldDelta(SymbiozWorldBuildBroadcast message)
        {
            prototype?.ApplyServerWorldDelta(message.Location, message.X, message.Y, message.Kind, message.Note, message.Removed);
        }

        private void OnClientSnapshot(SymbiozWorldSnapshotBroadcast snapshot)
        {
            if (prototype == null || snapshot.X == null || snapshot.Y == null || snapshot.Kind == null)
                return;

            int count = Mathf.Min(snapshot.X.Length, Mathf.Min(snapshot.Y.Length, snapshot.Kind.Length));
            if (count == 0)
            {
                SymbiozRuntimeLog.Write("NET", $"Empty dedicated snapshot ignored. location={snapshot.Location} revision={snapshot.Revision}");
                prototype.EnsureLocationDefaultsFromServer(snapshot.Location);
                return;
            }

            SymbiozRuntimeLog.Write("NET", $"Dedicated snapshot applied. location={snapshot.Location} revision={snapshot.Revision} objects={count}");
            prototype.ClearLocationFromServer(snapshot.Location);
            for (int i = 0; i < count; i++)
            {
                string note = snapshot.Note != null && i < snapshot.Note.Length ? snapshot.Note[i] : string.Empty;
                prototype.ApplyServerWorldDelta(snapshot.Location, snapshot.X[i], snapshot.Y[i], snapshot.Kind[i], note, false);
            }
            prototype.EnsureLocationDefaultsFromServer(snapshot.Location);
        }

        private static bool IsValidCell(int x, int y)
        {
            return x >= 0 && x < 275 && y >= 0 && y < 275;
        }

        private static int MakeKey(int location, int x, int y)
        {
            return (location << 24) ^ (x << 12) ^ y;
        }

        private static int CountLegacyObjects(LegacyWorldSave legacy)
        {
            if (legacy == null || legacy.locations == null)
                return 0;

            int count = 0;
            for (int i = 0; i < legacy.locations.Count; i++)
            {
                LegacyLocationSave location = legacy.locations[i];
                if (location?.objects != null)
                    count += location.objects.Count;
            }

            return count;
        }

        private static bool TryParseLocation(string value, out int location)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "FirstSoil":
                    location = 0;
                    return true;
                case "ReturnYard":
                    location = 1;
                    return true;
                case "CityGate":
                    location = 2;
                    return true;
                case "CityCenter":
                    location = 3;
                    return true;
                default:
                    location = 0;
                    return false;
            }
        }

        private static bool TryParseKind(string value, out int kind)
        {
            switch ((value ?? string.Empty).Trim())
            {
                case "Wall":
                    kind = 0;
                    return true;
                case "Road":
                    kind = 1;
                    return true;
                case "Sign":
                    kind = 2;
                    return true;
                case "Repair":
                    kind = 3;
                    return true;
                case "BuildPlot":
                    kind = 4;
                    return true;
                case "SleepingBag":
                    kind = 5;
                    return true;
                case "Tent":
                    kind = 6;
                    return true;
                case "BerryBush":
                    kind = 7;
                    return true;
                case "CommunityStorage":
                    kind = 8;
                    return true;
                case "SlumTradeCenter":
                    kind = 9;
                    return true;
                case "StoneQuarry":
                    kind = 10;
                    return true;
                case "Sawmill":
                    kind = 11;
                    return true;
                case "Tree":
                    kind = TreeKind;
                    return true;
                case "SmallHouse":
                    kind = SmallHouseKind;
                    return true;
                case "StoneTower":
                    kind = StoneTowerKind;
                    return true;
                case "StoneKeep":
                    kind = StoneKeepKind;
                    return true;
                case "Castle":
                    kind = CastleKind;
                    return true;
                default:
                    kind = 0;
                    return false;
            }
        }

        private void EnsureServerSlumsResourceWorksites()
        {
            int entranceY = GridSize - 14;
            int spawned = 0;
            spawned += EnsureServerDefaultObjectNear(
                SlumsLocation,
                StoneQuarryKind,
                new Vector2Int(GridSize / 2 - 18, entranceY),
                "Slums visible stone quarry: mine stone.",
                searchLeft: true);
            spawned += EnsureServerDefaultObjectNear(
                SlumsLocation,
                SawmillKind,
                new Vector2Int(GridSize / 2 + 12, entranceY),
                "Slums visible sawmill: gather wood.",
                searchLeft: false);
            spawned += EnsureServerDefaultObjectNear(
                SlumsLocation,
                SignKind,
                new Vector2Int(GridSize / 2 - 18, entranceY - 9),
                "Stone quarry: use to mine stone.",
                searchLeft: true);
            spawned += EnsureServerDefaultObjectNear(
                SlumsLocation,
                SignKind,
                new Vector2Int(GridSize / 2 + 12, entranceY - 9),
                "Sawmill: use to gather wood.",
                searchLeft: false);

            if (spawned <= 0)
                return;

            revision++;
            ScheduleServerWorldSave();
            SymbiozRuntimeLog.Write("NET", $"Dedicated slums resource worksites seeded. spawned={spawned}");
        }

        private int EnsureServerDefaultObjectNear(int location, int kind, Vector2Int preferredCell, string note, bool searchLeft)
        {
            if (HasServerObjectKind(location, kind, note))
                return 0;

            if (!TryFindServerDefaultObjectCell(location, preferredCell, kind, searchLeft, out Vector2Int cell))
                return 0;

            serverWorld[MakeKey(location, cell.x, cell.y)] = new ServerWorldObject(location, cell.x, cell.y, kind, note ?? string.Empty);
            return 1;
        }

        private bool HasServerObjectKind(int location, int kind, string note)
        {
            foreach (ServerWorldObject value in serverWorld.Values)
            {
                if (value.Location != location || value.Kind != kind)
                    continue;

                bool exactNoteDefault = kind == SignKind || kind == StoneQuarryKind || kind == SawmillKind;
                if (!exactNoteDefault || string.Equals(value.Note ?? string.Empty, note ?? string.Empty, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private bool TryFindServerDefaultObjectCell(int location, Vector2Int preferredCell, int kind, bool searchLeft, out Vector2Int cell)
        {
            preferredCell = ClampServerCell(preferredCell);
            if (IsServerObjectFootprintFree(location, preferredCell, kind))
            {
                cell = preferredCell;
                return true;
            }

            int direction = searchLeft ? -1 : 1;
            for (int radius = 1; radius <= 36; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int candidate = ClampServerCell(preferredCell + new Vector2Int(direction * radius, y));
                    if (IsServerObjectFootprintFree(location, candidate, kind))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }

            cell = Vector2Int.zero;
            return false;
        }

        private bool IsServerObjectFootprintFree(int location, Vector2Int anchor, int kind, int ignoredKey = int.MinValue)
        {
            Vector2Int size = GetServerFootprint(kind);
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int cell = new Vector2Int(anchor.x + x, anchor.y + y);
                    if (!IsValidCell(cell.x, cell.y) || IsServerPortalReservedCell(location, cell))
                        return false;

                    foreach (ServerWorldObject value in serverWorld.Values)
                    {
                        if (MakeKey(value.Location, value.X, value.Y) == ignoredKey)
                            continue;

                        if (value.Location == location && CellInsideServerFootprint(cell, new Vector2Int(value.X, value.Y), value.Kind))
                            return false;
                    }
                }
            }

            return true;
        }

        private void EnsureServerMateriaTrees()
        {
            for (int location = 0; location < LocationCount; location++)
                EnsureServerMateriaTreesForLocation(location);
        }

        private void EnsureServerMateriaTreesForLocation(int location)
        {
            if (location < 0 || location >= LocationCount)
                return;

            int removed = CleanupInvalidServerTrees(location);
            int spawned = 0;
            Vector2Int spawnCell = GetServerSpawnCell(location);
            int visible = CountServerTreesNear(location, spawnCell, ServerVisibleTreeRadiusCells);
            while (visible + spawned < ServerVisibleTreeMinimumPerLocation
                && TryFindServerTreeCellNear(location, spawnCell, ServerVisibleTreeRadiusCells, spawned, out Vector2Int visibleCell))
            {
                AddServerTree(location, visibleCell, spawned);
                spawned++;
            }

            int total = CountServerTrees(location);
            while (total + spawned < ServerTreeMinimumPerLocation
                && TryFindServerTreeCell(location, total + spawned, out Vector2Int cell))
            {
                AddServerTree(location, cell, total + spawned);
                spawned++;
            }

            if (spawned <= 0 && removed <= 0)
                return;

            revision++;
            ScheduleServerWorldSave();
            SymbiozRuntimeLog.Write("NET", $"Dedicated materia trees seeded. location={location} spawned={spawned} removedInvalidTrees={removed} total={CountServerTrees(location)} visible={CountServerTreesNear(location, spawnCell, ServerVisibleTreeRadiusCells)}");
        }

        private int CleanupInvalidServerTrees(int location)
        {
            List<int> removeKeys = null;
            foreach (KeyValuePair<int, ServerWorldObject> pair in serverWorld)
            {
                ServerWorldObject value = pair.Value;
                if (value.Location != location || value.Kind != TreeKind)
                    continue;

                if (IsServerTreeSpawnCellInsideBounds(new Vector2Int(value.X, value.Y)))
                    continue;

                removeKeys ??= new List<int>();
                removeKeys.Add(pair.Key);
            }

            if (removeKeys == null)
                return 0;

            for (int i = 0; i < removeKeys.Count; i++)
                serverWorld.Remove(removeKeys[i]);

            return removeKeys.Count;
        }

        private void AddServerTree(int location, Vector2Int cell, int seed)
        {
            int variant = Mathf.Abs((location * 37 + cell.x * 11 + cell.y * 17 + seed) % TreeVariantCount);
            string note = $"tree={variant};wood={TreeWoodYield}";
            serverWorld[MakeKey(location, cell.x, cell.y)] = new ServerWorldObject(location, cell.x, cell.y, TreeKind, note);
        }

        private int CountServerTrees(int location)
        {
            int count = 0;
            foreach (ServerWorldObject value in serverWorld.Values)
            {
                if (value.Location == location
                    && value.Kind == TreeKind
                    && ParseServerTreeWood(value.Note) > 0
                    && IsServerTreeSpawnCellInsideBounds(new Vector2Int(value.X, value.Y)))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountServerTreesNear(int location, Vector2Int origin, int radiusCells)
        {
            int count = 0;
            int sqrRadius = radiusCells * radiusCells;
            foreach (ServerWorldObject value in serverWorld.Values)
            {
                if (value.Location != location || value.Kind != TreeKind || ParseServerTreeWood(value.Note) <= 0)
                    continue;

                if (!IsServerTreeSpawnCellInsideBounds(new Vector2Int(value.X, value.Y)))
                    continue;

                Vector2Int delta = new Vector2Int(value.X, value.Y) - origin;
                if (delta.sqrMagnitude <= sqrRadius)
                    count++;
            }

            return count;
        }

        private static int ParseServerTreeWood(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return TreeWoodYield;

            string[] parts = note.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string[] pair = parts[i].Split('=');
                if (pair.Length == 2
                    && string.Equals(pair[0].Trim(), "wood", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(pair[1].Trim(), out int wood))
                {
                    return Mathf.Max(0, wood);
                }
            }

            return TreeWoodYield;
        }

        private bool TryFindServerTreeCell(int location, int seed, out Vector2Int cell)
        {
            int startX = Mathf.Abs(53 + location * 41 + seed * 17) % GridSize;
            int startY = Mathf.Abs(97 + location * 29 + seed * 31) % GridSize;
            for (int attempt = 0; attempt < GridSize; attempt++)
            {
                int x = (startX + attempt * 37) % GridSize;
                int y = (startY + attempt * 61) % GridSize;
                cell = new Vector2Int(x, y);
                if (IsServerTreeCellFree(location, cell))
                    return true;
            }

            cell = Vector2Int.zero;
            return false;
        }

        private bool TryFindServerTreeCellNear(int location, Vector2Int origin, int radiusCells, int seed, out Vector2Int cell)
        {
            for (int attempt = 0; attempt < 360; attempt++)
            {
                int ring = 5 + ((attempt + seed) % Mathf.Max(1, radiusCells - 4));
                int side = attempt % 4;
                int offset = ((attempt * 7 + seed * 3) % (ring * 2 + 1)) - ring;
                int x = origin.x;
                int y = origin.y;
                switch (side)
                {
                    case 0:
                        x += offset;
                        y += ring;
                        break;
                    case 1:
                        x += ring;
                        y += offset;
                        break;
                    case 2:
                        x += offset;
                        y -= ring;
                        break;
                    default:
                        x -= ring;
                        y += offset;
                        break;
                }

                cell = new Vector2Int(x, y);
                if (!IsServerTreeSpawnCellInsideBounds(cell))
                    continue;

                if ((cell - origin).sqrMagnitude > radiusCells * radiusCells)
                    continue;

                if (IsServerTreeCellFree(location, cell))
                    return true;
            }

            cell = Vector2Int.zero;
            return false;
        }

        private bool IsServerTreeCellFree(int location, Vector2Int cell)
        {
            if (!IsServerTreeSpawnCellInsideBounds(cell) || IsServerPortalReservedCell(location, cell))
                return false;

            foreach (ServerWorldObject value in serverWorld.Values)
            {
                if (value.Location != location)
                    continue;

                if (CellInsideServerFootprint(cell, new Vector2Int(value.X, value.Y), value.Kind))
                    return false;
            }

            return true;
        }

        private static bool IsServerTreeSpawnCellInsideBounds(Vector2Int cell)
        {
            return cell.x >= ServerTreeEdgeMarginCells
                && cell.y >= ServerTreeEdgeMarginCells
                && cell.x < GridSize - ServerTreeEdgeMarginCells
                && cell.y < GridSize - ServerTreeEdgeMarginCells;
        }

        private static bool CellInsideServerFootprint(Vector2Int cell, Vector2Int anchor, int kind)
        {
            Vector2Int size = GetServerFootprint(kind);
            return cell.x >= anchor.x
                && cell.y >= anchor.y
                && cell.x < anchor.x + size.x
                && cell.y < anchor.y + size.y;
        }

        private static Vector2Int GetServerFootprint(int kind)
        {
            switch (kind)
            {
                case 8:
                case 9:
                    return new Vector2Int(6, 4);
                case 10:
                case 11:
                    return new Vector2Int(7, 7);
                case SmallHouseKind:
                    return new Vector2Int(7, 7);
                case StoneTowerKind:
                    return new Vector2Int(9, 9);
                case StoneKeepKind:
                    return new Vector2Int(14, 14);
                case CastleKind:
                    return new Vector2Int(19, 19);
                default:
                    return Vector2Int.one;
            }
        }

        private static bool IsServerPortalReservedCell(int location, Vector2Int cell)
        {
            Vector2Int north = new Vector2Int(GridSize / 2, GridSize - 1);
            Vector2Int south = new Vector2Int(GridSize / 2, 0);
            if (location == 2 && IsNearDoor(cell, south))
                return true;
            if ((location == 0 || location == 1 || location == 2) && IsNearDoor(cell, north))
                return true;
            if (location == 3 && IsNearDoor(cell, south))
                return true;
            return false;
        }

        private static bool IsNearDoor(Vector2Int cell, Vector2Int center)
        {
            return Mathf.Abs(cell.x - center.x) <= 2 && Mathf.Abs(cell.y - center.y) <= 1;
        }

        private static Vector2Int GetServerSpawnCell(int location)
        {
            Vector2Int north = new Vector2Int(GridSize / 2, GridSize - 1);
            Vector2Int south = new Vector2Int(GridSize / 2, 0);
            return location == 0 || location == 1
                ? ClampServerCell(north + Vector2Int.down * 18)
                : ClampServerCell(south + Vector2Int.up * 18);
        }

        private static Vector2Int ClampServerCell(Vector2Int cell)
        {
            return new Vector2Int(
                Mathf.Clamp(cell.x, 0, GridSize - 1),
                Mathf.Clamp(cell.y, 0, GridSize - 1));
        }

        private readonly struct ServerWorldObject
        {
            public readonly int Location;
            public readonly int X;
            public readonly int Y;
            public readonly int Kind;
            public readonly string Note;

            public ServerWorldObject(int location, int x, int y, int kind, string note)
            {
                Location = location;
                X = x;
                Y = y;
                Kind = kind;
                Note = note;
            }
        }

        [Serializable]
        private sealed class ServerWorldSave
        {
            public int Version;
            public int Revision;
            public List<ServerWorldObjectSave> Objects = new List<ServerWorldObjectSave>();
        }

        [Serializable]
        private sealed class ServerWorldObjectSave
        {
            public int Location;
            public int X;
            public int Y;
            public int Kind;
            public string Note;
        }

        [Serializable]
        private sealed class LegacyWorldSave
        {
            public int version;
            public string currentLocation;
            public List<LegacyLocationSave> locations;
        }

        [Serializable]
        private sealed class LegacyLocationSave
        {
            public string id;
            public List<LegacyObjectSave> objects;
        }

        [Serializable]
        private sealed class LegacyObjectSave
        {
            public int x;
            public int y;
            public string kind;
            public string note;
        }
    }
}
