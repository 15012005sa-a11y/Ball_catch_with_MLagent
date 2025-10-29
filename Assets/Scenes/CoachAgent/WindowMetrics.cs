// Assets/Scenes/CoachAgent/WindowMetrics.cs
namespace CoachEnv
{
    /// ������� ���� (��������, �� 10 �����)
    public struct WindowMetrics
    {
        public float hitRate;          // �������� 0..1
        public float avgReactionMs;    // ������� �������, ��
        public float trunkCompFrac;    // ���� ����������� �������� 0..1
        public float throughputPerMin; // ����: �������/���
    }
}
