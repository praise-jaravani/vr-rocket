using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRRocket
{
    /// <summary>
    /// Turns assembly events into audio and haptics using the <see cref="FeedbackLibrary"/> (SPEC.md section 6).
    /// Glow is driven by the attach points themselves. Sounds are 3D and play from the attach point or the part.
    /// Rejected placements and break-aways get nothing, on purpose.
    /// </summary>
    public sealed class AssemblyFeedback : MonoBehaviour
    {
        [SerializeField] FeedbackLibrary m_Library;
        [SerializeField, Range(1, 8)] int m_AudioSourceCount = 4;

        AudioSource[] m_Sources;
        int m_NextSource;

        public FeedbackLibrary library
        {
            get => m_Library;
            set => m_Library = value;
        }

        void Awake()
        {
            m_Sources = new AudioSource[m_AudioSourceCount];
            for (var i = 0; i < m_AudioSourceCount; i++)
            {
                var go = new GameObject("AudioSource_" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 0.2f;
                src.maxDistance = 6f;
                m_Sources[i] = src;
            }
        }

        void OnEnable()
        {
            AssemblyEvents.GuideEngaged += OnGuideEngaged;
            AssemblyEvents.PartSeated += OnPartSeated;
            AssemblyEvents.PartRemoved += OnPartRemoved;
            AssemblyEvents.PartRespawned += OnPartRespawned;
        }

        void OnDisable()
        {
            AssemblyEvents.GuideEngaged -= OnGuideEngaged;
            AssemblyEvents.PartSeated -= OnPartSeated;
            AssemblyEvents.PartRemoved -= OnPartRemoved;
            AssemblyEvents.PartRespawned -= OnPartRespawned;
        }

        void OnGuideEngaged(RocketPart part, AttachPoint point)
        {
            if (m_Library == null) return;
            PlayAt(m_Library.guideEngage, point.transform.position);
            SendHaptic(part, m_Library.hapticGuideEngage);
        }

        void OnPartSeated(RocketPart part, AttachPoint point)
        {
            if (m_Library == null) return;
            PlayAt(m_Library.SeatClipFor(part.partType), point.transform.position);
            SendHaptic(part, m_Library.SeatHapticFor(part.partType));
        }

        void OnPartRemoved(RocketPart part, AttachPoint point)
        {
            if (m_Library == null) return;
            PlayAt(m_Library.unseat, point.transform.position);
            SendHaptic(part, m_Library.hapticPartRemoved);
        }

        void OnPartRespawned(RocketPart part)
        {
            if (m_Library == null) return;
            PlayAt(m_Library.respawnPop, part.transform.position);
        }

        /// <summary>Plays a clip as a 3D sound at a world position using the pooled sources. No allocation.</summary>
        public void PlayAt(AudioClip clip, Vector3 position)
        {
            if (clip == null || m_Sources == null) return;
            var src = m_Sources[m_NextSource];
            m_NextSource = (m_NextSource + 1) % m_Sources.Length;
            src.transform.position = position;
            src.volume = m_Library != null ? m_Library.volume : 1f;
            src.clip = clip;
            src.Play();
        }

        /// <summary>Sends an impulse to the controller that is holding, or last held, the part.</summary>
        public static bool SendHaptic(RocketPart part, FeedbackLibrary.Haptic haptic)
        {
            if (part == null || haptic.amplitude <= 0f || haptic.duration <= 0f) return false;
            var interactor = part.lastHoldingInteractor as XRBaseInputInteractor;
            if (interactor == null) return false;
            return interactor.SendHapticImpulse(haptic.amplitude, haptic.duration);
        }
    }
}
