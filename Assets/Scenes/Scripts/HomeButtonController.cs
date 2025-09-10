using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HomeButtonController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button homeButton;                   // ��� Button
    [SerializeField] private string appShellSceneName = "AppShell";
    [SerializeField] private LevelDirector levelDirector;         // ����� �� ��������� � ����� �������������

    private CanvasGroup cg;

    private void Reset()
    {
        if (!homeButton) homeButton = GetComponent<Button>();
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!homeButton) homeButton = GetComponent<Button>();
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>();
        cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (homeButton) homeButton.onClick.AddListener(GoHome);
    }

    private void OnEnable()
    {
        // ������������� �� ������� (�����: ������ ������� �������� ������!)
        if (!levelDirector) levelDirector = FindObjectOfType<LevelDirector>();
        if (levelDirector != null)
        {
            levelDirector.OnGameStarted += Hide;
            levelDirector.OnGameFinished += Show;
        }

        // �� ������ � ����������
        Show();
    }

    private void OnDisable()
    {
        if (levelDirector != null)
        {
            levelDirector.OnGameStarted -= Hide;
            levelDirector.OnGameFinished -= Show;
        }
    }

    public void Show()
    {
        if (!cg) return;
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        if (homeButton) homeButton.interactable = true;
        // �����: �� ��������� gameObject!
        // gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        if (homeButton) homeButton.interactable = false;
        // �����: �� ������ SetActive(false) � ����� ������� �� ������� � ������ �� �������.
    }

    public void GoHome()
    {
        // ����� ����� ��������� �������/����� ��� ��������
        // FindObjectOfType<KinectManager>()?.StopKinect();

        SceneManager.LoadScene(appShellSceneName, LoadSceneMode.Single);
    }

    // �� ������, ���� �� ������ ������ �� LevelDirector � ����� �������� ��� ������ �� UnityEvent � UI
    public void OnGameStartedHandler() => Hide();
    public void OnGameFinishedHandler() => Show();
}
