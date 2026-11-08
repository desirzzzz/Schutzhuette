using UnityEngine;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Hilfen für die Client-Vorhersage: nicht vernetzte Darstellungs-Kopien von
    /// Gegenständen und lokales Aus- und Einblenden der echten Gegenstände.
    /// </summary>
    /// <remarks>
    /// Eine Kopie zeigt nur, was der Host sehr wahrscheinlich gleich tun wird. Sie ist kein
    /// NetworkObject, die anderen Spieler sehen sie nie. Reine Funktionen ohne Zustand.
    /// </remarks>
    public static class LocalGhost
    {
        /// <summary>
        /// Erzeugt eine Kopie mit gleicher Form, Größe, Farbe und Lage wie das Original,
        /// ohne Collider und ohne Rigidbody.
        /// </summary>
        public static GameObject CreateFrom(Transform source)
        {
            var ghost = new GameObject($"{source.name} (Vorhersage)");
            ghost.transform.SetPositionAndRotation(source.position, source.rotation);
            ghost.transform.localScale = source.lossyScale;

            if (source.TryGetComponent(out MeshFilter sourceFilter))
            {
                ghost.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            }

            if (source.TryGetComponent(out MeshRenderer sourceRenderer))
            {
                ghost.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
            }

            return ghost;
        }

        /// <summary>Schaltet die Darstellung eines Gegenstands nur auf diesem Rechner ein oder aus.</summary>
        public static void SetVisible(Renderer[] renderers, bool visible)
        {
            foreach (Renderer itemRenderer in renderers)
            {
                itemRenderer.enabled = visible;
            }
        }
    }
}
