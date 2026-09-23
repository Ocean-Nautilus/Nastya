using System;
using System.IO;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DocumentManager : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField textInputField;
    public TMP_Text statusText;
    public TMP_Text titleText;

    [Header("Document Settings")]
    public string defaultDocumentsPath = "Documents";

    private TextDocument currentDocument;
    private string currentFilePath = "";
    private bool isModified;

    // Сообщение в строке статуса ("Сохранено", "Найдено 1 из 3" и т.п.)
    private string statusMessage = "";

    // Состояние поиска - для перехода к следующему совпадению при повторном нажатии
    private string lastSearchTerm = "";
    private int lastSearchIndex = -1;

    // Папка для документов пользователя.
    // В редакторе - Assets/Documents (там лежит пример документа).
    // В билде Application.dataPath на многих платформах недоступен для записи,
    // поэтому используется persistentDataPath - он предназначен как раз для этого.
#if UNITY_EDITOR
    public string DocumentsFolder => Path.Combine(Application.dataPath, defaultDocumentsPath);
#else
    public string DocumentsFolder => Path.Combine(Application.persistentDataPath, defaultDocumentsPath);
#endif

    void Start()
    {
        Directory.CreateDirectory(DocumentsFolder);

        if (textInputField != null)
        {
            // Без этого выделение сбрасывается, как только фокус уходит на кнопку
            // (Bold, Italic, Поиск...), и форматирование/подсветка не работают
            textInputField.resetOnDeActivation = false;
            textInputField.onFocusSelectAll = false;
        }

        // Создаем новый документ при старте (внутри уже обновляется интерфейс)
        CreateNewDocument();
    }

    // Создание нового документа
    public void CreateNewDocument()
    {
        currentDocument = new TextDocument();
        currentFilePath = "";
        isModified = false;
        ResetSearch();
        SetStatusMessage("Создан новый документ");
        UpdateUI();

        Debug.Log("Создан новый документ");
    }

    // Загрузка документа - настоящий диалог выбора файла (работает в редакторе Unity)
    public void LoadDocument()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Открыть документ", DocumentsFolder, "txt");
        if (string.IsNullOrEmpty(path))
            return;

        LoadFromPath(path);
#else
        SetStatusMessage("Открытие файлов доступно только в редакторе Unity");
        UpdateStatus();
        Debug.LogWarning("Системный диалог выбора файла доступен только при запуске в редакторе Unity. Для готового билда нужен отдельный файловый браузер внутри интерфейса.");
