using UnityEngine;

namespace VRRocket
{
    /// <summary>
    /// Holds the body tube upright during assembly (SPEC.md section 8.2). M1 scope: clamp the tube so it cannot be grabbed.
    /// Release, whole-rocket carry and re-capture arrive in M7.
    /// </summary>
    public sealed class AssemblyStand : MonoBehaviour
    {
        [SerializeField, Tooltip("Where the tube's origin (centre of the bottom rim) sits while clamped.")] Transform m_TubeAnchor;
        [SerializeField] RocketPart m_Tube;

        public Transform tubeAnchor => m_TubeAnchor;
        public RocketPart tube => m_Tube;
        public bool isHolding { get; private set; }

        public void Configure(Transform tubeAnchor, RocketPart tube)
        {
            m_TubeAnchor = tubeAnchor;
            m_Tube = tube;
        }

        void Start()
        {
            if (m_Tube != null) Hold(m_Tube);
        }

        /// <summary>Clamps the tube at the anchor. It becomes kinematic and, if it has a grab interactable, ungrabbable.</summary>
        public void Hold(RocketPart tube)
        {
            m_Tube = tube;
            var t = tube.transform;
            t.SetParent(m_TubeAnchor, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            var body = tube.body;
            if (body != null)
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            var grab = tube.grabInteractable;
            if (grab != null) grab.enabled = false;
            isHolding = true;
        }
    }
}
