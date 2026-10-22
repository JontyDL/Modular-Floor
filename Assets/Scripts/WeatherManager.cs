using UnityEngine;
using System.Collections.Generic;

public class WeatherManager : MonoBehaviour
{
    private WaveSpawner waveSpawner;

    [Header("Weather events")]
    [SerializeField] private WeatherEventBase[] WeatherEventPrefabs;

    [Header("Event Selection")]
    [SerializeField, Min(1)] private int FirstWeatherWave = 4;              // waves before this number cannot recieve weather events
    [SerializeField, Range(0f, 1f)] private float EventChance = 0.4f;       // 40% chance of a weather event happening each wave

    private WeatherEventBase ActiveEvent;

    private void Awake()
    {
        waveSpawner = GetComponent<WaveSpawner>();
    }

    private void OnEnable()
    {
        if (waveSpawner == null)
        {
            Debug.LogWarning("Weather Manager: Wave Spawner isn't found via GetComponent()", this);
            return;
        }

        waveSpawner.OnWaveStarted += HandleWaveStarted;
        waveSpawner.OnWaveEnded += HandleWaveEnded;
    }

    private void OnDisable()
    {
        if (waveSpawner != null)
        {
            waveSpawner.OnWaveStarted -= HandleWaveStarted;
            waveSpawner.OnWaveEnded -= HandleWaveEnded;
        }

        StopActiveEvent();
    }

    private void HandleWaveStarted(int WaveNumber)
    {
        StopActiveEvent();

        if (WaveNumber < FirstWeatherWave) return;          // too soon for weather events

        if (Random.value >= EventChance) return;            // no weather this wave

        WeatherEventBase SelectedEvent = ChooseEvent();

        if (SelectedEvent == null)
        {
            Debug.LogWarning("WeatherDirector: No valid weather events assigned.", this);
            return;
        }

        ActiveEvent = Instantiate(SelectedEvent, transform);
        ActiveEvent.gameObject.SetActive(true);
        ActiveEvent.BeginEvent(WaveNumber);

        Debug.Log($"Wave {WaveNumber}: weather event is {ActiveEvent.name}.");
    }

    private void HandleWaveEnded(int WaveNumber)
    {
        StopActiveEvent();
    }

    private void StopActiveEvent()
    {
        if (ActiveEvent == null)
            return;

        ActiveEvent.EndEvent();
        Destroy(ActiveEvent.gameObject);
        ActiveEvent = null;
    }

    private WeatherEventBase ChooseEvent()
    {
        if (WeatherEventPrefabs == null || WeatherEventPrefabs.Length == 0) return null;

        int Choice = Random.Range(0, WeatherEventPrefabs.Length);

        return WeatherEventPrefabs[Choice];
    }
}
