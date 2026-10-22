using UnityEngine;

public abstract class WeatherEventBase : MonoBehaviour
{
    public abstract void BeginEvent(int WaveNumber);

    public abstract void EndEvent();
}
