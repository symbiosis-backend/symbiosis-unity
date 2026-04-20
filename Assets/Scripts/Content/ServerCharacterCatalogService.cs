using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MahjongGame.Content
{
    public sealed class ServerCharacterCatalogService : MonoBehaviour
    {
        private const string CatalogUrl = "http://91.99.176.77:8080/content/characters";

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

            using UnityWebRequest request = UnityWebRequest.Get(CatalogUrl);
            request.timeout = Mathf.Max(1, requestTimeoutSeconds);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                LastError = request.error;
                Debug.LogWarning("[ServerCharacterCatalogService] Character catalog request failed: " + LastError);
                IsLoading = false;
                yield break;
            }

            BattleCharacterDatabase.RemoteCharacterCatalog catalog = null;
            try
            {
                catalog = JsonUtility.FromJson<BattleCharacterDatabase.RemoteCharacterCatalog>(request.downloadHandler.text);
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
