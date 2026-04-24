using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MahjongGame.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class LocalWifiBattleNetwork : MonoBehaviour
    {
        public const int DefaultTcpPort = 47770;
        public const int DefaultDiscoveryPort = 47771;

        private const string DiscoveryPrefix = "SYMBIOSIS_WIFI_BATTLE";
        private const string MessageHello = "HELLO";
        private const string MessageStart = "START";
        private const string MessageTilePick = "PICK";
        private const string MessageDamage = "DAMAGE";
        private const string MessageRoundEnd = "ROUND_END";
        private const string MessageDisconnect = "BYE";

        public event Action<IReadOnlyList<DiscoveredGame>> DiscoveryChanged;
        public event Action<string> StatusChanged;
        public event Action<RemotePlayerInfo> PeerInfoChanged;
        public event Action<bool> ConnectionStateChanged;
        public event Action<int> MatchStartRequested;
        public event Action<int, string> RemoteTilePicked;
        public event Action<BattleBoardSide, int> RemoteDamageApplied;
        public event Action<int, BattleBoardSide> RemoteRoundEnded;
        public event Action ConnectionClosed;

        public static LocalWifiBattleNetwork I { get; private set; }

        private readonly ConcurrentQueue<Action> mainThreadActions = new();
        private readonly ConcurrentQueue<RemoteTilePickMessage> pendingRemoteTilePicks = new();
        private readonly ConcurrentQueue<RemoteDamageMessage> pendingRemoteDamages = new();
        private readonly List<DiscoveredGame> discoveredGames = new();
        private readonly object discoveredLock = new();

        private TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;
        private Thread acceptThread;
        private Thread readThread;
        private Thread broadcastThread;
        private Thread discoveryThread;
        private CancellationTokenSource lifetime;
        private CancellationTokenSource discoveryLifetime;

        private bool isHost;
        private bool isConnected;
        private bool matchStarted;
        private int pendingMatchSeed;
        private string status = "Idle";
        private LocalPlayerInfo localPlayer;
        private RemotePlayerInfo remotePlayer;
        private AndroidJavaObject multicastLock;

        public bool IsHost => isHost;
        public bool IsConnected => isConnected;
        public bool MatchStarted => matchStarted;
        public int MatchSeed => pendingMatchSeed;
        public string Status => status;
        public RemotePlayerInfo RemotePlayer => remotePlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static LocalWifiBattleNetwork EnsureInstance()
        {
            if (I != null)
                return I;

            GameObject root = new GameObject("LocalWifiBattleNetwork");
            I = root.AddComponent<LocalWifiBattleNetwork>();
            DontDestroyOnLoad(root);
            return I;
        }

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            while (mainThreadActions.TryDequeue(out Action action))
                action?.Invoke();
        }

        private void OnDestroy()
        {
            StopAllNetworking();
            ReleaseAndroidMulticastLock();
            if (I == this)
                I = null;
        }

        public void StartHost(LocalPlayerInfo player, int matchSeed = 0)
        {
            StopAllNetworking();

            localPlayer = player ?? LocalPlayerInfo.CreateFallback();
            remotePlayer = null;
            pendingMatchSeed = matchSeed > 0 ? matchSeed : UnityEngine.Random.Range(100000, 999999);
            isHost = true;
            matchStarted = false;
            lifetime = new CancellationTokenSource();

            try
            {
                listener = new TcpListener(IPAddress.Any, DefaultTcpPort);
                listener.Start();
                AcquireAndroidMulticastLock();
                SetStatus($"Hosting Wi-Fi battle on {GetLocalAddressText()}:{DefaultTcpPort}");

                acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "LocalWifiBattleAccept" };
                acceptThread.Start(lifetime.Token);

                broadcastThread = new Thread(BroadcastLoop) { IsBackground = true, Name = "LocalWifiBattleBroadcast" };
                broadcastThread.Start(lifetime.Token);
            }
            catch (Exception ex)
            {
                SetStatus("Host failed: " + ex.Message);
                StopAllNetworking();
            }
        }

        public void StartDiscovery()
        {
            StopDiscovery();

            AcquireAndroidMulticastLock();
            discoveryLifetime = new CancellationTokenSource();
            discoveryThread = new Thread(DiscoveryLoop) { IsBackground = true, Name = "LocalWifiBattleDiscovery" };
            discoveryThread.Start(discoveryLifetime.Token);
            SetStatus("Searching local Wi-Fi battles...");
        }

        public void ConnectTo(DiscoveredGame game, LocalPlayerInfo player)
        {
            if (game == null)
            {
                SetStatus("No Wi-Fi battle selected.");
                return;
            }

            ConnectTo(game.Address, game.Port, player);
        }

        public void ConnectTo(string address, int port, LocalPlayerInfo player)
        {
            StopDiscovery();
            StopConnectionOnly();

            localPlayer = player ?? LocalPlayerInfo.CreateFallback();
            remotePlayer = null;
            isHost = false;
            matchStarted = false;

            if (lifetime == null || lifetime.IsCancellationRequested)
                lifetime = new CancellationTokenSource();

            Thread connectThread = new Thread(() =>
            {
                try
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.NoDelay = true;
                    tcpClient.Connect(address, Mathf.Clamp(port, 1, 65535));
                    AttachClient(tcpClient);
                    SendHello();
                    StartHelloRetry();
                    Enqueue(() => SetStatus($"Connected to {address}:{port}. Waiting for match start..."));
                }
                catch (Exception ex)
                {
                    Enqueue(() => SetStatus("Connect failed: " + ex.Message));
                }
            })
            {
                IsBackground = true,
                Name = "LocalWifiBattleConnect"
            };
            connectThread.Start();
        }

        public void SendLocalTilePick(int tileIndex, string tileId)
        {
            if (!isConnected || tileIndex < 0)
                return;

            SendLine($"{MessageTilePick}|{tileIndex}|{Encode(tileId)}");
        }

        public void SendDamage(BattleBoardSide targetSide, int amount)
        {
            if (!isConnected || amount <= 0)
                return;

            SendLine($"{MessageDamage}|{(int)targetSide}|{amount}");
        }

        public void SendRoundEnded(int roundNumber, BattleBoardSide deadSide)
        {
            if (!isConnected || roundNumber <= 0)
                return;

            SendLine($"{MessageRoundEnd}|{roundNumber}|{(int)deadSide}");
        }

        public bool TryDequeuePendingRemoteTilePick(out int tileIndex, out string tileId)
        {
            if (pendingRemoteTilePicks.TryDequeue(out RemoteTilePickMessage message))
            {
                tileIndex = message.TileIndex;
                tileId = message.TileId;
                return true;
            }

            tileIndex = -1;
            tileId = string.Empty;
            return false;
        }

        public bool TryDequeuePendingRemoteDamage(out BattleBoardSide targetSide, out int amount)
        {
            if (pendingRemoteDamages.TryDequeue(out RemoteDamageMessage message))
            {
                targetSide = message.TargetSide;
                amount = message.Amount;
                return true;
            }

            targetSide = BattleBoardSide.Opponent;
            amount = 0;
            return false;
        }

        public bool StartHostedMatch()
        {
            if (!isHost)
            {
                SetStatus("Only the host can start this Wi-Fi battle.");
                return false;
            }

            if (!isConnected)
            {
                SetStatus("Waiting for a player before starting.");
                return false;
            }

            if (matchStarted)
                return true;

            matchStarted = true;
            SendLine($"{MessageStart}|{pendingMatchSeed}");
            SetStatus("Starting Wi-Fi battle...");
            MatchStartRequested?.Invoke(pendingMatchSeed);
            return true;
        }

        public void StopAllNetworking()
        {
            StopDiscovery();
            StopConnectionOnly();
            StopLifetime();

            lock (discoveredLock)
                discoveredGames.Clear();

            isHost = false;
            isConnected = false;
            matchStarted = false;
            pendingMatchSeed = 0;
            remotePlayer = null;
            ClearPendingGameplayMessages();
            ReleaseAndroidMulticastLock();
            NotifyDiscoveryChanged();
            ConnectionStateChanged?.Invoke(false);
        }

        private void StopDiscovery()
        {
            try { discoveryLifetime?.Cancel(); } catch { }
            discoveryLifetime = null;
            discoveryThread = null;
        }

        private void StopConnectionOnly()
        {
            try
            {
                SendLine($"{MessageDisconnect}");
            }
            catch
            {
                // Closing can race with a dead socket; ignore and continue teardown.
            }

            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            try { listener?.Stop(); } catch { }

            stream = null;
            client = null;
            listener = null;
            acceptThread = null;
            readThread = null;
            broadcastThread = null;
            if (isConnected)
            {
                isConnected = false;
                ConnectionStateChanged?.Invoke(false);
            }
        }

        private void StopLifetime()
        {
            try { lifetime?.Cancel(); } catch { }
            lifetime = null;
        }

        private void AcceptLoop(object tokenObject)
        {
            CancellationToken token = (CancellationToken)tokenObject;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient accepted = listener.AcceptTcpClient();
                    accepted.NoDelay = true;
                    AttachClient(accepted);
                    SendHello();
                    StartHelloRetry();
                    Enqueue(() =>
                    {
                        SetStatus("Player connected. Waiting for host to start...");
                    });
                    return;
                }
                catch (SocketException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Enqueue(() => SetStatus("Host accept failed: " + ex.Message));
                    return;
                }
            }
        }

        private void AttachClient(TcpClient tcpClient)
        {
            client = tcpClient;
            stream = client.GetStream();
            isConnected = true;
            Enqueue(() => ConnectionStateChanged?.Invoke(true));

            readThread = new Thread(ReadLoop) { IsBackground = true, Name = "LocalWifiBattleRead" };
            readThread.Start(lifetime != null ? lifetime.Token : CancellationToken.None);
        }

        private void ReadLoop(object tokenObject)
        {
            CancellationToken token = (CancellationToken)tokenObject;

            try
            {
                byte[] one = new byte[1];
                List<byte> line = new List<byte>(128);

                while (!token.IsCancellationRequested && client != null && client.Connected)
                {
                    int read = stream.Read(one, 0, 1);
                    if (read <= 0)
                        break;

                    if (one[0] == (byte)'\n')
                    {
                        string message = Encoding.UTF8.GetString(line.ToArray()).Trim();
                        line.Clear();
                        if (!string.IsNullOrWhiteSpace(message))
                            Enqueue(() => HandleMessage(message));
                    }
                    else
                    {
                        line.Add(one[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Enqueue(() => SetStatus("Connection closed: " + ex.Message));
            }

            Enqueue(() =>
            {
                if (!isConnected)
                    return;

                isConnected = false;
                remotePlayer = null;
                ConnectionStateChanged?.Invoke(false);
                ConnectionClosed?.Invoke();
            });
        }

        private void BroadcastLoop(object tokenObject)
        {
            CancellationToken token = (CancellationToken)tokenObject;

            using UdpClient udp = new UdpClient();
            udp.EnableBroadcast = true;
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Broadcast, DefaultDiscoveryPort);

            while (!token.IsCancellationRequested && isHost && !isConnected)
            {
                string payload = CreateDiscoveryPayload();
                byte[] bytes = Encoding.UTF8.GetBytes(payload);

                try
                {
                    udp.Send(bytes, bytes.Length, endpoint);
                }
                catch
                {
                    // Some networks block broadcast; the host can still be joined by IP if needed later.
                }

                Thread.Sleep(1000);
            }
        }

        private void DiscoveryLoop(object tokenObject)
        {
            CancellationToken token = (CancellationToken)tokenObject;

            try
            {
                using UdpClient udp = new UdpClient(DefaultDiscoveryPort);
                udp.EnableBroadcast = true;
                udp.Client.ReceiveTimeout = 1000;
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        byte[] bytes = udp.Receive(ref endpoint);
                        string payload = Encoding.UTF8.GetString(bytes);
                        DiscoveredGame game = ParseDiscovery(payload, endpoint.Address.ToString());
                        if (game != null)
                            Enqueue(() => UpsertDiscoveredGame(game));
                    }
                    catch (SocketException)
                    {
                        // Receive timeout keeps the thread responsive to cancellation.
                    }
                }
            }
            catch (Exception ex)
            {
                Enqueue(() => SetStatus("Discovery failed: " + ex.Message));
            }
        }

        private string CreateDiscoveryPayload()
        {
            string playerName = localPlayer != null ? localPlayer.DisplayName : "Player";
            return string.Join(
                "|",
                DiscoveryPrefix,
                DefaultTcpPort.ToString(),
                pendingMatchSeed.ToString(),
                Encode(playerName));
        }

        private static DiscoveredGame ParseDiscovery(string payload, string fallbackAddress)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            string[] parts = payload.Split('|');
            if (parts.Length < 4 || parts[0] != DiscoveryPrefix)
                return null;

            if (!int.TryParse(parts[1], out int port))
                port = DefaultTcpPort;

            int.TryParse(parts[2], out int seed);

            return new DiscoveredGame(
                fallbackAddress,
                Mathf.Clamp(port, 1, 65535),
                seed,
                Decode(parts[3]));
        }

        private void UpsertDiscoveredGame(DiscoveredGame game)
        {
            lock (discoveredLock)
            {
                int index = discoveredGames.FindIndex(item =>
                    string.Equals(item.Address, game.Address, StringComparison.OrdinalIgnoreCase) &&
                    item.Port == game.Port);

                if (index >= 0)
                    discoveredGames[index] = game.WithLastSeen(Time.realtimeSinceStartup);
                else
                    discoveredGames.Add(game.WithLastSeen(Time.realtimeSinceStartup));
            }

            NotifyDiscoveryChanged();
        }

        private void NotifyDiscoveryChanged()
        {
            List<DiscoveredGame> snapshot;
            lock (discoveredLock)
                snapshot = new List<DiscoveredGame>(discoveredGames);

            DiscoveryChanged?.Invoke(snapshot);
        }

        private void HandleMessage(string message)
        {
            string[] parts = message.Split('|');
            if (parts.Length == 0)
                return;

            switch (parts[0])
            {
                case MessageHello:
                    remotePlayer = RemotePlayerInfo.FromParts(parts);
                    PeerInfoChanged?.Invoke(remotePlayer);
                    SetStatus(isHost
                        ? $"Player joined: {remotePlayer.DisplayName}. Press Start."
                        : $"Connected to host: {remotePlayer.DisplayName}. Waiting for start...");
                    break;
                case MessageStart:
                    if (parts.Length > 1 && int.TryParse(parts[1], out int seed))
                        pendingMatchSeed = seed;

                    matchStarted = true;
                    MatchStartRequested?.Invoke(pendingMatchSeed);
                    break;
                case MessageTilePick:
                    if (parts.Length > 2 && int.TryParse(parts[1], out int tileIndex))
                        RaiseOrQueueRemoteTilePick(tileIndex, Decode(parts[2]));
                    else if (parts.Length > 1)
                        RaiseOrQueueRemoteTilePick(-1, Decode(parts[1]));
                    break;
                case MessageDamage:
                    if (parts.Length > 2 &&
                        int.TryParse(parts[1], out int sideValue) &&
                        int.TryParse(parts[2], out int amount))
                    {
                        RaiseOrQueueRemoteDamage((BattleBoardSide)sideValue, Mathf.Max(0, amount));
                    }
                    break;
                case MessageRoundEnd:
                    if (parts.Length > 2 &&
                        int.TryParse(parts[1], out int roundNumber) &&
                        int.TryParse(parts[2], out int deadSideValue))
                    {
                        RemoteRoundEnded?.Invoke(Mathf.Max(1, roundNumber), (BattleBoardSide)deadSideValue);
                    }
                    break;
                case MessageDisconnect:
                    SetStatus("Peer left the Wi-Fi battle.");
                    isConnected = false;
                    ConnectionClosed?.Invoke();
                    break;
            }
        }

        private void SendHello()
        {
            LocalPlayerInfo player = localPlayer ?? LocalPlayerInfo.CreateFallback();
            SendLine(string.Join(
                "|",
                MessageHello,
                Encode(player.DisplayName),
                Encode(player.RankTier),
                player.RankPoints.ToString(),
                Encode(player.CharacterId)));
        }

        public void SendProfileUpdate()
        {
            SendHello();
        }

        private void StartHelloRetry()
        {
            Thread helloThread = new Thread(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!isConnected || stream == null || matchStarted)
                        return;

                    try
                    {
                        SendHello();
                    }
                    catch
                    {
                        return;
                    }

                    Thread.Sleep(700);
                }
            })
            {
                IsBackground = true,
                Name = "LocalWifiBattleHelloRetry"
            };
            helloThread.Start();
        }

        private void SendLine(string line)
        {
            if (stream == null || string.IsNullOrEmpty(line))
                return;

            byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
            lock (this)
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
        }

        private void SetStatus(string value)
        {
            status = string.IsNullOrWhiteSpace(value) ? "Idle" : value;
            StatusChanged?.Invoke(status);
            Debug.Log("[LocalWifiBattleNetwork] " + status);
        }

        private void AcquireAndroidMulticastLock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (multicastLock != null)
                return;

            try
            {
                using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject appContext = activity.Call<AndroidJavaObject>("getApplicationContext");
                AndroidJavaObject wifiManager = appContext.Call<AndroidJavaObject>("getSystemService", "wifi");
                multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "SymbiosisLocalWifiBattle");
                multicastLock.Call("setReferenceCounted", false);
                multicastLock.Call("acquire");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LocalWifiBattleNetwork] Failed to acquire Android multicast lock: " + ex.Message);
                multicastLock = null;
            }
