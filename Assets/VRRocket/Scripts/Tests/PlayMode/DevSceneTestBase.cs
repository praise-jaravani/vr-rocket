using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRRocket.Tests
{
    /// <summary>
    /// Loads Dev_Interactions, takes over the right controller's transform, and offers grab/move helpers plus an event log.
    /// Feedback is checked as events and audio; haptics can only be felt in the headset (SPEC.md 12.2).
    /// </summary>
    public abstract class DevSceneTestBase
    {
        const string k_Scene = "Assets/VRRocket/Scenes/Dev_Interactions.unity";

        protected RocketWorkstation workstation;
        protected XRInteractionManager manager;
        protected NearFarInteractor hand;
        protected Transform controller;
        protected AssemblyTuning tuning;
        protected readonly List<string> events = new List<string>();

        /// <summary>The hand holds a part this far above its origin, so hand = target pose + grabOffset.</summary>
        protected static readonly Vector3 grabOffset = Vector3.up * 0.03f;

        [UnitySetUp]
        public IEnumerator SetUpScene()
        {
            // Scene start-up with the simulator can log an XRI error ("GameObject is already being activated or deactivated")
            // from the rig's modality manager toggling controller objects. That is not what these tests check.
            LogAssert.ignoreFailingMessages = true;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(k_Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            workstation = Object.FindFirstObjectByType<RocketWorkstation>();
            manager = Object.FindFirstObjectByType<XRInteractionManager>();
            // The rig's XRInputModalityManager keeps the controller objects inactive until a controller is tracked.
            // Give the simulator a moment, then take the right controller regardless and stop the manager toggling it.
            for (var frame = 0; frame < 120 && hand == null; frame++)
            {
                foreach (var nf in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (nf.transform.parent != null && nf.transform.parent.name.StartsWith("Right") && nf.gameObject.activeInHierarchy) hand = nf;
                if (hand == null) yield return null;
            }
            if (hand == null)
            {
                foreach (var nf in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (nf.transform.parent != null && nf.transform.parent.name.StartsWith("Right")) hand = nf;
            }
            Assert.IsNotNull(workstation, "RocketWorkstation in the dev scene");
            Assert.IsNotNull(hand, "right NearFarInteractor");
            yield return null;
            foreach (var modality in Object.FindObjectsByType<XRInputModalityManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                modality.enabled = false;
            yield return null;
            controller = hand.transform.parent;
            if (!controller.gameObject.activeSelf) controller.gameObject.SetActive(true);
            foreach (var tpd in controller.GetComponents<TrackedPoseDriver>()) tpd.enabled = false;
            yield return null;
            LogAssert.ignoreFailingMessages = false;
            // park the hand away from the tray so its pusher body cannot sweep parts off the desk
            controller.position = new Vector3(0.6f, 1.4f, 0.3f);
            controller.rotation = Quaternion.identity;
            tuning = workstation.tuning;
            events.Clear();
            AssemblyEvents.GuideEngaged += OnEngaged;
            AssemblyEvents.GuideReleased += OnReleased;
            AssemblyEvents.PartSeated += OnSeated;
            AssemblyEvents.PartRemoved += OnRemoved;
            AssemblyEvents.CapDetent += OnCapDetent;
            AssemblyEvents.CapLocked += OnCapLocked;
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDownScene()
        {
            AssemblyEvents.GuideEngaged -= OnEngaged;
            AssemblyEvents.GuideReleased -= OnReleased;
            AssemblyEvents.PartSeated -= OnSeated;
            AssemblyEvents.PartRemoved -= OnRemoved;
            AssemblyEvents.CapDetent -= OnCapDetent;
            AssemblyEvents.CapLocked -= OnCapLocked;
            yield return null;
        }

        void OnEngaged(RocketPart p, AttachPoint a) => events.Add("engaged:" + a.pointName);
        void OnReleased(RocketPart p, AttachPoint a) => events.Add("released:" + a.pointName);
        void OnSeated(RocketPart p, AttachPoint a) => events.Add("seated:" + a.pointName);
        void OnRemoved(RocketPart p, AttachPoint a) => events.Add("removed:" + a.pointName);
        void OnCapDetent(RocketPart p) => events.Add("detent");
        void OnCapLocked(RocketPart p) => events.Add("locked");

        protected int Count(string e)
        {
            var n = 0;
            foreach (var x in events) if (x == e) n++;
            return n;
        }

        protected RocketPart Part(PartType type, int id = 1)
        {
            foreach (var p in workstation.respawner.parts) if (p.partType == type && p.partId == id) return p;
            throw new AssertionException("no part " + type + "_" + id);
        }

        protected AttachPoint Point(string name)
        {
            return workstation.stand.tube.transform.Find("AttachPoints/" + name).GetComponent<AttachPoint>();
        }

        protected IEnumerator MoveHand(Vector3 worldPos)
        {
            controller.position = worldPos;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f);
        }

        protected IEnumerator MoveHand(Vector3 worldPos, Quaternion worldRot)
        {
            controller.rotation = worldRot;
            yield return MoveHand(worldPos);
        }

        /// <summary>Grabs the part from grabOffset above its origin.</summary>
        protected IEnumerator Grab(RocketPart part)
        {
            yield return MoveHand(part.transform.position + grabOffset);
            Assert.IsTrue(manager.IsSelectPossible(hand, part.grabInteractable), "near grab of " + part.name + " must be possible");
            manager.SelectEnter((IXRSelectInteractor)hand, part.grabInteractable);
            Assert.IsTrue(part.grabInteractable.isSelected, part.name + " selected");
            yield return new WaitForSeconds(0.25f);
        }

        protected void Release(RocketPart part)
        {
            manager.SelectExit((IXRSelectInteractor)hand, part.grabInteractable);
        }

        /// <summary>Guides a part onto a seat along the point's axis and releases it. Expects it to seat.</summary>
        protected IEnumerator SeatPart(RocketPart part, AttachPoint point)
        {
            yield return Grab(part);
            var away = point.GuideAxisWorld;
            yield return MoveHand(point.transform.position + away * (tuning.captureRadius * 0.6f) + grabOffset);
            var guide = part.GetComponent<GuideGrabTransformer>();
            Assert.IsTrue(guide.isGuided, part.name + " guided at " + point.pointName);
            Release(part);
            yield return new WaitForSeconds(tuning.SeatDurationFor(part.partType) + 0.3f);
            Assert.AreEqual(PartState.Attached, part.state, part.name + " attached");
            Assert.AreSame(point, part.attachedTo);
        }
    }
}
