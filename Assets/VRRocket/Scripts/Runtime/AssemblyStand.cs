using UnityEngine;

namespace VRRocket
{
    /// <summary>
    /// Holds the body tube upright during assembly (SPEC.md section 8.2). The clamp is an <see cref="AttachPoint"/> that accepts the
    /// tube, so the tube uses the same guided mechanic as every part: the stand releases it at PrototypeComplete (gating), lifting
    /// it past the guide length takes it out, and bringing it back within the capture radius snaps it back in.
    /// </summary>
    public sealed class AssemblyStand : MonoBehaviour
    {
        [SerializeField, Tooltip("The clamp attach point. The tube's origin (centre of the bottom rim) sits here while clamped.")] AttachPoint m_Clamp;
        [SerializeField] RocketPart m_Tube;

        public AttachPoint clamp => m_Clamp;
        public Transform tubeAnchor => m_Clamp != null ? m_Clamp.transform : null;
        public RocketPart tube => m_Tube;
        public bool isHolding => m_Clamp != null && m_Clamp.isOccupied;

        public void Configure(AttachPoint clamp, RocketPart tube)
        {
            m_Clamp = clamp;
            m_Tube = tube;
        }

        void Start()
        {
            if (m_Tube != null && !isHolding) Hold(m_Tube);
        }

        /// <summary>Clamps the tube at once: kinematic, parented to the clamp, attached to it.</summary>
        public void Hold(RocketPart tube)
        {
            if (m_Clamp == null || tube == null) return;
            if (m_Clamp.isOccupied && m_Clamp.attachedPart != tube) m_Clamp.ClearAttached();
            m_Tube = tube;
            var body = tube.body;
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
            }
            var t = tube.transform;
            t.SetParent(m_Clamp.transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            m_Clamp.SetAttached(tube);
            tube.SetAttached(m_Clamp);
        }
    }
}
