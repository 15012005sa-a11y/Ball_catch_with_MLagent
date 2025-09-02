using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;   

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class AppShellBootstrapper : MonoBehaviour
{
    [Header("Gameplay scene name")]
    public string gameplaySceneName = "GameScene";

    // Patients tab (search/add/list)
    RectTransform patientsListContent;
    TMP_InputField searchField, addNameField;

#if UNITY_EDITOR
    [Tooltip("Показывать UI в редакторе без Play")]
    public bool previewInEditMode = true;
#endif

    private NavigationController nav;

    // New session inputs
    TMP_InputField fL1, fL2, fRest, fRedChance;
    Toggle tStopKinect, tStopBetween;

    // Header
    TextMeshProUGUI headerTitle;

    // Select patient (New session)
    TextMeshProUGUI selectedPatientText;
    int selectedPatientId = -1;
    List<Button> patientButtons = new List<Button>();

    void Start()
    {
        if (Application.isPlaying)
            BuildUI();
    }


    // ----- Жизненный цикл (рядом со Start/Update) -----
    void OnEnable()
    {
#if UNITY_EDITOR
    // Не строим превью, если запускается Play или идёт смена режима
    if (!Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && previewInEditMode)
        RebuildEditorPreview();
#endif
    }

#if UNITY_EDITOR
void OnDisable()
{
    // Чистим только в режиме редактора, чтобы не конфликтовать с PlayMode revert
    if (!Application.isPlaying)
        ClearBuiltUI();
}
#endif

    // ----- Хелперы (ниже, в конце файла тоже ок) -----
    void ClearBuiltUI()
    {
#if UNITY_EDITOR
    // Сброс выделения, чтобы инспектор не держал уничтожаемые объекты
    Selection.activeObject = null;
#endif

        // Удаляем только наш Canvas превью
        var ex = transform.Find("AppShell_Canvas");
        if (ex != null)
        {
#if UNITY_EDITOR
        DestroyImmediate(ex.gameObject);
#else
            Destroy(ex.gameObject);
#endif
        }

        // И только наш локальный EventSystem превью
        var es = transform.Find("AppShell_EventSystem");
        if (es != null)
        {
#if UNITY_EDITOR
        DestroyImmediate(es.gameObject);
#else
            Destroy(es.gameObject);
#endif
        }
    }

#if UNITY_EDITOR
[ContextMenu("Rebuild UI (Editor)")]
public void RebuildEditorPreview()
{
    ClearBuiltUI();
    BuildUI();
    EditorApplication.QueuePlayerLoopUpdate();
    SceneView.RepaintAll();
}
#endif

    TMP_Dropdown ddPatient;
    Button btnStart;

    void SetProtocolInteractable(bool on)
    {
        if (fL1) fL1.interactable = on;
        if (fL2) fL2.interactable = on;
        if (fRest) fRest.interactable = on;
        if (fRedChance) fRedChance.interactable = on;
        if (tStopKinect) tStopKinect.interactable = on;
        if (tStopBetween) tStopBetween.interactable = on;
        if (btnStart) btnStart.interactable = on;
    }

    void ApplySettingsToUI(PatientSettings s)
    {
        // Учитываем суффиксы " s" в NumericField
        fL1.text = s.Level1Duration.ToString("0.#", CultureInfo.InvariantCulture) + " s";
        fL2.text = s.Level2Duration.ToString("0.#", CultureInfo.InvariantCulture) + " s";
        fRest.text = s.RestSeconds.ToString("0.#", CultureInfo.InvariantCulture) + " s";
        fRedChance.text = s.RedChance.ToString("0.##", CultureInfo.InvariantCulture);

        if (tStopKinect) tStopKinect.isOn = s.StopKinectOnGameEnd;
        if (tStopBetween) tStopBetween.isOn = s.StopBetweenLevels;
    }

    PatientSettings ReadSettingsFromUI()
    {
        return new PatientSettings
        {
            Level1Duration = UIPrimitives.ParseOrDefault(fL1, 60f),
            Level2Duration = UIPrimitives.ParseOrDefault(fL2, 40f),
            RestSeconds = UIPrimitives.ParseOrDefault(fRest, 10f),
            RedChance = UIPrimitives.ParseOrDefault(fRedChance, 0.35f),
            StopKinectOnGameEnd = tStopKinect && tStopKinect.isOn,
            StopBetweenLevels = tStopBetween && tStopBetween.isOn
        };
    }

    void OnPatientSelected(int idx, System.Collections.Generic.List<(int id, string name)> map)
    {
        if (idx < 0 || idx >= map.Count)
        {
            selectedPatientId = -1;
            selectedPatientText.text = "Selected: none";
            SetProtocolInteractable(false);
            return;
        }

        selectedPatientId = map[idx].id;
        selectedPatientText.text = $"Selected: {map[idx].id}. {map[idx].name}";
        var s = PatientSettingsDB.Load(selectedPatientId);
        ApplySettingsToUI(s);
        SetProtocolInteractable(true);
    }


    void EnsureLocalEventSystem(Transform parent)
    {
        // если какой-то EventSystem уже есть в сцене — ничего не делаем
        if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        // создаём ровно один локально под нашим Canvas
        var go = new GameObject("AppShell_EventSystem");
        go.transform.SetParent(parent, false);
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }


    // ================== UI BUILD ==================
    void BuildUI()
    {
        var canvas = UIPrimitives.EnsureCanvas();
        canvas.gameObject.name = "AppShell_Canvas";
        canvas.transform.SetParent(this.transform, false);
        EnsureLocalEventSystem(canvas.transform);


        var root = UIPrimitives.FullscreenPanel(canvas.transform, "Root", UIPrimitives.BgMain);

        // Sidebar
        var sidebar = UIPrimitives.Panel(root, "Sidebar", UIPrimitives.BgPanel);
        var sRT = (RectTransform)sidebar;
        sRT.anchorMin = new Vector2(0, 0);
        sRT.anchorMax = new Vector2(0, 1);
        sRT.pivot = new Vector2(0f, 0.5f);
        sRT.sizeDelta = new Vector2(240, 0);
        sRT.anchoredPosition = Vector2.zero;

        var sVL = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
        sVL.childForceExpandHeight = false;
        sVL.padding = new RectOffset(20, 20, 24, 24);
        sVL.spacing = 10;
        sidebar.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        UIPrimitives.Label(sidebar, "Ball Catch", 24, true, UIPrimitives.TextMuted);
        var sHr = UIPrimitives.Panel(sidebar, "Sep", new Color32(255, 255, 255, 15));
        ((RectTransform)sHr).sizeDelta = new Vector2(0, 2);

        var btnPatients = UIPrimitives.Button(sidebar, "Patients", 18, () => nav.ShowPatients());
        var btnNew = UIPrimitives.Button(sidebar, "New session", 18, () => nav.ShowNewSession());
        var btnSettings = UIPrimitives.Button(sidebar, "Settings", 18, () => nav.ShowSettings());

        var kinectCard = UIPrimitives.Panel(sidebar, "KinectCard", UIPrimitives.BgMain);
        var leKC = kinectCard.gameObject.AddComponent<LayoutElement>();
        leKC.preferredHeight = 140;
        var kcLabel = UIPrimitives.Label(kinectCard, "Kinect", 18, true, UIPrimitives.TextMuted, TextAlignmentOptions.Center);
        UIPrimitives.Stretch((RectTransform)kcLabel.transform);

        // Main area
        var main = UIPrimitives.Panel(root, "MainArea", new Color32(0, 0, 0, 0));
        var mRT = (RectTransform)main;
        mRT.anchorMin = new Vector2(0, 0);
        mRT.anchorMax = new Vector2(1, 1);
        mRT.offsetMin = new Vector2(260, 24);
        mRT.offsetMax = new Vector2(-24, -24);

        // Header
        var header = UIPrimitives.Panel(main, "Header", new Color32(0, 0, 0, 0));
        var hRT = (RectTransform)header;
        hRT.anchorMin = new Vector2(0, 1);
        hRT.anchorMax = new Vector2(1, 1);
        hRT.pivot = new Vector2(0, 1);
        hRT.sizeDelta = new Vector2(0, 60);

        headerTitle = UIPrimitives.Label(header, "", 32, true, UIPrimitives.Text);
        var htRT = (RectTransform)headerTitle.transform;
        htRT.anchorMin = new Vector2(0, 0.5f);
        htRT.anchorMax = new Vector2(0, 0.5f);
        htRT.pivot = new Vector2(0, 0.5f);
        htRT.anchoredPosition = new Vector2(0, 0);

        // Pages
        var pages = UIPrimitives.Panel(main, "Pages", new Color32(0, 0, 0, 0));
        var pRT = (RectTransform)pages;
        pRT.anchorMin = new Vector2(0, 0);
        pRT.anchorMax = new Vector2(1, 1);
        pRT.offsetMin = new Vector2(0, 0);
        pRT.offsetMax = new Vector2(0, 0);

        // Nav controller
        nav = main.gameObject.AddComponent<NavigationController>();
        nav.HeaderTitle = headerTitle;
        nav.PatientsView = CreatePatientsView(pages);
        nav.NewSessionView = CreateNewSessionView(pages);
        nav.SettingsView = CreateSettingsView(pages);

        // Default tab
        nav.ShowNewSession();
    }

    // ================== VIEWS ==================
    RectTransform CreateNewSessionView(Transform parent)
    {
        var view = UIPrimitives.Panel(parent, "NewSessionView", new Color32(0, 0, 0, 0));
        UIPrimitives.Stretch(view);

        // Карточка протокола
        var card = UIPrimitives.Panel(view, "ProtocolCard", UIPrimitives.BgPanel);
        var cRT = (RectTransform)card;
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(0, 1);
        cRT.pivot = new Vector2(0, 1);
        cRT.sizeDelta = new Vector2(720, 560);

        var vl = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(24, 24, 24, 24);
        vl.spacing = 16;
        vl.childForceExpandHeight = false;

        UIPrimitives.Label(card, "Protocol", 24, true, UIPrimitives.Text);

        // --- Select patient (dropdown) ---
        UIPrimitives.Label(card, "Select patient", 18, true, UIPrimitives.Text);

        // контейнер строки
        var rowSel = UIPrimitives.Panel(card, "RowSelectPatient", new Color32(0, 0, 0, 0));
        var hs = rowSel.gameObject.AddComponent<HorizontalLayoutGroup>();
        hs.spacing = 8; hs.childAlignment = TextAnchor.MiddleLeft;
        hs.childForceExpandWidth = false; hs.childForceExpandHeight = false;
        rowSel.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

        // подпись слева
        UIPrimitives.Label(rowSel, "Patient", 18, false, UIPrimitives.Text);

        // данные для dropdown
        var patients = SimplePatientStore.GetAll();                    // твой источник
        var options = patients.Select(p => $"{p.Id}. {p.Name}").ToList();
        var map = patients.Select(p => (p.Id, p.Name)).ToList();  // для обратного маппинга

        // сам dropdown (сохраняем ссылку в поле класса)
        (ddPatient, _) = UIPrimitives.Dropdown(rowSel, options, options.Count > 0 ? 0 : -1, new Vector2(420, 44));

        // подпись выбранного
        selectedPatientText = UIPrimitives.Label(
            card,
            options.Count > 0 ? $"Selected: {options[0]}" : "Selected: none",
            16, false, UIPrimitives.TextMuted);

        // инициализация/доступность полей
        if (options.Count == 0)
        {
            selectedPatientId = -1;
            SetProtocolInteractable(false);
        }
        else
        {
            // подставим настройки первого пациента и включим поля
            OnPatientSelected(ddPatient.value, map);
            ddPatient.onValueChanged.AddListener(i => OnPatientSelected(i, map));
        }


        // --- Protocol fields ---
        MakeRow(card, "Level 1 Duration", out fL1, defaultVal: "60");
        MakeRow(card, "Level 2 Duration", out fL2, defaultVal: "40");
        MakeRow(card, "Rest", out fRest, defaultVal: "10");

        var row4 = Row(card);
        UIPrimitives.Label(row4, "Red chance", 20, false, UIPrimitives.Text);
        (fRedChance, _) = UIPrimitives.FloatField(row4, "0.35");

        tStopKinect = UIPrimitives.Toggle(card, "Stop Kinect at the end of the game", false);
        tStopBetween = UIPrimitives.Toggle(card, "Stop between levels", false);

        // Footer
        var footer = UIPrimitives.Panel(card, "Footer", new Color32(0, 0, 0, 0));
        var fRT = (RectTransform)footer;
        fRT.sizeDelta = new Vector2(0, 70);
        var h = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleRight;
        h.padding = new RectOffset(0, 0, 8, 8);
        btnStart = UIPrimitives.Button(footer, "Start", 22, StartGame);


        return view;
    }

    RectTransform CreatePatientsView(Transform parent)
    {
        var view = UIPrimitives.Panel(parent, "PatientsView", new Color32(0, 0, 0, 0));
        UIPrimitives.Stretch((RectTransform)view);

        var card = UIPrimitives.Panel(view, "PatientsCard", UIPrimitives.BgPanel);
        var cRT = (RectTransform)card;
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0, 1);
        cRT.sizeDelta = new Vector2(0, 560);

        var vl = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(24, 24, 24, 24);
        vl.spacing = 12; vl.childForceExpandHeight = false;

        UIPrimitives.Label(card, "Patients", 24, true, UIPrimitives.Text);

        // Search row
        var rowSearch = UIPrimitives.Panel(card, "RowSearch", new Color32(0, 0, 0, 0));
        var hs = rowSearch.gameObject.AddComponent<HorizontalLayoutGroup>();
        hs.spacing = 8; hs.childAlignment = TextAnchor.MiddleLeft;
        hs.childForceExpandWidth = false; hs.childForceExpandHeight = false;
        var leRS = rowSearch.gameObject.AddComponent<LayoutElement>(); leRS.preferredHeight = 44;
        var lblSearch = UIPrimitives.Label(rowSearch, "Search", 18, false, UIPrimitives.Text);
        var leLblS = lblSearch.gameObject.AddComponent<LayoutElement>(); leLblS.preferredWidth = 100; leLblS.minWidth = 100;
        (searchField, _) = UIPrimitives.TextField(rowSearch, "name or id...", "");
        var leTF = searchField.GetComponent<LayoutElement>(); if (leTF != null) leTF.preferredWidth = 420;
        UIPrimitives.Button(rowSearch, "Find", 16, () => RebuildPatientList(), 120f);

        // --- Add row --------------------------------------------------------------
        var rowAdd = UIPrimitives.Panel(card, "RowAdd", new Color32(0, 0, 0, 0));
        var ha = rowAdd.gameObject.AddComponent<HorizontalLayoutGroup>();
        ha.spacing = 8;
        ha.childAlignment = TextAnchor.MiddleLeft;
        ha.childForceExpandWidth = false;
        ha.childForceExpandHeight = false;

        rowAdd.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

        // слева надпись
        var lblAdd = UIPrimitives.Label(rowAdd, "Add", 18, false, UIPrimitives.Text);
        var leLblA = lblAdd.gameObject.AddComponent<LayoutElement>();
        leLblA.preferredWidth = 100; leLblA.minWidth = 100;

        // поле ввода имени
        RectTransform nameRoot;
        (addNameField, nameRoot) = UIPrimitives.TextField(rowAdd, "Patient name...", "");
        var leName = nameRoot.GetComponent<LayoutElement>() ?? nameRoot.gameObject.AddComponent<LayoutElement>();
        leName.preferredWidth = 420;

        // кнопка "Add" -> вызываем наш обработчик
        UIPrimitives.Button(rowAdd, "Add", 18, AddPatient, 120f);


        // List
        var listHost = UIPrimitives.Panel(card, "ListHost", new Color32(0, 0, 0, 0));
        var lhRT = (RectTransform)listHost; lhRT.sizeDelta = new Vector2(0, 360);
        var (scroll, content) = UIPrimitives.VerticalScroll(listHost, new Vector2(0, 360));
        patientsListContent = content;

        RebuildPatientList();
        return (RectTransform)view;
    }

    // Добавление пациента из поля addNameField + сохранение дефолтных настроек
    void AddPatient()
    {
        var name = addNameField != null ? addNameField.text?.Trim() : null;
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[Patients] Enter a name before adding.");
            return;
        }

        // 1) ваш стор: добавляем по имени
        SimplePatientStore.Add(name);

        // 2) получаем Id новосозданного пациента
        var all = SimplePatientStore.GetAll();
        int newId = -1;

        // Если имена могут повторяться — берём ПОСЛЕДНИЙ с этим именем
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (string.Equals(all[i].Name, name, System.StringComparison.Ordinal))
            {
                newId = all[i].Id;
                break;
            }
        }

        // 3) создаём дефолтные настройки для него (если Id нашли)
        if (newId > 0)
            PatientSettingsDB.Save(newId, new PatientSettings());

        // 4) обновляем UI
        RefreshPatientsListUI();
        RebuildNewSessionDropdownAndSelect(newId);

        // 5) очистим поле ввода
        if (addNameField) addNameField.text = string.Empty;

        Debug.Log($"[Patients] Added: {(newId > 0 ? newId.ToString() : "?")} {name}");
    }


    // Полная перерасчётка dropdown на вкладке "New session" + выбор пациента selectId
    void RebuildNewSessionDropdownAndSelect(int selectId = -1)
    {
        if (ddPatient == null) return; // dropdown ещё не создан (например, ты на вкладке Patients)

        var patients = SimplePatientStore.GetAll();
        var options = patients.Select(p => $"{p.Id}. {p.Name}").ToList();
        var map = patients.Select(p => (p.Id, p.Name)).ToList();

        ddPatient.onValueChanged.RemoveAllListeners();
        ddPatient.options = options.Select(s => new TMP_Dropdown.OptionData(s)).ToList();

        int newIndex = 0;
        if (selectId > 0)
        {
            newIndex = map.FindIndex(m => m.Id == selectId);
            if (newIndex < 0) newIndex = 0;
        }

        // если пациентов нет — выключаем поля протокола
        if (options.Count == 0)
        {
            ddPatient.value = 0;
            ddPatient.RefreshShownValue();
            selectedPatientId = -1;
            selectedPatientText.text = "Selected: none";
            SetProtocolInteractable(false);
            return;
        }

        ddPatient.value = newIndex;
        ddPatient.RefreshShownValue();

        // руками вызовем обновление UI для выбранного
        OnPatientSelected(ddPatient.value, map);

        // подписка на смену выбора (захватываем актуальную map)
        ddPatient.onValueChanged.AddListener(i => OnPatientSelected(i, map));
    }

    // Простейшее обновление списка пациентов в левой панели "Patients"
    // Если у тебя уже есть свой рендер — можешь вызывать его тут.
    void RefreshPatientsListUI()
    {
        if (patientsListContent == null) return;

        // Почистим текущий контент
        foreach (Transform child in patientsListContent)
            Destroy(child.gameObject);

        // Перестроим список
        var list = SimplePatientStore.GetAll();
        foreach (var p in list)
        {
            var row = UIPrimitives.Panel(patientsListContent, $"PatientRow_{p.Id}", new Color32(0, 0, 0, 0));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8; h.childForceExpandWidth = true;
            UIPrimitives.Label(row, $"{p.Id}. {p.Name}", 18, false, UIPrimitives.Text);
        }
    }


    RectTransform CreateSettingsView(Transform parent)
    {
        var view = UIPrimitives.Panel(parent, "SettingsView", new Color32(0, 0, 0, 0));
        UIPrimitives.Stretch((RectTransform)view);
        var col = UIPrimitives.Panel(view, "SettingsCard", UIPrimitives.BgPanel);
        var rt = (RectTransform)col; rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.sizeDelta = new Vector2(520, 220);
        var vl = col.gameObject.AddComponent<VerticalLayoutGroup>(); vl.padding = new RectOffset(20, 20, 20, 20); vl.spacing = 12; vl.childForceExpandHeight = false;
        UIPrimitives.Label(col, "Settings", 24, true, UIPrimitives.Text);
        UIPrimitives.Toggle(col, "Fullscreen", Screen.fullScreen);
        UIPrimitives.Toggle(col, "VSync", QualitySettings.vSyncCount > 0);
        return (RectTransform)view;
    }

    // ================ HELPERS ================
    RectTransform Row(Transform parent)
    {
        var row = UIPrimitives.Panel(parent, "Row", new Color32(0, 0, 0, 0));
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 16; h.childForceExpandWidth = false; h.childAlignment = TextAnchor.MiddleLeft;
        var le = row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 50; le.minHeight = 44;
        return (RectTransform)row;
    }

    void MakeRow(Transform parent, string label, out TMP_InputField field, string defaultVal)
    {
        var row = Row(parent);
        var l = UIPrimitives.Label(row, label, 20, false, UIPrimitives.Text);
        var lRT = (RectTransform)l.transform; lRT.sizeDelta = new Vector2(260, 0);
        (field, _) = UIPrimitives.NumericField(row, defaultVal, " s");
    }

    // ---- Patients tab list building ----
    void RebuildPatientList()
    {
        for (int i = patientsListContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(patientsListContent.GetChild(i).gameObject);

        string query = (searchField != null ? searchField.text : "").Trim().ToLowerInvariant();
        var data = SimplePatientStore.GetAll();

        if (!string.IsNullOrEmpty(query))
            data = data.Where(p => p.Name.ToLowerInvariant().Contains(query) || p.Id.ToString() == query).ToList();

        foreach (var p in data)
            CreatePatientRow(p);
    }

    void CreatePatientRow(SimplePatientStore.Patient p)
    {
        var row = UIPrimitives.Panel(patientsListContent, $"Patient_{p.Id}", UIPrimitives.FieldBg);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12; h.childAlignment = TextAnchor.MiddleLeft; h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        var le = row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 44; le.minHeight = 44;

        UIPrimitives.Label(row, $"{p.Id}. {p.Name}", 18, false, UIPrimitives.Text);

        var spacer = new GameObject("spacer", typeof(RectTransform)); spacer.transform.SetParent(row.transform, false);
        var spLE = spacer.AddComponent<LayoutElement>(); spLE.flexibleWidth = 1;

        UIPrimitives.Button(row, "Select", 16, () => OnSelectPatient(p), 120f);
        UIPrimitives.Button(row, "Delete", 16, () =>
        {
            SimplePatientStore.Remove(p.Id);
            RebuildPatientList();
        }, 120f);
    }

    // ---- New session patient pick ----
    void BuildPatientChoiceButtons(Transform parent)
    {
        patientButtons.Clear();

        var list = SimplePatientStore.GetAll().Take(5).ToList();
        if (list.Count == 0)
        {
            SimplePatientStore.Add("Patient A");
            SimplePatientStore.Add("Patient B");
            SimplePatientStore.Add("Patient C");
            list = SimplePatientStore.GetAll().Take(5).ToList();
        }

        foreach (var p in list)
        {
            var btn = UIPrimitives.Button(parent, $"{p.Id}. {p.Name}", 16, () => OnSelectPatient(p), 180f);
            patientButtons.Add(btn);
        }
    }

    void OnSelectPatient(SimplePatientStore.Patient p)
    {
        selectedPatientId = p.Id;
        if (selectedPatientText != null)
            selectedPatientText.text = $"Selected: {p.Id}. {p.Name}";

        foreach (var b in patientButtons)
        {
            var img = b.GetComponent<Image>();
            if (img == null) continue;
            img.color = (b.GetComponentInChildren<TextMeshProUGUI>().text.StartsWith($"{p.Id}."))
                ? UIPrimitives.ButtonBgH
                : UIPrimitives.ButtonBg;
        }
    }

    // ================== START GAME ==================
    void StartGame()
    {
        // Сначала читаем UI -> сохраняем для выбранного пациента
        if (selectedPatientId > 0)
        {
            var ps = ReadSettingsFromUI();
            PatientSettingsDB.Save(selectedPatientId, ps);
            Debug.Log($"[PatientSettings] Saved for patientId={selectedPatientId} to {PatientSettingsDB.FilePath}\n{ps}");
        }
        else
        {
            Debug.LogWarning("[PatientSettings] No patient selected. Start is disabled but guard stays.");
            return;
        }

        // Параллельно заполним AppState.Config (как у тебя уже было)
        AppState.Config.Level1Duration = UIPrimitives.ParseOrDefault(fL1, 60f);
        AppState.Config.Level2Duration = UIPrimitives.ParseOrDefault(fL2, 40f);
        AppState.Config.RestSeconds = UIPrimitives.ParseOrDefault(fRest, 10f);
        AppState.Config.RedChance = UIPrimitives.ParseOrDefault(fRedChance, 0.35f);
        AppState.Config.StopKinectOnGameEnd = tStopKinect && tStopKinect.isOn;
        AppState.Config.StopBetweenLevels = tStopBetween && tStopBetween.isOn;
        AppState.Config.SelectedPatientId = selectedPatientId;

        // Переход в геймплей
        if (!string.IsNullOrEmpty(gameplaySceneName))
            SceneManager.LoadScene(gameplaySceneName);
        else
            Debug.LogError("[AppShell] gameplaySceneName is empty. Set it in the inspector.");
    }

}
