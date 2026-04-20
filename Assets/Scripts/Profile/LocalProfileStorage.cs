using System;
using System.IO;
using UnityEngine;

namespace MahjongGame
{
    public sealed class LocalProfileStorage
    {
        private const string FileName = "profile.json";

        private readonly string filePath;

        public string FilePath => filePath;

        public LocalProfileStorage()
        {
            filePath = Path.Combine(Application.persistentDataPath, FileName);
        }

        public bool Exists()
        {
            return File.Exists(filePath);
        }

        public void Save(PlayerProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("[LocalProfileStorage] Save failed: profile is null.");
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(profile, true);
                File.WriteAllText(filePath, json);

                Debug.Log($"[LocalProfileStorage] Profile saved: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalProfileStorage] Save failed: {ex.Message}");
            }
        }

        public PlayerProfile Load()
        {
            if (!Exists())
            {
                Debug.LogWarning("[LocalProfileStorage] Load skipped: profile file does not exist.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning("[LocalProfileStorage] Load failed: profile file is empty.");
                    return null;
                }

                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);

                if (profile == null)
                {
                    Debug.LogWarning("[LocalProfileStorage] Load failed: parsed profile is null.");
                    return null;
                }

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalProfileStorage] Load failed: {ex.Message}");
                return null;
            }
        }

        public void Delete()
        {
            if (!Exists())
                return;

            try
            {
                File.Delete(filePath);
                Debug.Log($"[LocalProfileStorage] Profile deleted: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalProfileStorage] Delete failed: {ex.Message}");
            }
        }
    }
}