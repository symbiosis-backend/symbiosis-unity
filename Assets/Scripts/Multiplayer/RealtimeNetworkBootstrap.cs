using System;
using System.Collections;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using MahjongGame.Clusters;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class RealtimeNetworkBootstrap : MonoBehaviour
    {
        private const string ConfigUrl = "https://dlsymbiosis.com/multiplayer/config";
        private const string HeadlessArg = "-fishnet-server";
        private const string HostArg = "-fishnet-host";
        private const string ClientArg = "-fishnet-client";
        private const string AddressArg = "-fishnet-address";
        private const string PortArg = "-fishnet-port";
        private const string MatrixPlayerResourcePath = "Network/MatrixNetworkPlayer";

        public static RealtimeNetworkBootstrap I { get; private set; }

        [Header("Defaults")]
        [SerializeField] private string defaultAddress = "91.99.176.77";
        [SerializeField] private ushort defaultPort = 7770;
        [SerializeField] private bool fetchServerConfigOnStartup = true;
        [SerializeField] private bool autoConnectClientOnStartup;
        [SerializeField] private int configTimeoutSeconds = 8;

        private NetworkManager networkManager;
        private Tugboat tugboat;
        private string resolvedAddress;
        private ushort resolvedPort;

        public NetworkManager NetworkManager => networkManager;
        public string Address => string.IsNullOrWhiteSpace(resolvedAddress) ? defaultAddress : resolvedAddress;
        public ushort Port => resolvedPort == 0 ? defaultPort : resolvedPort;
        public bool IsServerStarted => networkManager != null && networkManager.ServerManager != null && networkManager.ServerManager.Started;
        public bool IsClientStarted => networkManager != null && networkManager.ClientManager != null && networkManager.ClientManager.Started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (I != null)
                return;

            GameObject root = new GameObject("RealtimeNetworkBootstrap");
            root.SetActive(false);
            root.AddComponent<Tugboat>();
            NetworkManager manager = root.AddComponent<NetworkManager>();
            NetworkObject matrixPlayerPrefab = Resources.Load<NetworkObject>(MatrixPlayerResourcePath);
            if (matrixPlayerPrefab == null)
                matrixPlayerPrefab = CreateRuntimeMatrixPlayerPrefab();

            manager.SpawnablePrefabs = CreateSpawnablePrefabs(matrixPlayerPrefab);
            MatrixNetworkSpawner spawner = root.AddComponent<MatrixNetworkSpawner>();
            spawner.Configure(manager, matrixPlayerPrefab);
            root.AddComponent<RealtimeNetworkBootstrap>();
            PersistentObjectUtility.DontDestroyOnLoad(root);
            root.SetActive(true);
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

            NetworkObject networkObject = player.AddComponent<NetworkObject>();
            player.AddComponent<MatrixNetworkAvatar>();
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

            if (networkManager.ClientManager.Started)
                return;

            networkManager.ClientManager.StartConnection(Address, Port);
            Debug.Log($"[RealtimeNetworkBootstrap] FishNet client connecting to {Address}:{Port}");
        }

        public void StartServer()
        {
            ResolveComponents();
            if (networkManager == null || networkManager.ServerManager == null)
                return;

            if (networkManager.ServerManager.Started)
                return;

            networkManager.ServerManager.StartConnection(Port);
            Debug.Log($"[RealtimeNetworkBootstrap] FishNet server listening on port {Port}");
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
                networkManager.ClientManager.StopConnection();

            if (networkManager.ServerManager != null && networkManager.ServerManager.Started)
                networkManager.ServerManager.StopConnection(true);
        }

        private IEnumerator InitializeFromServer()
        {
            string[] args = Environment.GetCommandLineArgs();
            ApplyCommandLine(args);

            if (fetchServerConfigOnStartup)
                yield return FetchConfig();

            if (HasArg(args, HeadlessArg) || Application.isBatchMode)
                StartServer();
            else if (HasArg(args, HostArg))
                StartHost();
            else if (HasArg(args, ClientArg) || autoConnectClientOnStartup)
                StartClient();
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
