using System;
using UnityEngine;

namespace VRRocket
{
    /// <summary>
    /// Every clip and haptic value of SPEC.md section 6 in one asset, so the teammates' final sounds can be swapped in without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "VR Rocket/Feedback Library", fileName = "FeedbackLibrary")]
    public sealed class FeedbackLibrary : ScriptableObject
    {
        [Serializable]
        public struct Haptic
        {
            [Range(0f, 1f)] public float amplitude;
            public float duration;

            public Haptic(float amplitude, float duration)
            {
                this.amplitude = amplitude;
                this.duration = duration;
            }
        }

        [Header("Audio clips (placeholders until the audio owner delivers)")]
        public AudioClip guideEngage;        // soft tick
        public AudioClip seat;               // click: fin, flap, motor
        public AudioClip noseSeat;           // soft click
        public AudioClip capSeatUnlocked;    // soft seat sound, not the click
        public AudioClip capDetent;          // ratchet tick
        public AudioClip capLock;            // firm click
        public AudioClip unseat;             // soft unseat
        public AudioClip standRelease;
        public AudioClip impactDesk;         // dull
        public AudioClip impactPart;         // sharp
        public AudioClip respawnPop;         // soft pop
        [Range(0f, 1f)] public float volume = 1f;

        [Header("Haptics on the holding hand (amplitude 0-1, seconds), tunable")]
        public Haptic hapticGuideEngage = new Haptic(0.15f, 0.02f);
        public Haptic hapticSeat = new Haptic(0.5f, 0.08f);
        public Haptic hapticNoseSeat = new Haptic(0.4f, 0.08f);
        public Haptic hapticCapSeatUnlocked = new Haptic(0.2f, 0.04f);
        public Haptic hapticCapDetent = new Haptic(0.2f, 0.02f);
        public Haptic hapticCapLock = new Haptic(1.0f, 0.20f);
        public Haptic hapticPartRemoved = new Haptic(0.2f, 0.04f);
        public Haptic hapticPrototypeComplete = new Haptic(0.3f, 0.10f);   // both hands

        public AudioClip SeatClipFor(PartType type)
        {
            switch (type)
            {
                case PartType.NoseCone: return noseSeat;
                case PartType.MotorCap: return capSeatUnlocked;
                default: return seat;
            }
        }

        public Haptic SeatHapticFor(PartType type)
        {
            switch (type)
            {
                case PartType.NoseCone: return hapticNoseSeat;
                case PartType.MotorCap: return hapticCapSeatUnlocked;
                default: return hapticSeat;
            }
        }
    }
}
