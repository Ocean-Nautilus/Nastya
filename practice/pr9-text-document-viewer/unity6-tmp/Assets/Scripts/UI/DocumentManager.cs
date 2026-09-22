using System;
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

public class DocumentManager : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField textInputField;
    public TMP_Text statusText;
    public TMP_Text titleText;

    [Header("Document Settings")]
    public string defaultDocumentsPath = "Documents";

    [Header("Панели")]
    public FileBrowserController fileBrowser;
    public SaveAsController saveAsPanel;

    private TextDocument currentDocument;
    private string currentFilePath = "";

    // Папка, доступная для записи на всех платформах
    public string DocumentsFolder
    {
        get { return Path.Combine(Application.persistentDataPath, defaultDocumentsPath); }
    }

    void Start()
    {
        Directory.CreateDirectory(DocumentsFolder);
        Debug.Log("Папка документов: " + DocumentsFolder);

        // Без этой подписки OnTextChanged не вызывается и статистика не обновляется
        if (textInputField != null)
        {
            textInputField.onValueChanged.AddListener(OnInputFieldChanged);
        }

        CreateNewDocument();
    }

    private void OnInputFieldChanged(string value)
    {
        OnTextChanged();
    }

    // Создание нового документа
    public void CreateNewDocument()
    {
        currentDocument = new TextDocument();
        currentFilePath = "";
        UpdateUI();

        Debug.Log("Создан новый документ");
    }

    // Кнопка «Открыть» — показывает список файлов вместо жёсткого пути
    public void OpenDocumentRequested()
    {
        if (fileBrowser != null)
        {
            fileBrowser.ShowFileList(DocumentsFolder);
        }
        else
        {
            Debug.LogWarning("Панель выбора файла не назначена в инспекторе");
        }
    }

    // Вызывается FileBrowserController, когда пользователь выбрал файл в списке
    public void LoadDocument(string filePath)
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
            currentDocument.filePath = filePath;
            currentFilePath = filePath;
            currentDocument.UpdateStatistics();

            UpdateUI();
            Debug.Log("Документ загружен: " + filePath);
        }
        catch (Exception e)
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

    // Кнопка «Сохранить как» — показывает панель ввода имени файла
    public void SaveDocumentAs()
    {
        if (saveAsPanel != null)
        {
            saveAsPanel.Show(currentDocument.documentName);
        }
        else
        {
            Debug.LogWarning("Панель сохранения не назначена в инспекторе");
        }
    }

    // Вызывается SaveAsController, когда пользователь подтвердил имя файла
    public void SaveDocumentAsNamed(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "Новый документ";
        }

        // Убираем символы, запрещённые в именах файлов
        foreach (char forbidden in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(forbidden, '_');
        }

        fileName = fileName.Trim();

        if (!fileName.EndsWith(".txt"))
        {
            fileName += ".txt";
        }

        SaveToFile(Path.Combine(DocumentsFolder, fileName));
    }

    private void SaveToFile(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, currentDocument.content);
            currentFilePath = filePath;
            currentDocument.filePath = filePath;
            currentDocument.documentName = Path.GetFileName(filePath);

            UpdateUI();
            Debug.Log("Документ сохранён: " + filePath);
        }
        catch (Exception e)
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
            // Звёздочка означает, что документ ещё ни разу не сохраняли в файл
            string mark = string.IsNullOrEmpty(currentFilePath) ? " *" : "";
            titleText.text = currentDocument.documentName + mark;
        }

        UpdateStatus();
    }

    // Обновление статусной строки
    private void UpdateStatus()
    {
        if (statusText != null && currentDocument != null)
        {
            statusText.text = string.Format("Символов: {0} | Слов: {1} | Строк: {2}",
                currentDocument.characterCount,
                currentDocument.wordCount,
                currentDocument.lineCount);
        }
    }

    // Поиск текста с подсветкой
    public void SearchText(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm) || textInputField == null)
        {
            return;
        }

        int index = textInputField.text.IndexOf(
            searchTerm, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            if (statusText != null)
            {
                statusText.text = "Не найдено: " + searchTerm;
            }

            Debug.Log("Текст не найден: " + searchTerm);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SelectRange(index, searchTerm.Length));
    }

    // Поле сначала активируется, а выделение выставляется в конце кадра:
    // иначе TMP_InputField сбрасывает его в момент получения фокуса
    private IEnumerator SelectRange(int start, int length)
    {
        textInputField.Select();
        textInputField.ActivateInputField();

        yield return new WaitForEndOfFrame();

        textInputField.caretPosition = start + length;
        textInputField.selectionAnchorPosition = start;
        textInputField.selectionFocusPosition = start + length;
        textInputField.ForceLabelUpdate();
    }
}
