using System.Collections.Generic;
using Schutzhuette.Items;
using Unity.Netcode;
using UnityEngine;

namespace Schutzhuette.Carrying
{
    /// <summary>
    /// Legt beim Start einer Sitzung auf dem Host ein Prefab an festen Punkten aus
    /// (Kisten, Truhe, Ofen, Holz) und räumt beim Beenden wieder auf. Auf Wunsch wächst
    /// an einem Punkt nach einer Wartezeit ein neues Exemplar nach, wenn das alte
    /// verschwunden ist (zum Beispiel ein verbranntes Holzscheit).
    /// </summary>
    /// <remarks>
    /// Warum zur Laufzeit statt fest in der Szene: Jede Sitzung beginnt frisch, und nur
    /// der Host erzeugt die Objekte (NetworkObject.Spawn). Die Clients bekommen sie
    /// automatisch mitgeteilt. Liegt auf demselben GameObject wie der NetworkManager,
    /// mehrere dieser Komponenten nebeneinander sind erlaubt.
    /// Gegenstände (Prefabs mit <see cref="Item"/>) erscheinen und verschwinden über
    /// <see cref="ItemSpawning"/>, das vorher die Art prüft. Der Ofen ist kein Gegenstand
    /// und erscheint ohne Katalog.
    /// </remarks>
    [RequireComponent(typeof(NetworkManager))]
    public class CrateSpawner : MonoBehaviour
    {
        [Tooltip("Prefab mit NetworkObject (muss in den Network Prefabs stehen).")]
        [SerializeField] private NetworkObject _cratePrefab;

        [Tooltip("Hier liegt zu Beginn je ein Exemplar.")]
        [SerializeField] private Transform[] _spawnPoints = new Transform[0];

        [Tooltip("Sekunden, bis an einem leeren Punkt ein neues Exemplar erscheint. 0 = nie.")]
        [SerializeField, Min(0f)] private float _respawnDelay;

        [Tooltip("Nur für Vorratspakete ohne feste Ware (Kiste, Truhe): Diese Ware ist drin.")]
        [SerializeField] private WareKind _ware;

        private NetworkManager _networkManager;

        // Bewusst kein static: Mit ausgeschaltetem Domain Reload blieben die Listen
        // sonst ins nächste Play erhalten.
        private readonly List<NetworkObject> _spawnedObjects = new List<NetworkObject>();
        private NetworkObject[] _spawnedPerPoint = new NetworkObject[0];
        private float[] _emptySince = new float[0];

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
        }

        private void OnEnable()
        {
            _networkManager.OnServerStarted += SpawnAll;
            _networkManager.OnServerStopped += HandleServerStopped;
        }

        private void OnDisable()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.OnServerStarted -= SpawnAll;
            _networkManager.OnServerStopped -= HandleServerStopped;
        }

        // Läuft nur auf dem Host, direkt nach dem Start der Sitzung.
        private void SpawnAll()
        {
            if (_cratePrefab == null)
            {
                Debug.LogWarning("[Crates] Kein Prefab eingetragen.", this);
                return;
            }

            _spawnedPerPoint = new NetworkObject[_spawnPoints.Length];
            _emptySince = new float[_spawnPoints.Length];
            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                _emptySince[i] = -1f;
                SpawnAt(i);
            }

            Debug.Log($"[Crates] {_spawnedObjects.Count} x {_cratePrefab.name} ausgelegt.");
        }

        private void SpawnAt(int index)
        {
            Transform point = _spawnPoints[index];
            if (point == null)
            {
                return;
            }

            NetworkObject spawned;
            if (_cratePrefab.TryGetComponent(out Item item) && item.Kind != null)
            {
                // Gegenstand: Die Ausgabestelle prüft die Art. Erscheint nichts, steht der Grund im Log.
                spawned = ItemSpawning.Spawn(item.Kind, point.position, point.rotation, _ware);
                if (spawned == null)
                {
                    return;
                }
            }
            else
            {
                // Kein Gegenstand (zum Beispiel der Ofen).
                spawned = Instantiate(_cratePrefab, point.position, point.rotation);
                spawned.Spawn(destroyWithScene: true);
            }

            _spawnedPerPoint[index] = spawned;
            _spawnedObjects.Add(spawned);
        }

        // Nachwachsen, nur auf dem Host und nur, wenn eine Wartezeit eingestellt ist.
        private void Update()
        {
            if (_respawnDelay <= 0f || !_networkManager.IsServer || !_networkManager.IsListening)
            {
                return;
            }

            for (int i = 0; i < _spawnedPerPoint.Length; i++)
            {
                // Noch da (auch wenn es woanders liegt): nichts tun.
                if (_spawnedPerPoint[i] != null)
                {
                    _emptySince[i] = -1f;
                    continue;
                }

                if (_emptySince[i] < 0f)
                {
                    _emptySince[i] = Time.time;
                }
                else if (Time.time - _emptySince[i] >= _respawnDelay)
                {
                    _emptySince[i] = -1f;
                    SpawnAt(i);
                }
            }
        }

        // Host beendet: übrig gebliebene Objekte wegräumen, damit der nächste Start sauber beginnt.
        private void HandleServerStopped(bool wasHost)
        {
            foreach (NetworkObject spawned in _spawnedObjects)
            {
                if (spawned == null)
                {
                    continue;
                }

                if (spawned.TryGetComponent(out Item _))
                {
                    // Gegenstände verschwinden immer über die Ausgabestelle.
                    ItemSpawning.Remove(spawned);
                }
                else
                {
                    Destroy(spawned.gameObject);
                }
            }

            _spawnedObjects.Clear();
            _spawnedPerPoint = new NetworkObject[0];
            _emptySince = new float[0];
        }
    }
}
