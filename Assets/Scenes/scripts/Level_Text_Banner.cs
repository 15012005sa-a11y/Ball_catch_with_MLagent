using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelBanner : MonoBehaviour
{
    public TMP_Text bannerText;        // UI Text дл€ надписи
    public float showTime = 3f;        // сколько секунд показывать

    private void Start()
    {
        int levelIndex = SceneManager.GetActiveScene().buildIndex;
        // Ќапример: "1 уровень", "2 уровень" и т.д.
        string text = levelIndex + " уровень";

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
