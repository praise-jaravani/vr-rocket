using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRRocket
{
    /// <summary>
    /// Identity and state of one rocket part (SPEC.md section 10.2).
    /// The guided attach mechanic lives in <see cref="GuideGrabTransformer"/>; this component records what the part is, where it stands,
    /// which hand held it last (for haptics) and where it lives when loose (for detaching).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RocketPart : MonoBehaviour
    {
        [SerializeField] PartType m_PartType = PartType.TailFin;
        [SerializeField, Tooltip("1 to 3 for fins and flaps, 1 for the rest.")] int m_PartId = 1;

        XRGrabInteractable m_Grab;
        Rigidbody m_Rigidbody;
        bool m_HookedGrab;

        public PartType partType => m_PartType;
        public int partId => m_PartId;
        public PartState state { get; private set; } = PartState.Free;
        public AttachPoint attachedTo { get; private set; }

        /// <summary>The parent this part returns to when it is detached from the rocket (its tray group).</summary>
        public Transform homeParent { get; private set; }

        /// <summary>The interactor that is holding, or most recently held, this part. Used to send haptics after release.</summary>
        public IXRInteractor lastHoldingInteractor { get; private set; }

        public XRGrabInteractable grabInteractable => m_Grab != null ? m_Grab : (m_Grab = GetComponent<XRGrabInteractable>());
        public Rigidbody body => m_Rigidbody != null ? m_Rigidbody : (m_Rigidbody = GetComponent<Rigidbody>());
        public bool isHeld => grabInteractable != null && grabInteractable.isSelected;

        /// <summary>Id used in reports, for example "WingFlap_2".</summary>
        public string displayId => m_PartType + "_" + m_PartId;

        public void Configure(PartType type, int id)
        {
            m_PartType = type;
            m_PartId = id;
        }

        void Awake()
        {
            homeParent = transform.parent;
            HookGrab();
        }

        void HookGrab()
        {
            if (m_HookedGrab || grabInteractable == null) return;
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            m_HookedGrab = true;
        }

        void OnDestroy()
        {
            if (m_HookedGrab && m_Grab != null) m_Grab.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            lastHoldingInteractor = args.interactorObject;
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
