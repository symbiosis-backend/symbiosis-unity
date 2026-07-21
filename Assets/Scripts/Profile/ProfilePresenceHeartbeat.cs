using System;
using System.Collections;
using System.Text;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame
{
    public sealed class ProfilePresenceHeartbeat : MonoBehaviour
    {
        private const float IntervalSeconds = 20f;
        private const string PresencePath = "/presence/heartbeat";

        private float nextSendTime;
        private bool sending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Application.isBatchMode)
                return;

            if (FindAnyObjectByType<ProfilePresenceHeartbeat>() != null)
                return;

            GameObject root = new GameObject("ProfilePresenceHeartbeat");
            PersistentObjectUtility.DontDestroyOnLoad(root);
            root.AddComponent<ProfilePresenceHeartbeat>();
        }

        private void Update()
        {
            if (sending || Time.unscaledTime < nextSendTime)
                return;

            ProfileService service = ProfileService.I;
            PlayerProfile profile = service != null ? service.Current : null;
            string token = service != null ? service.CurrentSessionToken : string.Empty;
            if (profile == null || string.IsNullOrWhiteSpace(token))
            {
                nextSendTime = Time.unscaledTime + 3f;
                return;
            }

            nextSendTime = Time.unscaledTime + IntervalSeconds;
            StartCoroutine(SendHeartbeat(token, profile));
        }

        private IEnumerator SendHeartbeat(string token, PlayerProfile profile)
        {
            sending = true;

            PresenceHeartbeatPayload payload = new PresenceHeartbeatPayload
            {
                token = token,
                state = "online",
                mode = "symbiosis",
                screen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                characterId = profile != null ? profile.OnlinePlayerId : string.Empty,
                clientVersionCode = BackendEndpoints.GetClientVersionCode()
            };

            string json = JsonUtility.ToJson(payload);
            using UnityWebRequest request = new UnityWebRequest(BackendEndpoints.BuildUrl(BackendEndpoints.PrimaryBaseUrl, PresencePath), "POST");
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            BackendEndpoints.ApplyClientVersionHeaders(request);
            request.timeout = 8;

            yield return request.SendWebRequest();

            if (BackendEndpoints.RequestFailed(request))
                Debug.LogWarning("[ProfilePresenceHeartbeat] Heartbeat failed: " + request.error);

            sending = false;
        }

        [Serializable]
        private sealed class PresenceHeartbeatPayload
        {
            public string token;
            public string state;
            public string mode;
            public string screen;
            public string matchId;
            public string characterId;
            public int clientVersionCode;
        }
    }
}
