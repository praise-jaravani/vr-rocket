using UnityEngine;

namespace VRRocket
{
    /// <summary>
    /// Root of the RocketWorkstation prefab (SPEC.md section 8.1). Its origin sits on the desk surface. Everything rocket-related
    /// is a child of <see cref="scaledRoot"/>, which is scaled by the tuning asset's rocketScale.
    /// </summary>
    public sealed class RocketWorkstation : MonoBehaviour
    {
        [SerializeField] AssemblyTuning m_Tuning;
        [SerializeField] Transform m_ScaledRoot;
        [SerializeField] AssemblyStand m_Stand;
        [SerializeField] PartRespawner m_Respawner;

        public AssemblyTuning tuning => m_Tuning;
        public Transform scaledRoot => m_ScaledRoot;
        public AssemblyStand stand => m_Stand;
        public PartRespawner respawner => m_Respawner;

        public void Configure(AssemblyTuning tuning, Transform scaledRoot, AssemblyStand stand, PartRespawner respawner)
        {
            m_Tuning = tuning;
            m_ScaledRoot = scaledRoot;
            m_Stand = stand;
            m_Respawner = respawner;
        }

        void Awake()
        {
            ApplyScale();
        }

        /// <summary>Applies rocketScale from the tuning asset to the scaled root.</summary>
        public void ApplyScale()
        {
            if (m_ScaledRoot == null) return;
            var s = m_Tuning != null ? m_Tuning.rocketScale : 1f;
            m_ScaledRoot.localScale = Vector3.one * s;
        }
    }
}
