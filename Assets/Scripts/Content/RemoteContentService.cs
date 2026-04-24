using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MahjongGame.Content
{
    public sealed class RemoteContentService : MonoBehaviour
    {
        private static RemoteContentService instance;

        [SerializeField] private float initialDelaySeconds = 0.8f;
        [SerializeField] private bool checkCatalogOnStartup = true;

        public bool IsChecking { get; private set; }
        public bool LastCheckSucceeded { get; private set; }
        public string LastError { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isBatchMode)
                return;

            if (instance != null)
                return;

            GameObject serviceObject = new GameObject("RemoteContentService");
            instance = serviceObject.AddComponent<RemoteContentService>();
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
            if (checkCatalogOnStartup)
                StartCoroutine(CheckCatalogAfterDelay());
        }

        public static void CheckNow()
        {
            if (Application.isBatchMode)
                return;

            if (instance == null)
                Bootstrap();

            if (instance != null)
                instance.StartCoroutine(instance.CheckAndUpdateCatalogs());
        }

        public IEnumerator DownloadLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                yield break;

            AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(label);
            yield return sizeHandle;

            if (!HandleSucceeded(sizeHandle))
            {
                LastError = "Could not check remote content size for label: " + label;
                Debug.LogWarning("[RemoteContentService] " + LastError);
                ReleaseIfValid(sizeHandle);
                yield break;
            }

            long size = sizeHandle.Result;
            ReleaseIfValid(sizeHandle);

            if (size <= 0)
                yield break;

            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(label);
            yield return downloadHandle;

            if (!HandleSucceeded(downloadHandle))
            {
                LastError = "Could not download remote content label: " + label;
                Debug.LogWarning("[RemoteContentService] " + LastError);
            }

            ReleaseIfValid(downloadHandle);
        }

        private IEnumerator CheckCatalogAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, initialDelaySeconds));
            yield return CheckAndUpdateCatalogs();
        }

        private IEnumerator CheckAndUpdateCatalogs()
        {
            if (IsChecking)
                yield break;

            IsChecking = true;
            LastCheckSucceeded = false;
            LastError = string.Empty;

            AsyncOperationHandle initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            if (!HandleSucceeded(initHandle))
            {
                LastError = "Addressables initialization failed.";
#if UNITY_EDITOR
                Debug.Log("[RemoteContentService] Addressables initialization skipped in Editor. Remote content checks will run in builds.");
#else
                Debug.LogWarning("[RemoteContentService] " + LastError);
#endif
                ReleaseIfValid(initHandle);
                IsChecking = false;
                yield break;
            }

            ReleaseIfValid(initHandle);

            AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
            yield return checkHandle;

            if (!HandleSucceeded(checkHandle))
            {
                LastError = "Remote catalog check failed.";
                Debug.LogWarning("[RemoteContentService] " + LastError);
                ReleaseIfValid(checkHandle);
                IsChecking = false;
                yield break;
            }

            List<string> catalogs = checkHandle.Result;
            if (catalogs == null || catalogs.Count == 0)
            {
                LastCheckSucceeded = true;
                ReleaseIfValid(checkHandle);
                IsChecking = false;
                yield break;
            }

            ReleaseIfValid(checkHandle);

            AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs(catalogs, false);
            yield return updateHandle;

            LastCheckSucceeded = HandleSucceeded(updateHandle);
            if (!LastCheckSucceeded)
            {
                LastError = "Remote catalog update failed.";
                Debug.LogWarning("[RemoteContentService] " + LastError);
            }

            ReleaseIfValid(updateHandle);
            IsChecking = false;
        }

        private static bool HandleSucceeded(AsyncOperationHandle handle)
        {
            return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
        }

        private static bool HandleSucceeded<T>(AsyncOperationHandle<T> handle)
        {
            return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;
        }

        private static void ReleaseIfValid(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        private static void ReleaseIfValid<T>(AsyncOperationHandle<T> handle)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
    }
}
