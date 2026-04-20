using System.Collections.Generic;
using UnityEngine;

namespace OkeyGame
{
    public sealed class OkeyLocalViewGate : MonoBehaviour
    {
        [Header("Local Player")]
        [Range(0, 3)]
        public int LocalSeatIndex = 0;

        [Header("Rack Objects (Seat 0..3)")]
        public List<GameObject> AllRacks = new List<GameObject>();

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            if (AllRacks == null || AllRacks.Count == 0)
            {
                Debug.LogWarning("[OkeyLocalViewGate] AllRacks list is empty.");
                return;
            }

            for (int i = 0; i < AllRacks.Count; i++)
            {
                GameObject rack = AllRacks[i];
                if (rack == null)
                    continue;

                bool active = (i == LocalSeatIndex);
                rack.SetActive(active);
            }

            Debug.Log($"[OkeyLocalViewGate] Local seat = {LocalSeatIndex}");
        }

        public void SetLocalSeat(int seatIndex)
        {
            LocalSeatIndex = Mathf.Clamp(seatIndex, 0, 3);
            Apply();
        }
    }
}