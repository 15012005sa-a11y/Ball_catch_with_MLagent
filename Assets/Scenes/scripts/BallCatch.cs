using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BallCatch : MonoBehaviour
{
    [Tooltip("������� ����� ����� �� ���� �����")]
    public int points = 1;

    private void Reset()
    {
        // ��� ���������� ���������� � ������������� �������� Collider ��� �������
        var col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        // ��� ��� ����������, ��� Collider ���� � ��� Trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ���������, ��� ���� ����� ����� ��� Hand
        if (!other.CompareTag("Hand"))
            return;

        // ��������� ����
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(points);

        // ����� ����� ��������� ���� �������, �������� � �.�.

        // ���������� ���� ����� ����� ��� �������
        Destroy(gameObject);
    }
}
