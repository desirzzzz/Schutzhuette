using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Legt beim Start einer Sitzung auf dem Host Kisten an festen Punkten aus
    /// und räumt sie beim Beenden wieder weg.
    /// </summary>
    /// <remarks>
    /// Warum zur Laufzeit statt fest in der Szene: Jede Sitzung beginnt mit
    /// frischen Kisten an ihren Startplätzen, und nur der Host erzeugt sie
    /// (NetworkObject.Spawn), die Clients bekommen sie automatisch mitgeteilt.
    /// Liegt auf demselben GameObject wie der NetworkManager.
    /// </remarks>
    [RequireComponent(typeof(NetworkManager))]
    public class CrateSpawner : MonoBehaviour
    {
        [Tooltip("Kisten-Prefab mit NetworkObject (muss in den Network Prefabs stehen).")]
        [SerializeField] private NetworkObject _cratePrefab;

        [Tooltip("Hier liegt zu Beginn je eine Kiste.")]
        [SerializeField] private Transform[] _spawnPoints = new Transform[0];

        private NetworkManager _networkManager;

        // Bewusst kein static: Mit ausgeschaltetem Domain Reload bliebe die Liste sonst
        // ins nächste Play erhalten.
        private readonly List<NetworkObject> _spawnedCrates = new List<NetworkObject>();

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
        }

        private void OnEnable()
        {
            _networkManager.OnServerStarted += SpawnCrates;
            _networkManager.OnServerStopped += HandleServerStopped;
        }

        private void OnDisable()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.OnServerStarted -= SpawnCrates;
            _networkManager.OnServerStopped -= HandleServerStopped;
        }

        // Läuft nur auf dem Host, direkt nach dem Start der Sitzung.
        private void SpawnCrates()
        {
            if (_cratePrefab == null)
            {
                Debug.LogWarning("[Crates] Kein Kisten-Prefab eingetragen.", this);
                return;
            }

            foreach (Transform point in _spawnPoints)
            {
                if (point == null)
                {
                    continue;
                }

                NetworkObject crate = Instantiate(_cratePrefab, point.position, point.rotation);
                crate.Spawn(destroyWithScene: true);
                _spawnedCrates.Add(crate);
            }

            Debug.Log($"[Crates] {_spawnedCrates.Count} Kisten ausgelegt.");
        }

        // Host beendet: übrig gebliebene Kisten wegräumen, damit der nächste Start sauber beginnt.
        private void HandleServerStopped(bool wasHost)
        {
            foreach (NetworkObject crate in _spawnedCrates)
            {
                if (crate != null)
                {
                    Destroy(crate.gameObject);
                }
            }

            _spawnedCrates.Clear();
        }
    }
}
