// Assets/Scenes/CoachAgent/CoachTypes.cs
namespace K2.Coach
{
    /// ������� 1 "����" (��������, �� 10 �����)
    public struct WindowMetrics
    {
        public float hitRate;          // 0..1   � ���� ���������
        public float avgReactionMs;    // ��     � ������� �������
        public float trunkCompFrac;    // 0..1   � ���� ����������� ��������
        public float throughputPerMin; // �������/���
    }

    /// �������� ����� ��� CoachAgent (���� ����������� �������)
    public interface ICoachEnv
    {
        bool WindowReady { get; }
        WindowMetrics GetLastWindowMetrics();

        void GetCurrentParams(out float speed, out float interval, out float radius, out float sideBias);
        void ApplyParams(float speed, float interval, float radius, float sideBias);
        void ResetEpisode();
    }
}
