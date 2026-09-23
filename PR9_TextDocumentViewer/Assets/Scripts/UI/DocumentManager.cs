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

    // Папка для документов пользователя. Application.dataPath (старый вариант)
    // недоступен для записи в билде на многих платформах - persistentDataPath
    // предназначен для этого специально и работает везде.
    public string DocumentsFolder => Path.Combine(Application.persistentDataPath, defaultDocumentsPath);

    void Start()
    {
        Directory.CreateDirectory(DocumentsFolder);

        // Создаем новый документ при старте
        CreateNewDocument();

        // Обновляем интерфейс
        UpdateUI();
    }

    // Создание нового документа
    public void CreateNewDocument()
    {
        currentDocument = new TextDocument();
        currentFilePath = "";
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
        Debug.LogWarning("Системный диалог выбора файла доступен только при запуске в редакторе Unity. Для готового билда нужен отдельный файловый браузер внутри интерфейса.");
#endif
    }

    private void LoadFromPath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Файл не найден: " + filePath);
            return;
        }

        try
        {
            currentDocument = new TextDocument();
            currentDocument.content = File.ReadAllText(filePath);
            currentDocument.documentName = Path.GetFileName(filePath);
            currentFilePath = filePath;
            currentDocument.UpdateStatistics();

            UpdateUI();
            Debug.Log("Документ загружен: " + filePath);
        }
        catch (System.Exception e)
        {
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
        Debug.LogWarning("Системный диалог сохранения доступен только при запуске в редакторе Unity. Для готового билда нужен отдельный экран ввода имени файла.");
#endif
    }

    private void SaveToFile(string filePath)
    {
        try
        {
            // Создаем директорию если не существует
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Сохраняем файл
            File.WriteAllText(filePath, currentDocument.content);
            currentFilePath = filePath;
            currentDocument.documentName = Path.GetFileName(filePath);

            UpdateUI();
            Debug.Log("Документ сохранен: " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ошибка сохранения: " + e.Message);
        }
    }

    // Обновление содержимого документа из UI
    public void OnTextChanged()
    {
        if (currentDocument != null && textInputField != null)
        {
            currentDocument.content = textInputField.text;
            currentDocument.UpdateStatistics();
            UpdateStatus();
        }
    }

    // Обновление интерфейса
    private void UpdateUI()
    {
        if (textInputField != null)
        {
            textInputField.text = currentDocument.content;
        }

        if (titleText != null)
        {
            titleText.text = currentDocument.documentName + (string.IsNullOrEmpty(currentFilePath) ? " *" : "");
        }

        UpdateStatus();
    }

    // Обновление статусной строки
    private void UpdateStatus()
    {
        if (statusText != null && currentDocument != null)
        {
            statusText.text = $"Символов: {currentDocument.characterCount} | Слов: {currentDocument.wordCount} | Строк: {currentDocument.lineCount}";
        }
    }

    // Поиск текста с видимой подсветкой
    public void SearchText(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm) || textInputField == null)
            return;

        string content = textInputField.text;
        int index = content.IndexOf(searchTerm);
        if (index >= 0)
        {
            // Активируем поле перед выделением - иначе подсветка невидима,
            // если фокус остался на кнопке "Найти"
            textInputField.ActivateInputField();
            textInputField.Select();
            textInputField.selectionAnchorPosition = index;
            textInputField.selectionFocusPosition = index + searchTerm.Length;
        }
        else
        {
            Debug.Log("Текст не найден: " + searchTerm);
        }
    }
}
