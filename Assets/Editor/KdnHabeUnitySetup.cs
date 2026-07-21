using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KdnHabeUnitySetup
{
    [MenuItem("KDN HABE/Setup Movement Package")]
    public static void SetupMovementPackage()
    {
        GameObject player = FindPlayer();
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<CharacterController>(player);
            controller.height = 2f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 1f, 0f);
        }

        if (player.GetComponent<PlayerMovement>() == null)
        {
            Undo.AddComponent<PlayerMovement>(player);
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 3f, -6f);
        }

        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = Undo.AddComponent<CameraFollow>(camera.gameObject);
        }

        SerializedObject serializedFollow = new SerializedObject(follow);
        SerializedProperty target = serializedFollow.FindProperty("target");
        if (target != null)
        {
            target.objectReferenceValue = player.transform;
            serializedFollow.ApplyModifiedProperties();
        }

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("KDN HABE movement package setup complete. Test WASD, LeftShift, and Space in Play Mode.");
    }

    private static GameObject FindPlayer()
    {
        PlayerMovement movement = Object.FindAnyObjectByType<PlayerMovement>();
        if (movement != null)
        {
            return movement.gameObject;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        GameObject namedPlayer = GameObject.Find("Player");
        if (namedPlayer != null)
        {
            return namedPlayer;
        }

        GameObject created = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(created, "Create Player");
        created.transform.position = Vector3.zero;
        return created;
    }
}