#endif
    }

    private void LoadFromPath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SetStatusMessage("Файл не найден");
            UpdateStatus();
            Debug.LogWarning("Файл не найден: " + filePath);
            return;
        }

        try
        {
            currentDocument = new TextDocument();
            currentDocument.content = File.ReadAllText(filePath);
            currentDocument.documentName = Path.GetFileName(filePath);
            currentDocument.filePath = filePath;
            currentFilePath = filePath;
            currentDocument.UpdateStatistics();
            isModified = false;
            ResetSearch();

            SetStatusMessage("Документ открыт");
            UpdateUI();
            Debug.Log("Документ загружен: " + filePath);
        }
        catch (Exception e)
        {
            SetStatusMessage("Ошибка загрузки: " + e.Message);
            UpdateStatus();
            Debug.LogError("Ошибка загрузки: " + e.Message);
        }
    }

    // Сохранение документа
    public void SaveDocument()
    {
        if (string.IsNullOrEmpty(currentFilePath))
        {
            SaveDocumentAs();
            return;
        }

        SaveToFile(currentFilePath);
    }

    // Сохранение документа как... - настоящий диалог сохранения (работает в редакторе Unity)
    public void SaveDocumentAs()
    {
#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanel("Сохранить документ как", DocumentsFolder, currentDocument.documentName, "txt");
        if (string.IsNullOrEmpty(path))
            return;

        SaveToFile(path);
#else
        // В билде диалога нет - сохраняем в папку документов под текущим именем
        string fileName = currentDocument.documentName.EndsWith(".txt") ? currentDocument.documentName : currentDocument.documentName + ".txt";
        SaveToFile(Path.Combine(DocumentsFolder, fileName));
#endif
    }

    private void SaveToFile(string filePath)
    {
        try
        {
            // Создаем директорию если не существует
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Берём текст прямо из поля, чтобы не потерять последние изменения
            if (textInputField != null)
            {
                currentDocument.content = textInputField.text;
            }

            // Сохраняем файл
            File.WriteAllText(filePath, currentDocument.content);
            currentFilePath = filePath;
            currentDocument.filePath = filePath;
            currentDocument.documentName = Path.GetFileName(filePath);
            isModified = false;

            SetStatusMessage("Сохранено: " + currentDocument.documentName);
            UpdateTitle();
            UpdateStatus();
            Debug.Log("Документ сохранен: " + filePath);
        }
        catch (Exception e)
        {
            SetStatusMessage("Ошибка сохранения: " + e.Message);
            UpdateStatus();
            Debug.LogError("Ошибка сохранения: " + e.Message);
        }
    }

    // Обновление содержимого документа из UI (вызывается из On Value Changed поля ввода)
    public void OnTextChanged()
    {
        if (currentDocument != null && textInputField != null)
        {
            currentDocument.content = textInputField.text;
            currentDocument.UpdateStatistics();
            isModified = true;
            statusMessage = "";
            ResetSearch();

            UpdateTitle();
            UpdateStatus();
        }
    }

    // Обновление интерфейса
    private void UpdateUI()
    {
        if (textInputField != null)
        {
            // Без уведомления: иначе сработает OnTextChanged и только что
            // открытый документ сразу будет помечен как изменённый
            textInputField.SetTextWithoutNotify(currentDocument.content);
        }

        UpdateTitle();
        UpdateStatus();
    }

    // Заголовок: имя документа и "*", если есть несохранённые изменения
    private void UpdateTitle()
    {
        if (titleText != null && currentDocument != null)
        {
            titleText.text = currentDocument.documentName + (isModified ? " *" : "");
        }
    }

    // Обновление статусной строки
    private void UpdateStatus()
    {
        if (statusText != null && currentDocument != null)
        {
            string stats = $"Символов: {currentDocument.characterCount} | Слов: {currentDocument.wordCount} | Строк: {currentDocument.lineCount}";
            statusText.text = string.IsNullOrEmpty(statusMessage) ? stats : stats + " | " + statusMessage;
        }
    }

    private void SetStatusMessage(string message)
    {
        statusMessage = message;
    }

    // Сброс поиска: следующий поиск начнётся с начала текста
    public void ResetSearch()
    {
        lastSearchTerm = "";
        lastSearchIndex = -1;
    }

    // Поиск текста с подсветкой найденного фрагмента.
    // Без учёта регистра; повторный поиск той же строки переходит
    // к следующему совпадению, после последнего - снова к первому.
    public void SearchText(string searchTerm)
    {
        if (textInputField == null)
            return;

        if (string.IsNullOrEmpty(searchTerm))
        {
            SetStatusMessage("Введите текст для поиска");
            UpdateStatus();
            return;
        }

        string content = textInputField.text;

        int startFrom = 0;
        if (string.Equals(searchTerm, lastSearchTerm, StringComparison.OrdinalIgnoreCase) && lastSearchIndex >= 0)
        {
            startFrom = Mathf.Min(lastSearchIndex + searchTerm.Length, content.Length);
        }

        int index = content.IndexOf(searchTerm, startFrom, StringComparison.OrdinalIgnoreCase);
        if (index < 0 && startFrom > 0)
        {
            // Дошли до конца - начинаем сначала
            index = content.IndexOf(searchTerm, 0, StringComparison.OrdinalIgnoreCase);
        }

        lastSearchTerm = searchTerm;
        lastSearchIndex = index;

        if (index < 0)
        {
            SetStatusMessage($"«{searchTerm}» не найдено");
            UpdateStatus();
            Debug.Log("Текст не найден: " + searchTerm);
            return;
        }

        int total = CountOccurrences(content, searchTerm, content.Length);
        int number = CountOccurrences(content, searchTerm, index) + 1;
        SetStatusMessage($"Найдено: {number} из {total}");
        UpdateStatus();

        // Поле ввода получает фокус только в следующем кадре,
        // поэтому выделение ставится через корутину
        StartCoroutine(InputFieldSelection.Select(textInputField, index, index + searchTerm.Length));
    }

    // Количество совпадений, начинающихся до позиции limit
    private static int CountOccurrences(string content, string searchTerm, int limit)
    {
        int count = 0;
        int index = content.IndexOf(searchTerm, 0, StringComparison.OrdinalIgnoreCase);
        while (index >= 0 && index < limit)
        {
            count++;
            index = content.IndexOf(searchTerm, index + searchTerm.Length, StringComparison.OrdinalIgnoreCase);
        }
        return count;
    }
}
