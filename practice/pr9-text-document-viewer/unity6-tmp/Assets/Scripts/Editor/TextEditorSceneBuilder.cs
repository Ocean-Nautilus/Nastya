using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Собирает сцену текстового редактора: создаёт весь интерфейс теми же пунктами меню,
// которыми пользуются вручную (GameObject → UI → …), расставляет элементы,
// добавляет компоненты-контроллеры и связывает их между собой.
public static class TextEditorSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/TextEditorScene.unity";
    private const string PrefabPath = "Assets/Prefabs/FileButtonTemplate.prefab";
    private const string AutoBuildKey = "TextEditorSceneBuilder.Checked";

    private static readonly Color PanelColor = new Color(0.94f, 0.94f, 0.96f, 1f);
    private static readonly Color BarColor = new Color(0.85f, 0.87f, 0.92f, 1f);
    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.6f);

    [MenuItem("Практика 9/Собрать сцену", false, 1)]
    public static void BuildScene()
    {
        if (TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog(
                "Нужны ресурсы TextMeshPro",
                "Сначала импортируйте ресурсы TMP:\n\n" +
                "Window → TextMeshPro → Import TMP Essential Resources\n\n" +
                "После импорта снова выберите «Практика 9 → Собрать сцену».",
                "Понятно");
            return;
        }

        try
        {
            BuildSceneInternal();
        }
        catch (BuildException e)
        {
            EditorUtility.DisplayDialog(
                "Не удалось собрать сцену",
                e.Message + "\n\nСоберите интерфейс вручную по инструкции из методички " +
                "(части 2 и 5–7) — все скрипты в проекте уже готовы.",
                "Понятно");
            Debug.LogError(e.Message);
        }
    }

    private static void BuildSceneInternal()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Свет в сцене с интерфейсом не нужен
        GameObject light = GameObject.Find("Directional Light");
        if (light != null)
        {
            Object.DestroyImmediate(light);
        }

        GameObject canvas = CreateCanvas();
        GameObject editorPanel = CreatePanel(canvas.transform, "EditorPanel", PanelColor);
        Stretch(editorPanel, 10, 10, 10, 10);

        // --- панель инструментов ---
        GameObject toolbar = CreatePanel(editorPanel.transform, "ToolbarPanel", BarColor);
        TopStretch(toolbar, 60, 0);

        GameObject newButton = CreateButton(toolbar.transform, "NewButton", "Новый", 10, -10, 110, 40);
        GameObject openButton = CreateButton(toolbar.transform, "OpenButton", "Открыть", 128, -10, 110, 40);
        GameObject saveButton = CreateButton(toolbar.transform, "SaveButton", "Сохранить", 246, -10, 130, 40);
        GameObject saveAsButton = CreateButton(toolbar.transform, "SaveAsButton", "Сохранить как", 384, -10, 170, 40);

        TMP_InputField searchInput = CreateInputField(toolbar.transform, "SearchInput", "Что искать…", 566, -10, 220, 40);
        GameObject searchButton = CreateButton(toolbar.transform, "SearchButton", "Поиск", 794, -10, 100, 40);

        TMP_Text titleText = CreateText(toolbar.transform, "TitleText", "Новый документ", 906, -10, 330, 40, 18);
        titleText.alignment = TextAlignmentOptions.MidlineRight;

        // --- панель форматирования ---
        GameObject formatPanel = CreatePanel(editorPanel.transform, "FormatPanel", BarColor);
        TopStretch(formatPanel, 45, -62);

        TMP_Dropdown fontDropdown = CreateDropdown(formatPanel.transform, "FontDropdown", 10, -5, 230, 35);
        TMP_InputField fontSizeInput = CreateInputField(formatPanel.transform, "FontSizeInput", "36", 250, -5, 80, 35);
        GameObject boldButton = CreateButton(formatPanel.transform, "BoldButton", "Ж", 340, -5, 45, 35);
        GameObject italicButton = CreateButton(formatPanel.transform, "ItalicButton", "К", 392, -5, 45, 35);
        GameObject underlineButton = CreateButton(formatPanel.transform, "UnderlineButton", "Ч", 444, -5, 45, 35);

        // --- поле текста и его полоса прокрутки ---
        TMP_InputField textInputField = CreateInputField(editorPanel.transform, "TextInputField",
            "Введите текст документа…", 0, 0, 0, 0);
        Stretch(textInputField.gameObject, 10, 115, 36, 40);
        textInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        textInputField.characterLimit = 0;
        textInputField.richText = true;
        if (textInputField.textComponent != null)
        {
            textInputField.textComponent.alignment = TextAlignmentOptions.TopLeft;
            textInputField.textComponent.fontSize = 20;
        }

        Scrollbar scrollbar = CreateScrollbar(editorPanel.transform, "TextScrollbar");
        textInputField.verticalScrollbar = scrollbar;

        // --- строка состояния ---
        GameObject statusPanel = CreatePanel(editorPanel.transform, "StatusPanel", BarColor);
        BottomStretch(statusPanel, 30);
        TMP_Text statusText = CreateText(statusPanel.transform, "StatusText", "Символов: 0 | Слов: 0 | Строк: 0",
            10, -3, 900, 24, 15);

        // --- панель «Открыть файл» ---
        GameObject openPanel = CreatePanel(canvas.transform, "OpenFilePanel", OverlayColor);
        Stretch(openPanel, 0, 0, 0, 0);
        GameObject openWindow = CreatePanel(openPanel.transform, "BrowserWindow", Color.white);
        Center(openWindow, 520, 430);
        CreateText(openWindow.transform, "BrowserTitle", "Открыть документ", 15, -10, 400, 30, 18);

        GameObject scrollView = CreateScrollView(openWindow.transform, "FileListScrollView");
        Stretch(scrollView, 15, 50, 15, 60);
        Transform content = scrollView.transform.Find("Viewport/Content");
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.spacing = 4;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text emptyText = CreateText(openWindow.transform, "EmptyText", "Пока нет ни одного документа",
            15, -200, 490, 30, 16);
        emptyText.alignment = TextAlignmentOptions.Center;

        GameObject closeButton = CreateButton(openWindow.transform, "CloseBrowserButton", "Закрыть", 400, -390, 110, 35);

        Button filePrefab = CreateFileButtonPrefab();

        // --- панель «Сохранить как» ---
        GameObject savePanel = CreatePanel(canvas.transform, "SaveAsPanel", OverlayColor);
        Stretch(savePanel, 0, 0, 0, 0);
        GameObject saveWindow = CreatePanel(savePanel.transform, "SaveWindow", Color.white);
        Center(saveWindow, 470, 190);
        CreateText(saveWindow.transform, "SaveTitle", "Имя файла:", 20, -15, 300, 30, 18);
        TMP_InputField fileNameInput = CreateInputField(saveWindow.transform, "FileNameInput",
            "Новый документ", 20, -55, 430, 40);
        GameObject saveConfirmButton = CreateButton(saveWindow.transform, "SaveConfirmButton", "Сохранить", 20, -120, 200, 45);
        GameObject saveCancelButton = CreateButton(saveWindow.transform, "SaveCancelButton", "Отмена", 250, -120, 200, 45);

        // --- контроллеры ---
        GameObject managerObject = new GameObject("DocumentManager");
        DocumentManager manager = managerObject.AddComponent<DocumentManager>();
        FileBrowserController browser = managerObject.AddComponent<FileBrowserController>();
        SaveAsController saveAs = managerObject.AddComponent<SaveAsController>();
        SearchController search = managerObject.AddComponent<SearchController>();
        FormatController format = managerObject.AddComponent<FormatController>();

        manager.textInputField = textInputField;
        manager.statusText = statusText;
        manager.titleText = titleText;
        manager.fileBrowser = browser;
        manager.saveAsPanel = saveAs;

        browser.documentManager = manager;
        browser.panelRoot = openPanel;
        browser.contentParent = content;
        browser.fileButtonPrefab = filePrefab;
        browser.emptyText = emptyText;

        saveAs.documentManager = manager;
        saveAs.panelRoot = savePanel;
        saveAs.fileNameInput = fileNameInput;

        search.documentManager = manager;
        search.searchInputField = searchInput;

        format.textInputField = textInputField;
        format.fontDropdown = fontDropdown;
        format.fontSizeInput = fontSizeInput;

        // --- события кнопок ---
        Wire(newButton, manager.CreateNewDocument);
        Wire(openButton, manager.OpenDocumentRequested);
        Wire(saveButton, manager.SaveDocument);
        Wire(saveAsButton, manager.SaveDocumentAs);
        Wire(searchButton, search.OnSearchButtonClicked);
        Wire(closeButton, browser.ClosePanel);
        Wire(saveConfirmButton, saveAs.OnConfirmClicked);
        Wire(saveCancelButton, saveAs.OnCancelClicked);
        Wire(boldButton, format.OnBoldButtonClicked);
        Wire(italicButton, format.OnItalicButtonClicked);
        Wire(underlineButton, format.OnUnderlineButtonClicked);

        openPanel.SetActive(false);
        savePanel.SetActive(false);

        Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        Debug.Log("Сцена собрана: " + ScenePath + ". Нажмите Play.");
    }

    // При первом открытии проекта сцена собирается сама
    [InitializeOnLoadMethod]
    private static void AutoBuildOnce()
    {
        if (SessionState.GetBool(AutoBuildKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoBuildKey, true);

        EditorApplication.delayCall += () =>
        {
            if (File.Exists(ScenePath) || Application.isPlaying)
            {
                return;
            }

            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("Импортируйте ресурсы TMP: Window → TextMeshPro → Import TMP Essential Resources, " +
                                 "затем выберите меню «Практика 9 → Собрать сцену».");
                return;
            }

            BuildScene();
        };
    }

    // ---------- вспомогательные методы ----------

    private class BuildException : System.Exception
    {
        public BuildException(string message) : base(message) { }
    }

    private static GameObject Menu(string path, Transform parent, string name)
    {
        Selection.activeGameObject = parent != null ? parent.gameObject : null;

        if (!EditorApplication.ExecuteMenuItem(path))
        {
            throw new BuildException("Не найден пункт меню: " + path);
        }

        GameObject created = Selection.activeGameObject;

        if (created == null)
        {
            throw new BuildException("Пункт меню ничего не создал: " + path);
        }
        created.name = name;

        if (parent != null && created.transform.parent != parent)
        {
            created.transform.SetParent(parent, false);
        }

        return created;
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvas = Menu("GameObject/UI/Canvas", null, "MainCanvas");
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = Menu("GameObject/UI/Panel", parent, name);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static GameObject CreateButton(Transform parent, string name, string caption,
        float x, float y, float width, float height)
    {
        GameObject button = Menu("GameObject/UI/Button - TextMeshPro", parent, name);
        Place(button, x, y, width, height);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = caption;
            label.fontSize = 16;
        }

        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content,
        float x, float y, float width, float height, float fontSize)
    {
        GameObject textObject = Menu("GameObject/UI/Text - TextMeshPro", parent, name);
        Place(textObject, x, y, width, height);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.black;
        return text;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder,
        float x, float y, float width, float height)
    {
        GameObject field = Menu("GameObject/UI/Input Field - TextMeshPro", parent, name);

        if (width > 0 && height > 0)
        {
            Place(field, x, y, width, height);
        }

        TMP_InputField input = field.GetComponent<TMP_InputField>();

        if (input.placeholder != null)
        {
            TMP_Text hint = input.placeholder.GetComponent<TMP_Text>();
            if (hint != null)
            {
                hint.text = placeholder;
                hint.fontSize = 16;
            }
        }

        if (input.textComponent != null)
        {
            input.textComponent.fontSize = 16;
        }

        return input;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name,
        float x, float y, float width, float height)
    {
        GameObject dropdown = Menu("GameObject/UI/Dropdown - TextMeshPro", parent, name);
        Place(dropdown, x, y, width, height);
        return dropdown.GetComponent<TMP_Dropdown>();
    }

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        return Menu("GameObject/UI/Scroll View", parent, name);
    }

    private static Scrollbar CreateScrollbar(Transform parent, string name)
    {
        GameObject scrollbar = Menu("GameObject/UI/Scrollbar", parent, name);
        RectTransform rect = scrollbar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(20, 0);
        rect.offsetMin = new Vector2(-20, 40);
        rect.offsetMax = new Vector2(-10, -115);

        Scrollbar component = scrollbar.GetComponent<Scrollbar>();
        component.direction = Scrollbar.Direction.BottomToTop;
        return component;
    }

    // Шаблон кнопки файла сохраняем как префаб и убираем со сцены
    private static Button CreateFileButtonPrefab()
    {
        GameObject temporary = Menu("GameObject/UI/Button - TextMeshPro", null, "FileButtonTemplate");
        RectTransform rect = temporary.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 36);

        LayoutElement layoutElement = temporary.AddComponent<LayoutElement>();
        layoutElement.minHeight = 36;
        layoutElement.preferredHeight = 36;

        TMP_Text label = temporary.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = "Документ";
            label.fontSize = 16;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.margin = new Vector4(10, 0, 0, 0);
        }

        Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, PrefabPath);
        Object.DestroyImmediate(temporary);

        return prefab.GetComponent<Button>();
    }

    private static void Wire(GameObject buttonObject, UnityAction call)
    {
        Button button = buttonObject.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, call);
    }

    // ---------- расположение ----------

    private static void Place(GameObject target, float x, float y, float width, float height)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
    }

    private static void Stretch(GameObject target, float left, float top, float right, float bottom)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void TopStretch(GameObject target, float height, float y)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(0, height);
        rect.anchoredPosition = new Vector2(0, y);
    }

    private static void BottomStretch(GameObject target, float height)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(0, height);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void Center(GameObject target, float width, float height)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
    }
}
