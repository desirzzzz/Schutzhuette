using UnityEngine;

namespace Schutzhuette.Audio
{
    /// <summary>
    /// Spielt einen Clip einmal an einer Stelle in der Welt ab, mit eigener Tonhöhe und
    /// optionalem Tiefpass. Das Geräuschobjekt räumt sich danach selbst weg.
    /// </summary>
    /// <remarks>
    /// Warum nicht <c>AudioSource.PlayClipAtPoint</c>: Dort kann man weder Tonhöhe noch
    /// Hörweite noch Filter einstellen. Und warum nicht die AudioSource am Gegenstand selbst:
    /// Ein Holzscheit, das im Ofen landet, verschwindet sofort (Despawn) und würde sein
    /// eigenes Geräusch abschneiden. Reine Funktion ohne Zustand, rein lokal.
    /// </remarks>
    public static class OneShotAudio
    {
        /// <summary>Spielt einen Clip als 3D-Geräusch an einer Weltposition.</summary>
        /// <param name="clip">Der Clip. Bei null passiert nichts.</param>
        /// <param name="position">Weltposition.</param>
        /// <param name="volume">Lautstärke 0 bis 1.</param>
        /// <param name="pitch">Tonhöhe, 1 = Original.</param>
        /// <param name="cutoff">Tiefpass in Hz, ab 20000 kein Filter.</param>
        /// <param name="maxDistance">Hörweite in Metern.</param>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume, float pitch, float cutoff, float maxDistance)
        {
            if (clip == null || volume <= 0.001f)
            {
                return;
            }

            var soundObject = new GameObject($"Geräusch {clip.name}");
            soundObject.transform.position = position;

            var source = soundObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1.5f;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;

            if (cutoff < 20000f)
            {
                soundObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = cutoff;
            }

            source.Play();

            // Tiefere Tonhöhe = längeres Abspielen.
            Object.Destroy(soundObject, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        /// <summary>
        /// Wählt einen zufälligen Clip, möglichst nicht denselben wie beim letzten Mal.
        /// </summary>
        /// <param name="clips">Auswahl. Leer oder null ergibt null.</param>
        /// <param name="lastIndex">Index des letzten Clips, wird aktualisiert.</param>
        public static AudioClip PickClip(AudioClip[] clips, ref int lastIndex)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == lastIndex)
            {
                index = (index + 1) % clips.Length;
            }

            lastIndex = index;
            return clips[index];
        }
    }
}
