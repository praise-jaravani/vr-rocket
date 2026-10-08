using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRRocket
{
    /// <summary>
    /// Identity and state of one loose rocket part (SPEC.md section 10.2).
    /// The guided attach mechanic lives in the guide grab transformer; this component only records what the part is and where it stands.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RocketPart : MonoBehaviour
    {
        [SerializeField] PartType m_PartType = PartType.TailFin;
        [SerializeField, Tooltip("1 to 3 for fins and flaps, 1 for the rest.")] int m_PartId = 1;

        XRGrabInteractable m_Grab;
        Rigidbody m_Rigidbody;

        public PartType partType => m_PartType;
        public int partId => m_PartId;
        public PartState state { get; private set; } = PartState.Free;
        public AttachPoint attachedTo { get; private set; }

        public XRGrabInteractable grabInteractable => m_Grab != null ? m_Grab : (m_Grab = GetComponent<XRGrabInteractable>());
        public Rigidbody body => m_Rigidbody != null ? m_Rigidbody : (m_Rigidbody = GetComponent<Rigidbody>());

        /// <summary>Id used in reports, for example "WingFlap_2".</summary>
        public string displayId => m_PartType + "_" + m_PartId;

        public void Configure(PartType type, int id)
        {
            m_PartType = type;
            m_PartId = id;
        }

        internal void SetGuided()
        {
            state = PartState.Guided;
        }

        internal void SetAttached(AttachPoint point)
        {
            attachedTo = point;
            state = PartState.Attached;
        }

        internal void SetFree()
        {
            attachedTo = null;
            state = PartState.Free;
        }
    }
}
