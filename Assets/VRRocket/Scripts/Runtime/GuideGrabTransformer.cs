using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace VRRocket
{
    /// <summary>
    /// The guided attach mechanic of SPEC.md section 5.2, shared by every part.
    /// Free: the part follows the hand. Approach: accepting points within glowRadius glow. Guided: within captureRadius and
    /// orientation tolerance the part locks to the point's axis and slides between the seat and guideLength. Break away: sideways
    /// drift or pulling past the end releases the guide. Seat: releasing while guided animates to the seat and attaches.
    /// Remove: grabbing an attached part re-enters the guide at depth 0; pulling past guideLength detaches it.
    /// Registered in Start, after XRGrabInteractable adds its default transformer in Awake, so this one runs last and wins.
    /// </summary>
    [RequireComponent(typeof(RocketPart))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class GuideGrabTransformer : XRBaseGrabTransformer
    {
        [SerializeField, Tooltip("Optional. When empty the tuning asset is taken from the RocketWorkstation above this part.")]
        AssemblyTuning m_Tuning;

        protected override RegistrationMode registrationMode => RegistrationMode.SingleAndMultiple;

        RocketPart m_Part;
        XRGrabInteractable m_Grab;
        Rigidbody m_Body;
        Collider[] m_OwnColliders;
        readonly List<Collider> m_IgnoredColliders = new List<Collider>(32);

        AttachPoint m_Point;
        Quaternion m_SeatedRotation;
        bool m_Guided;
        bool m_FromAttached;
        Coroutine m_SeatRoutine;

        public AssemblyTuning tuning
        {
            get
            {
                if (m_Tuning == null)
                {
                    var ws = GetComponentInParent<RocketWorkstation>();
                    if (ws != null) m_Tuning = ws.tuning;
                }
                return m_Tuning;
            }
            set => m_Tuning = value;
        }

        public bool isGuided => m_Guided;
        public AttachPoint guidePoint => m_Point;

        void Awake()
        {
            m_Part = GetComponent<RocketPart>();
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Body = GetComponent<Rigidbody>();
            m_OwnColliders = GetComponentsInChildren<Collider>();
            m_RocketGrab = m_Grab as RocketGrabInteractable;
            if (m_RocketGrab != null) m_RocketGrab.selectEntering += OnSelectEntering;
            else Debug.LogError("GuideGrabTransformer needs a RocketGrabInteractable so it can hook the grab before the rigidbody state is recorded.", this);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        RocketGrabInteractable m_RocketGrab;

        protected override void Start()
        {
            // XRGrabInteractable only adds its default XRGeneralGrabTransformer when no single transformer is registered by the
            // first update. This one registers in Start, which is earlier, so make sure the general transformer (the one that
            // makes the part follow the hand) is registered first. Ours then runs after it and overrides the pose while guided.
            var general = GetComponent<XRGeneralGrabTransformer>();
            if (general == null) general = gameObject.AddComponent<XRGeneralGrabTransformer>();
            var registered = false;
            for (var i = 0; i < m_Grab.singleGrabTransformersCount; i++)
                if (ReferenceEquals(m_Grab.GetSingleGrabTransformerAt(i), general)) registered = true;
            if (!registered) m_Grab.AddSingleGrabTransformer(general);
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_Grab == null) return;
            if (m_RocketGrab != null) m_RocketGrab.selectEntering -= OnSelectEntering;
            m_Grab.selectExited.RemoveListener(OnSelectExited);
        }

        float WorldScale => m_Part.transform.lossyScale.x;

        // ---- grab lifecycle ----

        void OnSelectEntering(SelectEnterEventArgs args)
        {
            // Runs before XRGrabInteractable.Grab() records the body's kinematic state, so an attached part must be dynamic here
            // or the grab interactable restores it to kinematic on release and refuses to throw it.
            if (m_Grab.interactorsSelecting.Count > 0) return; // second hand joining: keep state
            m_Grab.throwOnDetach = true;
            if (m_SeatRoutine != null)
            {
                StopCoroutine(m_SeatRoutine);
                m_SeatRoutine = null;
                FinishSeat();
            }
            if (m_Part.state == PartState.Attached && m_Part.attachedTo != null)
            {
                m_Point = m_Part.attachedTo;
                m_SeatedRotation = transform.rotation;
                m_Body.isKinematic = false;
                m_Guided = true;
                m_FromAttached = true;
                m_Part.SetGuided();
                // collisions with the rocket are already ignored from when it seated
            }
            else
            {
                m_Guided = false;
                m_FromAttached = false;
                m_Point = null;
            }
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_Grab.interactorsSelecting.Count > 0) return; // still held by the other hand
            if (m_Guided)
            {
                // XRGrabInteractable throws in the manager's LateUpdate, after this call. The part is about to become
                // kinematic for the seat animation, so suppress the throw for this release (re-enabled on the next grab).
                m_Grab.throwOnDetach = false;
                m_Guided = false;
                m_SeatRoutine = StartCoroutine(SeatRoutine());
            }
            else if (m_Part.state != PartState.Attached)
            {
                m_Part.SetFree();
                // XRGrabInteractable restores the parent it saw at grab time, which for a part pulled off the rocket is the
                // attach point. A loose part belongs to its tray group.
                if (transform.parent != m_Part.homeParent) transform.SetParent(m_Part.homeParent, true);
            }
        }

        // ---- per-frame ----

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            var t = tuning;
            if (t == null) return;
            var s = WorldScale;

            if (!m_Guided)
            {
                var glowR = t.glowRadius * s;
                var captureR = t.captureRadius * s;
                AttachPoint best = null;
                var bestDist = float.MaxValue;
                var points = AttachPoint.active;
                for (var i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    if (!p.Accepts(m_Part)) continue;
                    var d = Vector3.Distance(targetPose.position, p.transform.position);
                    if (d >= glowR) continue;
                    p.RequestGlow(1f - d / glowR);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = p;
                    }
                }
                if (best != null && bestDist <= captureR && OrientationOk(best, targetPose.rotation, t))
                    Engage(best, targetPose.rotation);
            }

            if (m_Guided)
            {
                var seat = m_Point.transform.position;
                var axis = m_Point.GuideAxisWorld;
                var depth = GuideMath.Depth(targetPose.position, seat, axis);
                var sideways = GuideMath.Sideways(targetPose.position, seat, axis);
                var guideLength = t.GuideLengthFor(m_Part.partType) * s;
                var breakRadius = t.breakRadius * s;

                if (m_FromAttached)
                {
                    // An attached part only comes off by being pulled out past the end of the guide.
                    if (depth > guideLength)
                    {
                        Detach();
                        return;
                    }
                }
                else if (GuideMath.ShouldBreak(depth, sideways, guideLength, breakRadius))
                {
                    ReleaseGuide();
                    return;
                }

                var fraction = m_Part.partType == PartType.NoseCone ? t.noseMagnetism : 1f;
                targetPose.position = GuideMath.GuidedPosition(seat, axis, depth, guideLength, fraction);
                targetPose.rotation = m_SeatedRotation;
                // keep the point lit while guided
                m_Point.RequestGlow(1f);
            }
        }

        bool OrientationOk(AttachPoint point, Quaternion heldRotation, AssemblyTuning t)
        {
            var tolerance = t.OrientationToleranceFor(m_Part.partType);
            var partAxis = point.isSlot ? heldRotation * Vector3.forward : heldRotation * Vector3.up;
            return GuideMath.WithinOrientation(partAxis, point.ReferenceAxisWorld, tolerance);
        }

        // ---- transitions ----

        void Engage(AttachPoint point, Quaternion heldRotation)
        {
            m_Point = point;
            m_SeatedRotation = GuideMath.SeatedRotation(m_Part.partType, point.transform.rotation, heldRotation);
            m_Guided = true;
            m_FromAttached = false;
            m_Part.SetGuided();
            SetRocketCollisionsIgnored(true);
            AssemblyEvents.RaiseGuideEngaged(m_Part, point);
        }

        void ReleaseGuide()
        {
            var point = m_Point;
            m_Guided = false;
            m_Point = null;
            m_Part.SetFree();
            SetRocketCollisionsIgnored(false);
            AssemblyEvents.RaiseGuideReleased(m_Part, point);
        }

        void Detach()
        {
            var point = m_Point;
            m_Guided = false;
            m_FromAttached = false;
            m_Point = null;
            point.ClearAttached();
            m_Part.SetFree();
            transform.SetParent(m_Part.homeParent, true);
            SetRocketCollisionsIgnored(false);
            AssemblyEvents.RaisePartRemoved(m_Part, point);
        }

        IEnumerator SeatRoutine()
        {
            var t = tuning;
            var duration = t != null ? t.SeatDurationFor(m_Part.partType) : 0.12f;
            if (!m_Body.isKinematic)
            {
                m_Body.linearVelocity = Vector3.zero;
                m_Body.angularVelocity = Vector3.zero;
            }
            m_Body.isKinematic = true;
            var startPos = transform.position;
            var startRot = transform.rotation;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                var seat = m_Point.transform.position;
                transform.SetPositionAndRotation(Vector3.Lerp(startPos, seat, k), Quaternion.Slerp(startRot, m_SeatedRotation, k));
                yield return null;
            }
            m_SeatRoutine = null;
            FinishSeat();
            AssemblyEvents.RaisePartSeated(m_Part, m_Point);
        }

        void FinishSeat()
        {
            if (m_Point == null) return;
            m_Body.isKinematic = true;
            transform.SetPositionAndRotation(m_Point.transform.position, m_SeatedRotation);
            transform.SetParent(m_Point.transform, true);
            m_Point.SetAttached(m_Part);
            m_Part.SetAttached(m_Point);
        }

        void SetRocketCollisionsIgnored(bool ignore)
        {
            if (ignore)
            {
                if (m_Point == null || m_Point.tube == null) return;
                m_IgnoredColliders.Clear();
                var rocketColliders = m_Point.tube.GetComponentsInChildren<Collider>();
                foreach (var c in rocketColliders)
                {
                    if (c.isTrigger || System.Array.IndexOf(m_OwnColliders, c) >= 0) continue;
                    foreach (var own in m_OwnColliders) Physics.IgnoreCollision(own, c, true);
                    m_IgnoredColliders.Add(c);
                }
            }
            else
            {
                foreach (var c in m_IgnoredColliders)
                {
                    if (c == null) continue;
                    foreach (var own in m_OwnColliders) Physics.IgnoreCollision(own, c, false);
                }
                m_IgnoredColliders.Clear();
            }
        }
    }
}
