using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Rechnet aus Gewicht und Aufprallgeschwindigkeit, wie ein Aufprall klingt, und spielt ihn.
    /// Je schwerer und je härter, desto lauter und tiefer. Schwere Dinge klingen zusätzlich dumpf.
    /// </summary>
    /// <remarks>Reine Funktionen ohne Zustand, rein lokal.</remarks>
    public static class ImpactAudio
    {
        /// <summary>Klang eines Aufpralls.</summary>
        public struct ImpactTone
        {
            /// <summary>Lautstärke des Holz-Aufpralls (0 bis 1).</summary>
            public float Volume;

            /// <summary>Lautstärke der dumpfen Schicht (0 bis 1).</summary>
            public float HeavyLayerVolume;

            /// <summary>Tonhöhe, 1 = Original.</summary>
            public float Pitch;

            /// <summary>Tiefpass in Hz.</summary>
            public float Cutoff;
        }

        /// <summary>
        /// Gewicht als Wert von 0 (leichtester Gegenstand) bis 1 (schwerster). Logarithmisch,
        /// weil das Ohr Verhältnisse hört: 3 → 10 kg klingt ähnlich viel schwerer wie 10 → 30 kg.
        /// </summary>
        public static float Weight(SoundSettings settings, float mass)
        {
            float light = Mathf.Log(Mathf.Max(0.01f, settings.LightMass));
            float heavy = Mathf.Log(Mathf.Max(settings.LightMass + 0.01f, settings.HeavyMass));
            return Mathf.Clamp01(Mathf.InverseLerp(light, heavy, Mathf.Log(Mathf.Max(0.01f, mass))));
        }

        /// <summary>Härte als Wert von 0 (gerade hörbar) bis 1 (voller Aufprall).</summary>
        public static float Hardness(SoundSettings settings, float impactSpeed)
        {
            return Mathf.Clamp01(Mathf.InverseLerp(settings.MinImpactSpeed, settings.FullImpactSpeed, impactSpeed));
        }

        /// <summary>Klang eines Aufpralls ohne Zufall (für Vorschau und Tests).</summary>
        /// <param name="settings">Geräusch-Einstellungen.</param>
        /// <param name="mass">Gewicht in kg.</param>
        /// <param name="impactSpeed">Aufprallgeschwindigkeit senkrecht zur Fläche, m/s.</param>
        public static ImpactTone Evaluate(SoundSettings settings, float mass, float impactSpeed)
        {
            float weight = Weight(settings, mass);
            float hardness = Hardness(settings, impactSpeed);

            // Leise Stupser bleiben hörbar, schwere Dinge sind auch bei gleichem Tempo lauter.
            float volume = Mathf.Lerp(0.15f, 1f, hardness) * Mathf.Lerp(0.55f, 1f, weight) * settings.ImpactVolume;

            return new ImpactTone
            {
                Volume = volume,
                HeavyLayerVolume = volume * weight,
                // Härter = noch ein wenig tiefer.
                Pitch = Mathf.Lerp(settings.LightPitch, settings.HeavyPitch, weight) * Mathf.Lerp(1.05f, 0.92f, hardness),
                Cutoff = Mathf.Lerp(settings.LightCutoff, settings.HeavyCutoff, weight),
            };
        }

        /// <summary>Spielt einen Aufprall an einer Weltposition, mit leichtem Zufall in der Tonhöhe.</summary>
        public static void Play(SoundSettings settings, float mass, Vector3 position, float impactSpeed)
        {
            if (settings == null || impactSpeed < settings.MinImpactSpeed)
            {
                return;
            }

            ImpactTone tone = Evaluate(settings, mass, impactSpeed);
            float pitch = tone.Pitch * Random.Range(0.95f, 1.05f);

            int unused = -1;
            OneShotAudio.PlayAt(OneShotAudio.PickClip(settings.ImpactClips, ref unused), position,
                tone.Volume, pitch, tone.Cutoff, settings.ImpactMaxDistance);

            if (tone.HeavyLayerVolume > 0.02f)
            {
                OneShotAudio.PlayAt(OneShotAudio.PickClip(settings.HeavyImpactClips, ref unused), position,
                    tone.HeavyLayerVolume, pitch * 0.9f, tone.Cutoff, settings.ImpactMaxDistance);
            }
        }

        /// <summary>
        /// Liest aus einer Kollision Ort und Aufprallgeschwindigkeit senkrecht zur Fläche.
        /// Schleifen und Rutschen zählen so kaum, nur echtes Aufschlagen.
        /// </summary>
        /// <returns>false, wenn die Kollision keinen Kontaktpunkt hat.</returns>
        public static bool TryGetImpact(Collision collision, out Vector3 point, out float speed)
        {
            point = Vector3.zero;
            speed = 0f;
            if (collision.contactCount == 0)
            {
                return false;
            }

            ContactPoint contact = collision.GetContact(0);
            point = contact.point;
            speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));
            return true;
        }
    }
}
