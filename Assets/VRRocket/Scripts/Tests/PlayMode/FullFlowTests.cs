using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VRRocket.Tests
{
    /// <summary>M7: stand release, whole-rocket carry, submission stub, report, inspect-only return, new build, abort return, dropped rocket (SPEC.md 5.6, 8.3, 8.4, 8.5).</summary>
    public class FullFlowTests : DevSceneTestBase
    {
        IEnumerator BuildPrototype()
        {
            yield return BuildAirframe();
            yield return SeatPart(Part(PartType.Motor), Point("MotorSeat"), Quaternion.identity);
            yield return SeatPart(Part(PartType.MotorCap), Point("CapSeat"), Quaternion.identity);
            Assert.AreEqual(AssemblyState.PrototypeComplete, assembly.State);
        }

        [UnityTest]
        public IEnumerator StandRelease_Carry_Submit_FailedLaunch_NewBuild()
        {
            var tube = Tube;
            var clamp = Point("StandClamp");
            bool can = false;
            yield return CanGrabFromNear(tube, r => can = r);
            Assert.IsFalse(can, "the tube cannot be grabbed during assembly");

            yield return BuildPrototype();
            Assert.AreEqual(1, Count("prototypeComplete"), "stand release fired once");
            Assert.IsTrue(assembly.inStand);
            yield return CanGrabFromNear(tube, r => can = r);
            Assert.IsTrue(can, "the stand releases the finished prototype");

            // lift it out: it re-enters the clamp's guide at depth 0 and comes free past guideLengthTube
            yield return Grab(tube);
            yield return PlaceHeld(clamp.transform.position + Vector3.up * (tuning.guideLengthTube * 1.5f), Quaternion.identity);
            Assert.AreEqual(PartState.Free, tube.state, "lifted out of the stand");
            Assert.IsFalse(assembly.inStand);
            Assert.AreEqual(1, CountPrefix("removed:BodyTube"));
            var fin = Part(PartType.TailFin, 1);
            Assert.AreSame(tube, fin.attachedTo.tube, "parts stay on the rocket");
            Assert.IsFalse(assembly.CanGrab(fin), "out of the stand nothing comes off");
            Assert.IsTrue(assembly.CanGrab(Part(PartType.MotorCap)), "except the cap, which can still be twisted");

            // carry it to the submission zone and let go there
            var zone = Object.FindFirstObjectByType<SubmissionZoneStub>();
            Assert.IsNotNull(zone, "submission zone stub in the dev scene");
            var zoneCollider = zone.GetComponent<BoxCollider>();
            yield return PlaceHeld(zoneCollider.bounds.center + Vector3.down * 0.1f, Quaternion.identity);
            Release(tube);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(zone.rocketInside, "a loose complete rocket is inside the zone");

            // submit
            BuildReport received = null;
            assembly.Submitted += r => received = r;
            zone.OnSubmitPressed();
            Assert.AreEqual(AssemblyState.Submitted, assembly.State);
            Assert.IsNotNull(received, "Submitted raised with a report");
            Assert.AreEqual(LaunchOutcome.MotorRetentionLoss, received.outcome, "cap was seated but never locked");
            Assert.IsFalse(received.FlapError);
            CollectionAssert.AreEqual(new[] { "MotorCap" }, received.failedComponents);
            Assert.IsTrue(assembly.isFrozen);
            Assert.IsFalse(assembly.CanGrab(tube), "frozen rocket");
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(tube.gameObject.activeInHierarchy, "the stub bin took it away");

            // failed launch: inspect-only return plus a new build
            var readout = Object.FindFirstObjectByType<OutcomeReadoutStub>();
            Assert.IsNotNull(readout, "outcome readout stub in the dev scene");
            readout.OnFailedLaunchPressed();
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(tube.gameObject.activeInHierarchy, "the prototype came back");
            Assert.AreSame(tube, assembly.inspectPrototype);
            Assert.IsTrue(assembly.CanGrab(tube), "it can be picked up and examined");
            Assert.IsFalse(assembly.CanGrab(fin), "but not altered");
            Assert.IsFalse(tube.body.isKinematic, "rests as a loose object");
            Assert.AreNotSame(tube, assembly.tube, "a fresh tube is in the stand");
            Assert.IsTrue(assembly.inStand);
            Assert.AreEqual(AssemblyState.BuildingAirframe, assembly.State);
            Assert.AreEqual(9, workstation.respawner.parts.Count, "nine fresh parts on the tray");
            foreach (var p in workstation.respawner.parts) Assert.AreEqual(PartState.Free, p.state, p.name + " is loose");
            Assert.IsTrue(Point("FinSlot_1").Accepts(Part(PartType.TailFin, 1)), "the new tube accepts parts");
            // only one inspection prototype at a time: the old inspect rocket's points are gated off
            foreach (var ap in tube.GetComponentsInChildren<AttachPoint>()) Assert.IsFalse(ap.gatingAllows, ap.pointName + " of the inspection prototype is dead");
        }

        [UnityTest]
        public IEnumerator Submit_Abort_ReturnsEditableToStand()
        {
            var tube = Tube;
            yield return BuildPrototype();
            yield return Grab(tube);
            yield return PlaceHeld(Point("StandClamp").transform.position + Vector3.up * (tuning.guideLengthTube * 1.5f), Quaternion.identity);
            var zone = Object.FindFirstObjectByType<SubmissionZoneStub>();
            yield return PlaceHeld(zone.GetComponent<BoxCollider>().bounds.center + Vector3.down * 0.1f, Quaternion.identity);
            Release(tube);
            yield return new WaitForSeconds(0.6f);
            zone.OnSubmitPressed();
            Assert.AreEqual(AssemblyState.Submitted, assembly.State);

            var readout = Object.FindFirstObjectByType<OutcomeReadoutStub>();
            readout.OnAbortPressed();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(AssemblyState.PrototypeComplete, assembly.State, "the same rocket is editable again");
            Assert.IsTrue(assembly.inStand, "back in the stand");
            Assert.IsTrue(tube.gameObject.activeInHierarchy);
            Assert.IsFalse(assembly.isFrozen);
            Assert.IsTrue(assembly.CanGrab(Part(PartType.MotorCap)), "cap editable");
            Assert.IsFalse(assembly.CanGrab(Part(PartType.TailFin, 1)), "fins still locked by the seated motor");
            // and it can be corrected: pull the cap, which is a backward move
            yield return RemovePart(Part(PartType.MotorCap));
            Assert.AreEqual(AssemblyState.MotorFitted, assembly.State);
        }

        [UnityTest]
        public IEnumerator DroppedRocket_ReturnsToStand()
        {
            var tube = Tube;
            yield return BuildPrototype();
            yield return Grab(tube);
            yield return PlaceHeld(Point("StandClamp").transform.position + Vector3.up * (tuning.guideLengthTube * 1.5f), Quaternion.identity);
            // drop it below the floor level
            yield return PlaceHeld(new Vector3(0f, 0.1f, 0.85f), Quaternion.identity);
            Release(tube);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(assembly.inStand, "a dropped rocket returns to the stand");
            Assert.AreEqual(AssemblyState.PrototypeComplete, assembly.State, "still complete");
            Assert.AreSame(tube, Part(PartType.NoseCone).attachedTo.tube, "parts came back with it");
        }
    }
}
