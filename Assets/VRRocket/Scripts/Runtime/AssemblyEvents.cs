using System;

namespace VRRocket
{
    /// <summary>
    /// Static event hub for assembly moments (SPEC.md section 6). The mechanic raises them; <see cref="AssemblyFeedback"/> turns them
    /// into glow, audio and haptics, and <see cref="RocketAssembly"/> keeps the state machine. Static, so parts need no scene reference.
    /// </summary>
    public static class AssemblyEvents
    {
        public static event Action<RocketPart, AttachPoint> GuideEngaged;
        public static event Action<RocketPart, AttachPoint> GuideReleased;   // break away: deliberately no feedback
        public static event Action<RocketPart, AttachPoint> PartSeated;
        public static event Action<RocketPart, AttachPoint> PartRemoved;
        public static event Action<RocketPart> PartRespawned;
        public static event Action<RocketPart> CapDetent;      // one ratchet tick of new progress
        public static event Action<RocketPart> CapLocked;
        public static event Action<RocketPart> PrototypeComplete;   // the stand opens; the tube is the argument
        public static event Action<RocketPart, float, bool> PartImpact;  // part, relative speed, hit another part

        internal static void RaiseCapDetent(RocketPart part) => CapDetent?.Invoke(part);
        internal static void RaiseCapLocked(RocketPart part) => CapLocked?.Invoke(part);
        internal static void RaiseGuideEngaged(RocketPart part, AttachPoint point) => GuideEngaged?.Invoke(part, point);
        internal static void RaiseGuideReleased(RocketPart part, AttachPoint point) => GuideReleased?.Invoke(part, point);
        internal static void RaisePartSeated(RocketPart part, AttachPoint point) => PartSeated?.Invoke(part, point);
        internal static void RaisePartRemoved(RocketPart part, AttachPoint point) => PartRemoved?.Invoke(part, point);
        internal static void RaisePartRespawned(RocketPart part) => PartRespawned?.Invoke(part);
        internal static void RaisePrototypeComplete(RocketPart tube) => PrototypeComplete?.Invoke(tube);
        internal static void RaisePartImpact(RocketPart part, float speed, bool hitPart) => PartImpact?.Invoke(part, speed, hitPart);
    }
}
