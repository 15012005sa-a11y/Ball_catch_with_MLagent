// Assets/Scenes/CoachAgent/CoachTypes.cs
namespace K2.Coach
{
    /// ћетрики 1 "окна" (например, из 10 м€чей)
    public struct WindowMetrics
    {
        public float hitRate;          // 0..1   Ч дол€ пойманных
        public float avgReactionMs;    // мс     Ч средн€€ реакци€
        public float trunkCompFrac;    // 0..1   Ч дол€ компенсаций корпусом
        public float throughputPerMin; // попыток/мин
    }

    ///  онтракт среды дл€ CoachAgent (если используете адаптер)
    public interface ICoachEnv
    {
        bool WindowReady { get; }
        WindowMetrics GetLastWindowMetrics();

        void GetCurrentParams(out float speed, out float interval, out float radius, out float sideBias);
        void ApplyParams(float speed, float interval, float radius, float sideBias);
        void ResetEpisode();
    }
}
