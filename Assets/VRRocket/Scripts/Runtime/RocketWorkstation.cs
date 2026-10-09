using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRRocket
{
    /// <summary>
    /// Root of the RocketWorkstation prefab (SPEC.md section 8.1). Its origin sits on the desk surface. Everything rocket-related
    /// is a child of <see cref="scaledRoot"/>, which is scaled by the tuning asset's rocketScale. Also knows how to spawn a fresh kit
    /// (tube in the stand, nine parts on the tray) for <see cref="RocketAssembly.BeginNewBuild"/>.
    /// </summary>
    public sealed class RocketWorkstation : MonoBehaviour
    {
        [Serializable]
        public struct TraySlot
        {
            public PartType type;
            public int id;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        [SerializeField] AssemblyTuning m_Tuning;
        [SerializeField] Transform m_ScaledRoot;
        [SerializeField] AssemblyStand m_Stand;
        [SerializeField] PartRespawner m_Respawner;
        [SerializeField] Transform m_PartsTray;
        [Header("Prefabs for a fresh kit")]
        [SerializeField] GameObject m_TubePrefab;
        [SerializeField] GameObject m_TailFinPrefab;
        [SerializeField] GameObject m_WingFlapPrefab;
        [SerializeField] GameObject m_NoseConePrefab;
        [SerializeField] GameObject m_MotorPrefab;
        [SerializeField] GameObject m_MotorCapPrefab;

        readonly List<TraySlot> m_TrayLayout = new List<TraySlot>();

        public AssemblyTuning tuning => m_Tuning;
        public Transform scaledRoot => m_ScaledRoot;
        public AssemblyStand stand => m_Stand;
        public PartRespawner respawner => m_Respawner;
        public Transform partsTray => m_PartsTray;
        public IReadOnlyList<TraySlot> trayLayout => m_TrayLayout;

        public void Configure(AssemblyTuning tuning, Transform scaledRoot, AssemblyStand stand, PartRespawner respawner)
        {
            m_Tuning = tuning;
            m_ScaledRoot = scaledRoot;
            m_Stand = stand;
            m_Respawner = respawner;
        }

        public void ConfigureKit(Transform partsTray, GameObject tube, GameObject fin, GameObject flap, GameObject nose, GameObject motor, GameObject cap)
        {
            m_PartsTray = partsTray;
            m_TubePrefab = tube; m_TailFinPrefab = fin; m_WingFlapPrefab = flap; m_NoseConePrefab = nose; m_MotorPrefab = motor; m_MotorCapPrefab = cap;
        }

        void Awake()
        {
            ApplyScale();
            CaptureTrayLayout();
        }

        /// <summary>Applies rocketScale from the tuning asset to the scaled root.</summary>
        public void ApplyScale()
        {
            if (m_ScaledRoot == null) return;
            var s = m_Tuning != null ? m_Tuning.rocketScale : 1f;
            m_ScaledRoot.localScale = Vector3.one * s;
        }

        /// <summary>Remembers where each loose part starts on the tray, so a fresh kit lands in the same places.</summary>
        public void CaptureTrayLayout()
        {
            m_TrayLayout.Clear();
            if (m_PartsTray == null) return;
            foreach (var p in m_PartsTray.GetComponentsInChildren<RocketPart>(true))
            {
                if (p.transform.parent != m_PartsTray) continue;
                m_TrayLayout.Add(new TraySlot { type = p.partType, id = p.partId, localPosition = p.transform.localPosition, localRotation = p.transform.localRotation });
            }
        }

        GameObject PrefabFor(PartType type)
        {
            switch (type)
            {
                case PartType.BodyTube: return m_TubePrefab;
                case PartType.TailFin: return m_TailFinPrefab;
                case PartType.WingFlap: return m_WingFlapPrefab;
                case PartType.NoseCone: return m_NoseConePrefab;
                case PartType.Motor: return m_MotorPrefab;
                case PartType.MotorCap: return m_MotorCapPrefab;
                default: return null;
            }
        }

        public bool canSpawnKit => m_TubePrefab != null && m_TailFinPrefab != null && m_WingFlapPrefab != null && m_NoseConePrefab != null && m_MotorPrefab != null && m_MotorCapPrefab != null && m_PartsTray != null && m_TrayLayout.Count > 0;

        /// <summary>Instantiates a new tube (returned, not yet in the stand) and nine new parts on the tray in their original places.</summary>
        public RocketPart SpawnKit(List<RocketPart> newParts)
        {
            if (!canSpawnKit) throw new InvalidOperationException("RocketWorkstation has no kit prefabs or tray layout to spawn from.");
            var tubeGo = Instantiate(m_TubePrefab, m_ScaledRoot);
            tubeGo.name = "BodyTube";
            var tube = tubeGo.GetComponent<RocketPart>();
            foreach (var slot in m_TrayLayout)
            {
                var go = Instantiate(PrefabFor(slot.type), m_PartsTray);
                go.name = slot.type + "_" + slot.id;
                go.transform.localPosition = slot.localPosition;
                go.transform.localRotation = slot.localRotation;
                var part = go.GetComponent<RocketPart>();
                part.Configure(slot.type, slot.id);
                newParts.Add(part);
            }
            return tube;
        }
    }
}
