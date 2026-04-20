using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OkeyGame
{
    [DisallowMultipleComponent]
    public sealed class OkeyDiscardZone : MonoBehaviour, IDropHandler
    {
        public OkeyTurnManager TurnManager;

        public void OnDrop(PointerEventData eventData)
        {
            if (TurnManager == null)
            {
                Debug.LogWarning("[OkeyDiscardZone] TurnManager is not assigned.");
                return;
            }

            if (eventData == null || eventData.pointerDrag == null)
            {
                Debug.LogWarning("[OkeyDiscardZone] Drop ignored: pointerDrag is null.");
                return;
            }

            GameObject draggedObject = eventData.pointerDrag;
            OkeyTileInstance tile = draggedObject.GetComponent<OkeyTileInstance>();
            if (tile == null)
            {
                Debug.LogWarning("[OkeyDiscardZone] Dropped object has no OkeyTileInstance.");
                return;
            }

            OkeyTileDrag drag = draggedObject.GetComponent<OkeyTileDrag>();

            bool discardSucceeded = TryDiscard(tile);

            if (discardSucceeded)
            {
                if (drag != null)
                    drag.MarkDropHandled();

                Debug.Log($"[OkeyDiscardZone] Discard accepted: {tile.name}");
            }
            else
            {
                Debug.LogWarning($"[OkeyDiscardZone] Discard rejected: {tile.name}");
            }
        }

        private bool TryDiscard(OkeyTileInstance tile)
        {
            if (tile == null || TurnManager == null)
                return false;

            MethodInfo method = typeof(OkeyTurnManager).GetMethod(
                "TryDiscardForLocalPlayer",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (method == null)
            {
                Debug.LogWarning("[OkeyDiscardZone] Method TryDiscardForLocalPlayer was not found on OkeyTurnManager.");
                return false;
            }

            object result = method.Invoke(TurnManager, new object[] { tile });

            if (method.ReturnType == typeof(bool))
                return result is bool ok && ok;

            // Если метод void, пытаемся определить успех по состоянию объекта.
            // Обычно после успешного discard камень либо уходит из руки,
            // либо меняет parent, либо отключается.
            if (!tile.gameObject.activeInHierarchy)
                return true;

            return tile.transform.parent != null && tile.transform.parent != transform.parent;
        }
    }
}