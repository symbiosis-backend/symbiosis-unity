using System;
using System.IO;
using System.Reflection;
using Dynasty.Legacy.Symbioz;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dynasty.Legacy.Symbioz.Editor
{
    public static class SymbiozHouseValidation
    {
        private const string ExteriorAssetPath = "Assets/Resources/SymbiozTiles/Buildings/small-house-exterior.png";
        private const string AutoRunRequestPath = "Library/SymbiozHouseValidation.request";

        [InitializeOnLoadMethod]
        private static void RunRequestedValidationAfterReload()
        {
            if (!File.Exists(AutoRunRequestPath))
                return;

            File.Delete(AutoRunRequestPath);
            EditorApplication.delayCall += RunSmallHouseValidation;
        }

        [MenuItem("Dynasty/Symbioz/Validate Small House Layers")]
        public static void RunSmallHouseValidation()
        {
            try
            {
                ValidateExteriorAsset();
                ValidateRuntimeHouseLayers();
                Debug.Log("[SymbiozHouseValidation] Small house exterior/interior layer validation passed.");

                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SymbiozHouseValidation] Validation failed: " + ex);

                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        private static void ValidateExteriorAsset()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ExteriorAssetPath);
            if (texture == null)
                throw new InvalidOperationException("Missing exterior asset: " + ExteriorAssetPath);

            if (texture.width < 512 || texture.height < 512)
                throw new InvalidOperationException($"Exterior asset is too small: {texture.width}x{texture.height}");
        }

        private static void ValidateRuntimeHouseLayers()
        {
            Scene validationScene = EditorSceneManager.NewPreviewScene();

            GameObject prototypeObject = new GameObject("SymbiozHouseValidationPrototype");
            SymbiozFlagshipPrototype prototype = prototypeObject.AddComponent<SymbiozFlagshipPrototype>();
            GameObject objectsObject = new GameObject("ArchitectObjects");
            SceneManager.MoveGameObjectToScene(prototypeObject, validationScene);
            SceneManager.MoveGameObjectToScene(objectsObject, validationScene);

            try
            {
                Type prototypeType = typeof(SymbiozFlagshipPrototype);
                SetPrivateField(prototypeType, prototype, "objectsRoot", objectsObject.transform);
                InvokePrivate(prototypeType, prototype, "LoadTileMaterials");

                Type objectKindType = prototypeType.GetNestedType("ObjectKind", BindingFlags.NonPublic);
                object smallHouseKind = Enum.Parse(objectKindType, "SmallHouse");
                MethodInfo createEstateBuilding = RequireMethod(prototypeType, "CreateEstateBuilding");
                var house = (GameObject)createEstateBuilding.Invoke(prototype, new object[] { new Vector2Int(120, 120), smallHouseKind });

                if (house == null)
                    throw new InvalidOperationException("CreateEstateBuilding returned null for SmallHouse.");

                Renderer exterior = FindRenderer(house.transform, "EstateExteriorSprite");
                if (exterior == null)
                    throw new InvalidOperationException("SmallHouse has no EstateExteriorSprite renderer.");

                int interiorRendererCount = CountRenderers(house.transform, "Interior", expectEnabled: false);
                if (interiorRendererCount < 12)
                    throw new InvalidOperationException("SmallHouse interior was not created or is visible before entering.");

                InvokePrivateStatic(prototypeType, "SetEstateExteriorCoverVisible", house.transform, false);
                InvokePrivateStatic(prototypeType, "SetEstateInteriorVisible", house.transform, true);

                if (exterior.enabled)
                    throw new InvalidOperationException("Exterior sprite is still visible after entering the house.");

                int visibleInteriorCount = CountRenderers(house.transform, "Interior", expectEnabled: true);
                if (visibleInteriorCount != interiorRendererCount)
                    throw new InvalidOperationException("Not all interior renderers became visible after entering.");

                InvokePrivateStatic(prototypeType, "SetEstateExteriorCoverVisible", house.transform, true);
                InvokePrivateStatic(prototypeType, "SetEstateInteriorVisible", house.transform, false);

                if (!exterior.enabled)
                    throw new InvalidOperationException("Exterior sprite did not come back after exiting the house.");

                CountRenderers(house.transform, "Interior", expectEnabled: false);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(validationScene);
            }
        }

        private static void SetPrivateField(Type type, object instance, string name, object value)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(type.FullName, name);

            field.SetValue(instance, value);
        }

        private static void InvokePrivate(Type type, object instance, string name)
        {
            RequireMethod(type, name).Invoke(instance, null);
        }

        private static void InvokePrivateStatic(Type type, string name, params object[] args)
        {
            RequireMethod(type, name).Invoke(null, args);
        }

        private static MethodInfo RequireMethod(Type type, string name)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(type.FullName, name);

            return method;
        }

        private static Renderer FindRenderer(Transform root, string objectName)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name == objectName)
                    return renderers[i];
            }

            return null;
        }

        private static int CountRenderers(Transform root, string namePrefix, bool expectEnabled)
        {
            int count = 0;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.gameObject.name.StartsWith(namePrefix, StringComparison.Ordinal))
                    continue;

                count++;
                if (renderer.enabled != expectEnabled)
                    throw new InvalidOperationException($"{renderer.gameObject.name} enabled={renderer.enabled}, expected {expectEnabled}.");
            }

            return count;
        }
    }
}
