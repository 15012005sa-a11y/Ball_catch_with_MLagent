using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelBanner : MonoBehaviour
{
    public TMP_Text bannerText;        // UI Text ��� �������
    public float showTime = 3f;        // ������� ������ ����������

    private void Start()
    {
        int levelIndex = SceneManager.GetActiveScene().buildIndex;
        // ��������: "1 �������", "2 �������" � �.�.
        string text = levelIndex + " �������";

        StartCoroutine(ShowBanner(text));
    }

    private System.Collections.IEnumerator ShowBanner(string msg)
    {
        if (bannerText == null) yield break;

        bannerText.text = msg;
        bannerText.gameObject.SetActive(true);

        yield return new WaitForSeconds(showTime);

        bannerText.gameObject.SetActive(false);
    }
}
