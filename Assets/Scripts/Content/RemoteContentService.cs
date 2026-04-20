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

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LastError = "Could not check remote content size for label: " + label;
                Debug.LogWarning("[RemoteContentService] " + LastError);
                Addressables.Release(sizeHandle);
                yield break;
            }

            long size = sizeHandle.Result;
            Addressables.Release(sizeHandle);

            if (size <= 0)
                yield break;

            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(label);
            yield return downloadHandle;

            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LastError = "Could not download remote content label: " + label;
                Debug.LogWarning("[RemoteContentService] " + LastError);
            }

            Addressables.Release(downloadHandle);
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

            if (initHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LastError = "Addressables initialization failed.";
                Debug.LogWarning("[RemoteContentService] " + LastError);
                Addressables.Release(initHandle);
                IsChecking = false;
                yield break;
            }

            Addressables.Release(initHandle);

            AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates(false);
            yield return checkHandle;

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LastError = "Remote catalog check failed.";
                Debug.LogWarning("[RemoteContentService] " + LastError);
                Addressables.Release(checkHandle);
                IsChecking = false;
                yield break;
            }

            List<string> catalogs = checkHandle.Result;
            if (catalogs == null || catalogs.Count == 0)
            {
                LastCheckSucceeded = true;
                Addressables.Release(checkHandle);
                IsChecking = false;
                yield break;
            }

            Addressables.Release(checkHandle);

            AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs(catalogs, false);
            yield return updateHandle;

            LastCheckSucceeded = updateHandle.Status == AsyncOperationStatus.Succeeded;
            if (!LastCheckSucceeded)
            {
                LastError = "Remote catalog update failed.";
                Debug.LogWarning("[RemoteContentService] " + LastError);
            }

            Addressables.Release(updateHandle);
            IsChecking = false;
        }
    }
}