#endif
        }

        private void ReleaseAndroidMulticastLock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (multicastLock == null)
                return;

            try
            {
                multicastLock.Call("release");
                multicastLock.Dispose();
            }
            catch
            {
                // Ignore Android cleanup failures during scene/app shutdown.
            }
            finally
            {
                multicastLock = null;
            }
#endif
        }

        private void Enqueue(Action action)
        {
            if (action != null)
                mainThreadActions.Enqueue(action);
        }

        private void RaiseOrQueueRemoteTilePick(int tileIndex, string tileId)
        {
            if (RemoteTilePicked != null)
                RemoteTilePicked.Invoke(tileIndex, tileId);
            else
                pendingRemoteTilePicks.Enqueue(new RemoteTilePickMessage(tileIndex, tileId));
        }

        private void RaiseOrQueueRemoteDamage(BattleBoardSide targetSide, int amount)
        {
            if (RemoteDamageApplied != null)
                RemoteDamageApplied.Invoke(targetSide, amount);
            else
                pendingRemoteDamages.Enqueue(new RemoteDamageMessage(targetSide, amount));
        }

        private void ClearPendingGameplayMessages()
        {
            while (pendingRemoteTilePicks.TryDequeue(out _))
            {
            }

            while (pendingRemoteDamages.TryDequeue(out _))
            {
            }
        }

        private static string Encode(string value)
        {
            value ??= string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetLocalAddressText()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                for (int i = 0; i < host.AddressList.Length; i++)
                {
                    IPAddress address = host.AddressList[i];
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                        return address.ToString();
                }
            }
            catch
            {
                return "this device";
            }

            return "this device";
        }

        private readonly struct RemoteTilePickMessage
        {
            public readonly int TileIndex;
            public readonly string TileId;

            public RemoteTilePickMessage(int tileIndex, string tileId)
            {
                TileIndex = tileIndex;
                TileId = tileId;
            }
        }

        private readonly struct RemoteDamageMessage
        {
            public readonly BattleBoardSide TargetSide;
            public readonly int Amount;

            public RemoteDamageMessage(BattleBoardSide targetSide, int amount)
            {
                TargetSide = targetSide;
                Amount = amount;
            }
        }

        [Serializable]
        public sealed class DiscoveredGame
        {
            public readonly string Address;
            public readonly int Port;
            public readonly int MatchSeed;
            public readonly string HostName;
            public readonly float LastSeen;

            public DiscoveredGame(string address, int port, int matchSeed, string hostName, float lastSeen = 0f)
            {
                Address = address;
                Port = port;
                MatchSeed = matchSeed;
                HostName = string.IsNullOrWhiteSpace(hostName) ? "Wi-Fi Host" : hostName;
                LastSeen = lastSeen;
            }

            public DiscoveredGame WithLastSeen(float value)
            {
                return new DiscoveredGame(Address, Port, MatchSeed, HostName, value);
            }
        }

        public sealed class LocalPlayerInfo
        {
            public string DisplayName;
            public string RankTier;
            public int RankPoints;
            public string CharacterId;

            public static LocalPlayerInfo CreateFallback()
            {
                return new LocalPlayerInfo
                {
                    DisplayName = "Player",
                    RankTier = "Unranked",
                    RankPoints = 0,
                    CharacterId = string.Empty
                };
            }
        }

        public sealed class RemotePlayerInfo
        {
            public string DisplayName;
            public string RankTier;
            public int RankPoints;
            public string CharacterId;

            public static RemotePlayerInfo FromParts(string[] parts)
            {
                RemotePlayerInfo info = new RemotePlayerInfo
                {
                    DisplayName = parts.Length > 1 ? Decode(parts[1]) : "Opponent",
                    RankTier = parts.Length > 2 ? Decode(parts[2]) : "Unranked",
                    RankPoints = 0,
                    CharacterId = parts.Length > 4 ? Decode(parts[4]) : string.Empty
                };

                if (parts.Length > 3 && int.TryParse(parts[3], out int points))
                    info.RankPoints = Mathf.Max(0, points);

                if (string.IsNullOrWhiteSpace(info.DisplayName))
                    info.DisplayName = "Opponent";
                if (string.IsNullOrWhiteSpace(info.RankTier))
                    info.RankTier = "Unranked";
                if (info.CharacterId == null)
                    info.CharacterId = string.Empty;

                return info;
            }
        }
    }
}
