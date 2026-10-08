using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRRocket.Tests
{
    /// <summary>
    /// Drives the guided attach mechanic of SPEC.md section 5.2 on the nose cone in the dev scene, by taking over the right
    /// controller's transform. Checks every stage: free, approach glow, capture, axis lock, break away, seat, remove.
    /// Feedback is checked as events and audio; haptics can only be felt in the headset (SPEC.md 12.2).
    /// </summary>
    public class NoseGuideTests
    {
        const string k_Scene = "Assets/VRRocket/Scenes/Dev_Interactions.unity";

        RocketWorkstation m_Workstation;
        XRInteractionManager m_Manager;
        NearFarInteractor m_Hand;
        Transform m_Controller;
        RocketPart m_Nose;
        GuideGrabTransformer m_Guide;
        AttachPoint m_Seat;
        AssemblyTuning m_Tuning;
        readonly List<string> m_Events = new List<string>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(k_Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            m_Workstation = Object.FindFirstObjectByType<RocketWorkstation>();
            m_Manager = Object.FindFirstObjectByType<XRInteractionManager>();
            // The rig's XRInputModalityManager keeps the controller objects inactive until a controller is tracked.
            // Give the simulator a moment, then take the right controller regardless and stop the manager toggling it.
            for (var frame = 0; frame < 120 && m_Hand == null; frame++)
            {
                foreach (var nf in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (nf.transform.parent != null && nf.transform.parent.name.StartsWith("Right") && nf.gameObject.activeInHierarchy) m_Hand = nf;
                if (m_Hand == null) yield return null;
            }
            if (m_Hand == null)
            {
                foreach (var nf in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (nf.transform.parent != null && nf.transform.parent.name.StartsWith("Right")) m_Hand = nf;
            }
            Assert.IsNotNull(m_Workstation);
            Assert.IsNotNull(m_Hand, "right NearFarInteractor");
            foreach (var modality in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputModalityManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                modality.enabled = false;
            m_Controller = m_Hand.transform.parent;
            m_Controller.gameObject.SetActive(true);
            yield return null;
            foreach (var tpd in m_Controller.GetComponents<TrackedPoseDriver>()) tpd.enabled = false;
            // park the hand away from the tray so its pusher body cannot sweep parts off the desk
            m_Controller.position = new Vector3(0.6f, 1.4f, 0.3f);
            m_Controller.rotation = Quaternion.identity;
            foreach (var p in m_Workstation.respawner.parts) if (p.partType == PartType.NoseCone) m_Nose = p;
            m_Guide = m_Nose.GetComponent<GuideGrabTransformer>();
            m_Seat = m_Workstation.stand.tube.transform.Find("AttachPoints/NoseSeat").GetComponent<AttachPoint>();
            m_Tuning = m_Workstation.tuning;
            m_Events.Clear();
            AssemblyEvents.GuideEngaged += OnEngaged;
            AssemblyEvents.GuideReleased += OnReleased;
            AssemblyEvents.PartSeated += OnSeated;
            AssemblyEvents.PartRemoved += OnRemoved;
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AssemblyEvents.GuideEngaged -= OnEngaged;
            AssemblyEvents.GuideReleased -= OnReleased;
            AssemblyEvents.PartSeated -= OnSeated;
            AssemblyEvents.PartRemoved -= OnRemoved;
            yield return null;
        }

        void OnEngaged(RocketPart p, AttachPoint a) => m_Events.Add("engaged:" + a.pointName);
        void OnReleased(RocketPart p, AttachPoint a) => m_Events.Add("released:" + a.pointName);
        void OnSeated(RocketPart p, AttachPoint a) => m_Events.Add("seated:" + a.pointName);
        void OnRemoved(RocketPart p, AttachPoint a) => m_Events.Add("removed:" + a.pointName);

        IEnumerator MoveHand(Vector3 worldPos)
        {
            m_Controller.position = worldPos;
            Physics.SyncTransforms();
            // let the interactor, grab interactable and attach controller settle
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator GrabNose()
        {
            yield return MoveHand(m_Nose.transform.position + Vector3.up * 0.03f);
            Assert.IsTrue(m_Manager.IsSelectPossible(m_Hand, m_Nose.grabInteractable), "near grab must be possible");
            m_Manager.SelectEnter((IXRSelectInteractor)m_Hand, m_Nose.grabInteractable);
            Assert.IsTrue(m_Nose.grabInteractable.isSelected);
            yield return new WaitForSeconds(0.25f);
        }

        void Release()
        {
            m_Manager.SelectExit((IXRSelectInteractor)m_Hand, m_Nose.grabInteractable);
        }

        Vector3 Seat => m_Seat.transform.position;
        Vector3 Rel => m_Nose.transform.position - Seat;

        [UnityTest]
        public IEnumerator NoseCone_ApproachCaptureSlideBreakSeatAndRemove()
        {
            var glowR = m_Tuning.glowRadius;
            var captureR = m_Tuning.captureRadius;
            var guideLen = m_Tuning.guideLengthNose;
            var breakR = m_Tuning.breakRadius;
            var magnet = m_Tuning.noseMagnetism;
            // the hand holds the nose 0.03 above its origin, so hand = target + 0.03 up
            var grabOffset = Vector3.up * 0.03f;

            yield return GrabNose();
            Assert.AreEqual(PartState.Free, m_Nose.state);
            Assert.IsFalse(m_Guide.isGuided);

            // Free, far: follows the hand, no glow
            yield return MoveHand(Seat + new Vector3(0.4f, 0.1f, 0f) + grabOffset);
            Assert.Less(Vector3.Distance(m_Nose.transform.position, m_Controller.position - grabOffset), 0.01f, "free part follows the hand");
            Assert.IsFalse(m_Seat.glowRenderer.enabled, "no glow outside glowRadius");

            // Approach: inside glowRadius, outside captureRadius
            yield return MoveHand(Seat + Vector3.up * (glowR * 0.5f) + grabOffset);
            Assert.IsTrue(m_Seat.glowRenderer.enabled, "glow inside glowRadius");
            Assert.IsFalse(m_Guide.isGuided, "not yet captured");

            // Capture: inside captureRadius, upright
            yield return MoveHand(Seat + new Vector3(0.01f, captureR * 0.6f, 0f) + grabOffset);
            Assert.IsTrue(m_Guide.isGuided, "guide engages inside captureRadius");
            Assert.AreEqual(PartState.Guided, m_Nose.state);
            CollectionAssert.Contains(m_Events, "engaged:NoseSeat");
            Assert.Less(Mathf.Abs(Rel.x) + Mathf.Abs(Rel.z), 1e-3f, "on the axis");
            Assert.AreEqual(captureR * 0.6f * magnet, Rel.y, 2e-3f, "displayed depth is hand depth times noseMagnetism");
            Assert.Less(Vector3.Angle(m_Nose.transform.up, m_Seat.transform.up), 0.5f, "rotation locked upright");

            // Slide with a sideways hand offset inside breakRadius: stays on the axis
            yield return MoveHand(Seat + new Vector3(breakR * 0.4f, 0.02f, 0f) + grabOffset);
            Assert.IsTrue(m_Guide.isGuided);
            Assert.Less(Mathf.Abs(Rel.x) + Mathf.Abs(Rel.z), 1e-3f, "sideways hand motion does not move the part off the axis");
            Assert.AreEqual(0.02f * magnet, Rel.y, 2e-3f);

            // Break away: sideways beyond breakRadius
            yield return MoveHand(Seat + new Vector3(breakR * 1.5f, 0.02f, 0f) + grabOffset);
            Assert.IsFalse(m_Guide.isGuided, "guide lets go beyond breakRadius");
            Assert.AreEqual(PartState.Free, m_Nose.state);
            CollectionAssert.Contains(m_Events, "released:NoseSeat");
            Assert.Less(Vector3.Distance(m_Nose.transform.position, m_Controller.position - grabOffset), 0.01f, "follows the hand again");

            // Re-capture and release: seats
            yield return MoveHand(Seat + Vector3.up * (captureR * 0.6f) + grabOffset);
            Assert.IsTrue(m_Guide.isGuided);
            Release();
            yield return new WaitForSeconds(m_Tuning.seatDuration + 0.3f);
            Assert.AreEqual(PartState.Attached, m_Nose.state);
            Assert.AreSame(m_Seat, m_Nose.attachedTo);
            Assert.IsTrue(m_Seat.isOccupied);
            Assert.AreSame(m_Seat.transform, m_Nose.transform.parent, "attached parts are parented under the tube");
            Assert.Less(m_Nose.transform.localPosition.magnitude, 1e-4f, "flush on the seat");
            Assert.IsTrue(m_Nose.body.isKinematic);
            Assert.IsFalse(m_Seat.glowRenderer.enabled, "glow off once seated");
            CollectionAssert.Contains(m_Events, "seated:NoseSeat");
            var tubeCollider = m_Workstation.stand.tube.GetComponent<Collider>();
            Assert.IsTrue(Physics.GetIgnoreCollision(m_Nose.GetComponent<Collider>(), tubeCollider), "attached part ignores the rocket");
            var feedback = m_Workstation.GetComponent<AssemblyFeedback>();
            var playedSeatClip = false;
            foreach (var src in feedback.GetComponentsInChildren<AudioSource>())
                if (src.clip == feedback.library.noseSeat) playedSeatClip = true;
            Assert.IsTrue(playedSeatClip, "nose seat clip was queued on a pooled 3D source");

            // Remove: grab the seated nose, it re-enters the guide at depth 0, pulling past guideLength detaches
            yield return GrabNose();
            Assert.IsTrue(m_Guide.isGuided, "grabbing an attached part re-enters the guide");
            Assert.AreEqual(PartState.Guided, m_Nose.state);
            yield return MoveHand(Seat + Vector3.up * (guideLen * 0.5f) + grabOffset);
            Assert.IsTrue(m_Guide.isGuided, "inside guideLength it slides");
            Assert.AreEqual(guideLen * 0.5f * magnet, Rel.y, 2e-3f);
            yield return MoveHand(Seat + Vector3.up * (guideLen * 1.6f) + grabOffset);
            Assert.IsFalse(m_Guide.isGuided, "pulled past guideLength detaches");
            Assert.AreEqual(PartState.Free, m_Nose.state);
            Assert.IsFalse(m_Seat.isOccupied);
            CollectionAssert.Contains(m_Events, "removed:NoseSeat");
            Assert.IsFalse(Physics.GetIgnoreCollision(m_Nose.GetComponent<Collider>(), tubeCollider), "loose part collides with the rocket again");

            // Drop it: dynamic again, back in its tray group, and it falls
            Release();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(m_Nose.body.isKinematic, "dropped part is dynamic");
            Assert.AreSame(m_Nose.homeParent, m_Nose.transform.parent, "dropped part returns to its tray group, not the attach point");
            Assert.Less(m_Nose.transform.position.y, Seat.y + guideLen * 1.6f - 0.03f, "it fell");
        }
    }
}
