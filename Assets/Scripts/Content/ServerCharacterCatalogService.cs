using System;
using System.Collections;
using MahjongGame.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame.Content
{
    public sealed class ServerCharacterCatalogService : MonoBehaviour
    {
        private const string CatalogPath = "/content/characters";

        private static ServerCharacterCatalogService instance;

        [SerializeField] private float initialDelaySeconds = 0.6f;
        [SerializeField] private bool loadOnStartup = true;
        [SerializeField] private int requestTimeoutSeconds = 10;

        public bool IsLoading { get; private set; }
        public bool LastLoadSucceeded { get; private set; }
        public string LastError { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isBatchMode)
                return;

            if (instance != null)
                return;

            GameObject serviceObject = new GameObject("ServerCharacterCatalogService");
            instance = serviceObject.AddComponent<ServerCharacterCatalogService>();
            PersistentObjectUtility.DontDestroyOnLoad(serviceObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            PersistentObjectUtility.DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (loadOnStartup)
                StartCoroutine(LoadAfterDelay());
        }

        public static void RefreshNow()
        {
            if (Application.isBatchMode)
                return;

            if (instance == null)
                Bootstrap();

            if (instance != null)
                instance.StartCoroutine(instance.LoadCatalog());
        }

        private IEnumerator LoadAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, initialDelaySeconds));
            yield return LoadCatalog();
        }

        private IEnumerator LoadCatalog()
        {
            if (IsLoading)
                yield break;

            IsLoading = true;
            LastLoadSucceeded = false;
            LastError = string.Empty;

            string responseText = string.Empty;
            string requestError = string.Empty;
            bool failed = true;

            for (int i = 0; i < BackendEndpoints.BaseUrls.Length; i++)
            {
                using UnityWebRequest request = UnityWebRequest.Get(BackendEndpoints.BuildUrl(BackendEndpoints.BaseUrls[i], CatalogPath));
                request.timeout = Mathf.Max(1, requestTimeoutSeconds);
                yield return request.SendWebRequest();

                responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                requestError = request.error;
                failed = BackendEndpoints.RequestFailed(request);
                if (!failed || !BackendEndpoints.CanRetryWithFallback(request) || i == BackendEndpoints.BaseUrls.Length - 1)
                    break;
            }

            if (failed)
            {
                LastError = requestError;
                Debug.LogWarning("[ServerCharacterCatalogService] Character catalog request failed: " + LastError);
                IsLoading = false;
                yield break;
            }

            BattleCharacterDatabase.RemoteCharacterCatalog catalog = null;
            try
            {
                catalog = JsonUtility.FromJson<BattleCharacterDatabase.RemoteCharacterCatalog>(responseText);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Debug.LogWarning("[ServerCharacterCatalogService] Character catalog JSON failed: " + LastError);
            }

            if (catalog == null || !catalog.success)
            {
                LastError = "Catalog is empty or unsuccessful.";
                IsLoading = false;
                yield break;
            }

            BattleCharacterDatabase database = ResolveDatabase();
            if (database == null)
            {
                LastError = "BattleCharacterDatabase is not available.";
                Debug.LogWarning("[ServerCharacterCatalogService] " + LastError);
                IsLoading = false;
                yield break;
            }

            LastLoadSucceeded = database.ApplyRemoteCatalog(catalog);
            Debug.Log("[ServerCharacterCatalogService] Character catalog applied. Version=" + catalog.version + " Changed=" + LastLoadSucceeded);
            IsLoading = false;
        }

        private static BattleCharacterDatabase ResolveDatabase()
        {
            if (BattleCharacterDatabase.HasInstance)
                return BattleCharacterDatabase.Instance;

            return FindAnyObjectByType<BattleCharacterDatabase>(FindObjectsInactive.Include);
        }
    }
}
