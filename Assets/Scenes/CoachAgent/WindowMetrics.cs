// Assets/Scenes/CoachAgent/WindowMetrics.cs
namespace CoachEnv
{
    /// ћетрики окна (например, за 10 м€чей)
    public struct WindowMetrics
    {
        public float hitRate;          // точность 0..1
        public float avgReactionMs;    // средн€€ реакци€, мс
        public float trunkCompFrac;    // дол€ компенсаций корпусом 0..1
        public float throughputPerMin; // темп: попыток/мин
    }
}
