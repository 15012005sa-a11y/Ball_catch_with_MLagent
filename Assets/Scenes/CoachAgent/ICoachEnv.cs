// Assets/Coach/ICoachEnv.cs
namespace Coach
{
    public struct WindowMetrics
    {
        public float hitRate;          // 0..1
        public float avgReactionMs;    // ~0..1000
        public float trunkCompFrac;    // 0..1
        public float throughputPerMin; // попыток/мин
    }

    public interface ICoachEnv
    {
        bool WindowReady { get; }                 // окно из попыток завершено?
        WindowMetrics GetLastWindowMetrics();     // метрики прошлого окна
        void GetCurrentParams(out float speed, out float interval, out float radius, out float sideBias);
        void ApplyParams(float speed, float interval, float radius, float sideBias);
        void ResetEpisode();                      // можно оставить пустым
    }
}
