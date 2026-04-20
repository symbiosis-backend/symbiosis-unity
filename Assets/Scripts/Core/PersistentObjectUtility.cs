using UnityEngine;

public static class PersistentObjectUtility
{
    public static void DontDestroyOnLoad(GameObject gameObject)
    {
        if (gameObject == null)
            return;

        Transform transform = gameObject.transform;
        if (transform.parent != null)
            transform.SetParent(null, true);

        UnityEngine.Object.DontDestroyOnLoad(gameObject);
    }
}
