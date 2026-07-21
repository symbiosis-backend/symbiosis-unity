using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dynasty.Legacy.Symbioz;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MahjongGame.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class RealtimeNetworkBootstrap : MonoBehaviour
    {
        private const string ConfigUrl = "https://dlsymbiosis.com/multiplayer/config";
        private const string HeadlessArg = "-fishnet-server";
        private const string HostArg = "-fishnet-host";
        private const string ClientArg = "-fishnet-client";
        private const string DirectSymbiozArg = "-symbioz-direct";
        private const string PlatformEntryArg = "-dls-platform-entry";
        private const string AddressArg = "-fishnet-address";
        private const string PortArg = "-fishnet-port";
        private const string MatrixPlayerResourcePath = "Network/MatrixNetworkPlayer";
        private const string SymbiozSceneName = "SymbiozFlagship";
        private const ulong RuntimeMatrixPlayerAssetPathHash = 0xD15F1A6B51D00001UL;

        public static RealtimeNetworkBootstrap I { get; private set; }
        private static FileStream dedicatedServerLock;
        private static string dedicatedServerLockPath;

        [Header("Defaults")]
        [SerializeField] private string defaultAddress = "91.99.176.77";
        [SerializeField] private ushort defaultPort = 7770;
        [SerializeField] private bool fetchServerConfigOnStartup = true;
        [SerializeField] private bool autoConnectClientOnStartup = false;
        [SerializeField] private int configTimeoutSeconds = 8;

        private NetworkManager networkManager;
        private NetworkObject matrixPlayerPrefab;
        private Tugboat tugboat;
        private string resolvedAddress;
        private ushort resolvedPort;
        private bool serverConnectionEventsRegistered;
        private bool serverStateEventsRegistered;
        private bool serverSceneEventsRegistered;
        private bool serverStartRequested;
        private bool clientStartRequested;
        private readonly Dictionary<int, NetworkObject> spawnedPlayersByConnection = new Dictionary<int, NetworkObject>();

        public NetworkManager NetworkManager => networkManager;
        public string Address => string.IsNullOrWhiteSpace(resolvedAddress) ? defaultAddress : resolvedAddress;
        public ushort Port => resolvedPort == 0 ? defaultPort : resolvedPort;
        public bool IsServerStarted => networkManager != null && networkManager.ServerManager != null && networkManager.ServerManager.Started;
        public bool IsClientStarted => networkManager != null && networkManager.ClientManager != null && networkManager.ClientManager.Started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!TryAcquireDedicatedServerLock(Environment.GetCommandLineArgs()))
                return;

            UnitySceneManager.sceneLoaded -= HandleSceneLoaded;
            UnitySceneManager.sceneLoaded += HandleSceneLoaded;
            TryBootstrapForScene(UnitySceneManager.GetActiveScene().name);
        }

        private static bool TryAcquireDedicatedServerLock(string[] args)
        {
            if (!HasArg(args, HeadlessArg))
                return true;

            if (dedicatedServerLock != null)
                return true;

            string lockPath = Path.Combine(Path.GetTempPath(), "symbiosis-fishnet-server.lock");
            try
            {
                AcquireDedicatedServerLockFile(lockPath);
                return true;
            }
            catch (IOException)
            {
                if (TryDeleteStaleDedicatedServerLock(lockPath))
                {
                    try
                    {
                        AcquireDedicatedServerLockFile(lockPath);
                        return true;
                    }
                    catch (IOException)
                    {
                        // Fall through to the active-process warning below.
                    }
                }
            }

            Debug.LogWarning($"[RealtimeNetworkBootstrap] Another dedicated server process already owns {lockPath}. This process will exit.");
            Application.Quit(0);
            return false;
        }

        private static void AcquireDedicatedServerLockFile(string lockPath)
        {
            dedicatedServerLock = new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            dedicatedServerLockPath = lockPath;
            string payloadText = $"pid={System.Diagnostics.Process.GetCurrentProcess().Id}\nutc={DateTime.UtcNow:O}\n";
            byte[] payload = Encoding.UTF8.GetBytes(payloadText);
            dedicatedServerLock.Write(payload, 0, payload.Length);
            dedicatedServerLock.Flush();
            Application.quitting -= ReleaseDedicatedServerLock;
            Application.quitting += ReleaseDedicatedServerLock;
            Debug.Log($"[RealtimeNetworkBootstrap] Dedicated server process lock acquired at {lockPath}");
        }

        private static bool TryDeleteStaleDedicatedServerLock(string lockPath)
        {
            try
            {
                if (!File.Exists(lockPath))
                    return false;

                string text = File.ReadAllText(lockPath);
                int pid = ParseLockPid(text);
                int currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
                bool stale = pid <= 0 || pid == currentPid || !IsProcessAlive(pid);
                if (!stale)
                    return false;

                File.Delete(lockPath);
                Debug.LogWarning($"[RealtimeNetworkBootstrap] Removed stale dedicated server lock at {lockPath}.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RealtimeNetworkBootstrap] Dedicated server stale lock check failed: " + ex.Message);
                return false;
            }
        }

        private static int ParseLockPid(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("pid=", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(line.Substring(4), out int pid))
                {
                    return pid;
                }
            }

            return 0;
        }

        private static bool IsProcessAlive(int pid)
        {
            try
            {
                System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(pid);
                return process != null && !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static void ReleaseDedicatedServerLock()
        {
            try
            {
                dedicatedServerLock?.Dispose();
                dedicatedServerLock = null;

                if (!string.IsNullOrWhiteSpace(dedicatedServerLockPath) && File.Exists(dedicatedServerLockPath))
                    File.Delete(dedicatedServerLockPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RealtimeNetworkBootstrap] Dedicated server process lock cleanup failed: " + ex.Message);
            }
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (ShouldRunNetworkForScene(scene.name))
            {
                TryBootstrapForScene(scene.name);
                return;
            }

            if (HasNetworkCommandLineArgs(Environment.GetCommandLineArgs()))
                return;

            if (I != null)
            {
                I.StopAll();
                Destroy(I.gameObject);
                I = null;
            }
        }

        private static void TryBootstrapForScene(string sceneName)
        {
            if (!ShouldRunNetworkForScene(sceneName) && !HasNetworkCommandLineArgs(Environment.GetCommandLineArgs()))
                return;

            CreateBootstrapIfMissing();
        }

        private static void CreateBootstrapIfMissing()
        {
            if (I != null)
                return;

            GameObject root = new GameObject("RealtimeNetworkBootstrap");
            root.SetActive(false);
            root.AddComponent<Tugboat>();
            ServerManager serverManager = root.AddComponent<ServerManager>();
            DisableFishNetHeadlessAutoStart(serverManager);
            NetworkManager manager = root.AddComponent<NetworkManager>();
            NetworkObject matrixPlayerPrefab = Resources.Load<NetworkObject>(MatrixPlayerResourcePath);
            if (matrixPlayerPrefab == null)
                matrixPlayerPrefab = CreateRuntimeMatrixPlayerPrefab();
            manager.SpawnablePrefabs = CreateSpawnablePrefabs(matrixPlayerPrefab);
            RealtimeNetworkBootstrap bootstrap = root.AddComponent<RealtimeNetworkBootstrap>();
            bootstrap.matrixPlayerPrefab = matrixPlayerPrefab;
            PersistentObjectUtility.DontDestroyOnLoad(root);
            root.SetActive(true);
        }

        private static bool ShouldRunNetworkForScene(string sceneName)
        {
            return string.Equals(sceneName, SymbiozSceneName, StringComparison.OrdinalIgnoreCase);
        }

        private static void DisableFishNetHeadlessAutoStart(ServerManager serverManager)
        {
            if (serverManager == null)
                return;

            FieldInfo field = typeof(ServerManager).GetField("_startOnHeadless", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                return;

            field.SetValue(serverManager, false);
        }

        private static bool HasNetworkCommandLineArgs(string[] args)
        {
            return HasArg(args, HeadlessArg) ||
                   HasArg(args, HostArg) ||
                   HasArg(args, ClientArg) ||
                   HasArg(args, DirectSymbiozArg);
        }

        private static DefaultPrefabObjects CreateSpawnablePrefabs(NetworkObject matrixPlayerPrefab)
        {
            DefaultPrefabObjects prefabs = ScriptableObject.CreateInstance<DefaultPrefabObjects>();
            if (matrixPlayerPrefab != null)
            {
                prefabs.AddObject(matrixPlayerPrefab);
            }
            else
            {
                Debug.LogWarning($"[RealtimeNetworkBootstrap] Matrix player prefab was not found at Resources/{MatrixPlayerResourcePath}.");
            }

            return prefabs;
        }

        private static NetworkObject CreateRuntimeMatrixPlayerPrefab()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "MatrixNetworkPlayerRuntimePrefab";
            player.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            Collider collider = player.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            MeshRenderer renderer = player.GetComponent<MeshRenderer>();
            if (renderer != null)
                Destroy(renderer);
            MeshFilter meshFilter = player.GetComponent<MeshFilter>();
            if (meshFilter != null)
                Destroy(meshFilter);

            NetworkObject networkObject = player.AddComponent<NetworkObject>();
            networkObject.SetAssetPathHash(RuntimeMatrixPlayerAssetPathHash);
            player.AddComponent<SymbiozNetworkPawn>();
            player.SetActive(false);
            PersistentObjectUtility.DontDestroyOnLoad(player);
            return networkObject;
        }

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
            ResolveComponents();
            resolvedAddress = defaultAddress;
            resolvedPort = defaultPort;
            if (matrixPlayerPrefab == null)
            {
                matrixPlayerPrefab = Resources.Load<NetworkObject>(MatrixPlayerResourcePath);
                if (matrixPlayerPrefab == null)
                    matrixPlayerPrefab = CreateRuntimeMatrixPlayerPrefab();
            }
        }

        private void Start()
        {
            StartCoroutine(InitializeFromServer());
        }

        public void StartClient()
        {
            ResolveComponents();
            if (networkManager == null || networkManager.ClientManager == null)
                return;

            if (networkManager.ClientManager.Started || clientStartRequested)
                return;

            clientStartRequested = true;
            networkManager.ClientManager.StartConnection(Address, Port);
            Debug.Log($"[RealtimeNetworkBootstrap] FishNet client connecting to {Address}:{Port}");
        }

        public void StartServer()
        {
            ResolveComponents();
            if (networkManager == null || networkManager.ServerManager == null)
                return;

            if (networkManager.ServerManager.Started || serverStartRequested)
            {
                RegisterServerConnectionEvents();
                return;
            }

            serverStartRequested = true;
            RegisterServerConnectionEvents();
            bool started = networkManager.ServerManager.StartConnection(Port);
            Debug.Log(started
                ? $"[RealtimeNetworkBootstrap] FishNet server start requested on port {Port}"
                : $"[RealtimeNetworkBootstrap] FishNet server failed to start on port {Port}");
        }

        public void StartHost()
        {
            StartServer();
            StartClient();
        }

        public void StopAll()
        {
            ResolveComponents();
            if (networkManager == null)
                return;

            if (networkManager.ClientManager != null && networkManager.ClientManager.Started)
            {
                networkManager.ClientManager.StopConnection();
                clientStartRequested = false;
            }

            if (networkManager.ServerManager != null && networkManager.ServerManager.Started)
            {
                UnregisterServerConnectionEvents();
                networkManager.ServerManager.StopConnection(true);
                serverStartRequested = false;
            }
        }

        private IEnumerator InitializeFromServer()
        {
            string[] args = Environment.GetCommandLineArgs();
            ApplyCommandLine(args);
            bool shouldRunServer = HasArg(args, HeadlessArg);
            bool directSymbiozClient = HasArg(args, DirectSymbiozArg);
            bool platformEntryClient = HasArg(args, PlatformEntryArg);
            bool shouldRunClient = HasArg(args, ClientArg) || directSymbiozClient || autoConnectClientOnStartup;

#if UNITY_EDITOR
            if (!HasNetworkCommandLineArgs(args))
            {
                if (ShouldRunNetworkForScene(UnitySceneManager.GetActiveScene().name))
                {
                    shouldRunClient = true;
                    Debug.Log("[RealtimeNetworkBootstrap] Editor Symbioz mode: auto-connecting to dedicated FishNet server.");
                }
                else
                {
                    Debug.Log("[RealtimeNetworkBootstrap] Editor local prototype mode: FishNet auto-connect skipped. Use -fishnet-client, -fishnet-host, or -fishnet-server to enable network.");
                    yield break;
                }
            }
#endif

            if (fetchServerConfigOnStartup && !directSymbiozClient)
                yield return FetchConfig();

            if (shouldRunServer && !ShouldRunNetworkForScene(UnitySceneManager.GetActiveScene().name))
            {
                Debug.Log($"[RealtimeNetworkBootstrap] Dedicated server loading {SymbiozSceneName} before FishNet start.");
                yield return UnitySceneManager.LoadSceneAsync(SymbiozSceneName);
            }
            else if (directSymbiozClient && !ShouldRunNetworkForScene(UnitySceneManager.GetActiveScene().name))
            {
                Debug.Log($"[RealtimeNetworkBootstrap] Direct test client loading {SymbiozSceneName} before FishNet start.");
                yield return UnitySceneManager.LoadSceneAsync(SymbiozSceneName);
            }
            else if (!shouldRunServer && shouldRunClient && platformEntryClient && !ShouldRunNetworkForScene(UnitySceneManager.GetActiveScene().name))
            {
                Debug.Log($"[RealtimeNetworkBootstrap] Platform entry client waiting for {SymbiozSceneName} before FishNet start.");
                while (!ShouldRunNetworkForScene(UnitySceneManager.GetActiveScene().name))
                    yield return null;
            }

            if (shouldRunServer)
                StartServer();
            else if (HasArg(args, HostArg))
                StartHost();
            else if (shouldRunClient)
                StartClient();
        }

        private void RegisterServerConnectionEvents()
        {
            ResolveComponents();
            if (networkManager == null || networkManager.ServerManager == null || serverConnectionEventsRegistered)
                return;

            networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
            serverConnectionEventsRegistered = true;

            if (!serverStateEventsRegistered)
            {
                networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
                serverStateEventsRegistered = true;
            }

            if (!serverSceneEventsRegistered && networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
                serverSceneEventsRegistered = true;
            }
        }

        private void UnregisterServerConnectionEvents()
        {
            if (networkManager == null || networkManager.ServerManager == null || !serverConnectionEventsRegistered)
                return;

            networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
            serverConnectionEventsRegistered = false;
            if (serverStateEventsRegistered)
            {
                networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
                serverStateEventsRegistered = false;
            }

            if (serverSceneEventsRegistered && networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
                serverSceneEventsRegistered = false;
            }

            spawnedPlayersByConnection.Clear();
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
        {
            Debug.Log($"[RealtimeNetworkBootstrap] Server connection state={args.ConnectionState}");
        }

        private void ServerManager_OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (connection == null)
                return;

            Debug.Log($"[RealtimeNetworkBootstrap] Remote connection {connection.ClientId} state={args.ConnectionState}");

            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                if (connection.LoadedStartScenes_Internal(true))
                    SpawnNetworkPawn(connection);
                else
                    Debug.Log($"[RealtimeNetworkBootstrap] Waiting for connection {connection.ClientId} to load start scenes before player spawn.");
                return;
            }

            if (args.ConnectionState == RemoteConnectionState.Stopped)
                DespawnNetworkPawn(connection.ClientId);
        }

        private void SceneManager_OnClientLoadedStartScenes(NetworkConnection connection, bool asServer)
        {
            if (!asServer || connection == null || !connection.IsActive)
                return;

            Debug.Log($"[RealtimeNetworkBootstrap] Connection {connection.ClientId} loaded start scenes; spawning owned pawn.");
            SpawnNetworkPawn(connection);
        }

        private void SpawnNetworkPawn(NetworkConnection connection)
        {
            if (networkManager == null || networkManager.ServerManager == null || !networkManager.ServerManager.Started)
                return;

            if (matrixPlayerPrefab == null)
            {
                Debug.LogWarning("[RealtimeNetworkBootstrap] No Matrix/Symbioz network player prefab is available.");
                return;
            }

            if (spawnedPlayersByConnection.ContainsKey(connection.ClientId))
                return;

            NetworkObject instance = Instantiate(matrixPlayerPrefab);
            instance.name = $"SymbiozNetworkPawn_{connection.ClientId}";
            instance.transform.position = ResolveSpawnPosition(connection.ClientId);
            instance.gameObject.SetActive(true);
            networkManager.ServerManager.Spawn(instance, connection);
            if (instance.Owner != connection)
                instance.GiveOwnership(connection);
            spawnedPlayersByConnection[connection.ClientId] = instance;
            int ownerId = instance.Owner != null ? instance.Owner.ClientId : -1;
            Debug.Log($"[RealtimeNetworkBootstrap] Spawned Symbioz network pawn for connection {connection.ClientId} owner={ownerId} at {instance.transform.position}.");
        }

        private void DespawnNetworkPawn(int clientId)
        {
            if (!spawnedPlayersByConnection.TryGetValue(clientId, out NetworkObject pawn))
                return;

            spawnedPlayersByConnection.Remove(clientId);
            if (pawn == null)
                return;

            if (networkManager != null && networkManager.ServerManager != null && networkManager.ServerManager.Started)
                networkManager.ServerManager.Despawn(pawn);
            else
                Destroy(pawn.gameObject);
        }

        private static Vector3 ResolveSpawnPosition(int clientId)
        {
            const int gridSize = 275;
            const float cellSize = 1f;
            const float half = gridSize * cellSize * 0.5f;
            const int northDoorX = gridSize / 2;
            const int firstLocationSpawnY = gridSize - 3;
            float offset = Mathf.Clamp(clientId, 0, 12) * 1.2f;
            float x = -half + (northDoorX + 0.5f) * cellSize + offset;
            float z = -half + (firstLocationSpawnY + 0.5f) * cellSize;
            return new Vector3(x, 0.22f, z);
        }

        private IEnumerator FetchConfig()
        {
            using UnityWebRequest request = UnityWebRequest.Get(ConfigUrl);
            request.timeout = Mathf.Max(1, configTimeoutSeconds);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogWarning("[RealtimeNetworkBootstrap] Multiplayer config request failed: " + request.error);
                yield break;
            }

            MultiplayerConfigResponse response = null;
            try
            {
                response = JsonUtility.FromJson<MultiplayerConfigResponse>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RealtimeNetworkBootstrap] Multiplayer config JSON failed: " + ex.Message);
            }

            if (response == null || !response.success)
                yield break;

            if (!string.IsNullOrWhiteSpace(response.host))
                resolvedAddress = response.host.Trim();

            if (response.port > 0 && response.port <= ushort.MaxValue)
                resolvedPort = (ushort)response.port;
        }

        private void ResolveComponents()
        {
            if (networkManager == null)
                networkManager = GetComponent<NetworkManager>();

            if (tugboat == null)
                tugboat = GetComponent<Tugboat>();

            if (tugboat != null)
            {
                tugboat.SetClientAddress(Address);
                tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
                tugboat.SetServerBindAddress("disabled", IPAddressType.IPv6);
                tugboat.SetPort(Port);
            }
        }

        private void ApplyCommandLine(string[] args)
        {
            string address = ReadArgValue(args, AddressArg);
            if (!string.IsNullOrWhiteSpace(address))
                resolvedAddress = address.Trim();

            string portText = ReadArgValue(args, PortArg);
            if (ushort.TryParse(portText, out ushort parsedPort) && parsedPort > 0)
                resolvedPort = parsedPort;
        }

        private static bool HasArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ReadArgValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return string.Empty;
        }

        [Serializable]
        private sealed class MultiplayerConfigResponse
        {
            public bool success;
            public string host;
            public int port;
        }
    }
}
