using System;

namespace VRRocket
{
    /// <summary>
    /// Static event hub for assembly moments (SPEC.md section 6). The mechanic raises them; <see cref="AssemblyFeedback"/> turns them
    /// into glow, audio and haptics. Keeping them static means the parts never need a scene reference.
    /// </summary>
    public static class AssemblyEvents
    {
        public static event Action<RocketPart, AttachPoint> GuideEngaged;
        public static event Action<RocketPart, AttachPoint> GuideReleased;   // break away: deliberately no feedback
        public static event Action<RocketPart, AttachPoint> PartSeated;
        public static event Action<RocketPart, AttachPoint> PartRemoved;
        public static event Action<RocketPart> PartRespawned;

        internal static void RaiseGuideEngaged(RocketPart part, AttachPoint point) => GuideEngaged?.Invoke(part, point);
        internal static void RaiseGuideReleased(RocketPart part, AttachPoint point) => GuideReleased?.Invoke(part, point);
        internal static void RaisePartSeated(RocketPart part, AttachPoint point) => PartSeated?.Invoke(part, point);
        internal static void RaisePartRemoved(RocketPart part, AttachPoint point) => PartRemoved?.Invoke(part, point);
        internal static void RaisePartRespawned(RocketPart part) => PartRespawned?.Invoke(part);
    }
}
