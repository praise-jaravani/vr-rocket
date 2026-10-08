using UnityEngine;

namespace VRRocket
{
    /// <summary>
    /// One attach point on the body tube (SPEC.md section 4.4). Generated from attach_points.json by the editor script.
    /// Knows what it accepts, which axis the guide runs along, whether it is occupied and whether gating currently allows it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttachPoint : MonoBehaviour
    {
        [SerializeField] string m_PointName = "";
        [SerializeField] PartType m_Accepts = PartType.TailFin;
        [SerializeField, Tooltip("Yaw of the point around the tube axis, from attach_points.json.")] float m_YawDegrees;
        [SerializeField, Tooltip("Optional emissive glow mesh for the approach feedback.")] Renderer m_GlowRenderer;

        /// <summary>Set by the assembly state machine. When false the point behaves as a rejection: no glow, no snap.</summary>
        public bool gatingAllows { get; set; } = true;

        public string pointName => string.IsNullOrEmpty(m_PointName) ? name : m_PointName;
        public PartType accepts => m_Accepts;
        public float yawDegrees => m_YawDegrees;
        public Renderer glowRenderer => m_GlowRenderer;
        public RocketPart attachedPart { get; private set; }
        public bool isOccupied => attachedPart != null;

        /// <summary>True for fins and flaps: the guide runs along local +Z (outward). False for the seats, where it runs along the tube axis.</summary>
        public bool isSlot => m_Accepts == PartType.TailFin || m_Accepts == PartType.WingFlap;

        /// <summary>Is this a Low (wrong) flap slot? Only meaningful when accepts == WingFlap.</summary>
        public bool isLowFlapSlot => pointName.StartsWith("FlapSlot_Low");

        /// <summary>World-space direction the guide runs along, pointing away from the seated position.</summary>
        public Vector3 GuideAxisWorld
        {
            get
            {
                if (isSlot) return transform.forward;
                // Seats: the guide extends away from the tube. Nose above (+Y), motor and cap below (-Y).
                return m_Accepts == PartType.NoseCone ? transform.up : -transform.up;
            }
        }

        /// <summary>Pose a part takes when seated here.</summary>
        public Pose SeatedPoseWorld => new Pose(transform.position, transform.rotation);

        /// <summary>Does this point currently accept the given part?</summary>
        public bool Accepts(RocketPart part)
        {
            return part != null && part.partType == m_Accepts && !isOccupied && gatingAllows;
        }

        public void Configure(string pointName, PartType accepts, float yawDegrees)
        {
            m_PointName = pointName;
            m_Accepts = accepts;
            m_YawDegrees = yawDegrees;
        }

        public void SetGlowRenderer(Renderer renderer)
        {
            m_GlowRenderer = renderer;
        }

        internal void SetAttached(RocketPart part)
        {
            attachedPart = part;
        }

        internal void ClearAttached()
        {
            attachedPart = null;
        }

        /// <summary>Maps an attach point name from attach_points.json to the part type it accepts.</summary>
        public static PartType AcceptsFromName(string pointName)
        {
            if (pointName.StartsWith("FinSlot")) return PartType.TailFin;
            if (pointName.StartsWith("FlapSlot")) return PartType.WingFlap;
            if (pointName == "NoseSeat") return PartType.NoseCone;
            if (pointName == "MotorSeat") return PartType.Motor;
            if (pointName == "CapSeat") return PartType.MotorCap;
            throw new System.ArgumentException("Unknown attach point name: " + pointName);
        }
    }
}
