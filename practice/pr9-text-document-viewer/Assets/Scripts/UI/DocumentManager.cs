using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class DocumentManager : MonoBehaviour
{
    [Header("UI Components")]
    public InputField textInputField;
    public Text statusText;
    public Text titleText;
    public Text messageText;

    [Header("Диалоги")]
    public FileBrowserController fileBrowser;
    public SaveDialogController saveDialog;
    public ConfirmDialog confirmDialog;

    [Header("Document Settings")]
    public float messageDuration = 3f;

    private TextDocument currentDocument;
    private string currentFilePath = "";
    private bool hasUnsavedChanges;
    private string lastSearchTerm = "";
    private int lastFoundIndex = -1;

    public TextDocument CurrentDocument { get { return currentDocument; } }
    public string CurrentFilePath { get { return currentFilePath; } }
    public bool HasUnsavedChanges { get { return hasUnsavedChanges; } }

    void Start()
    {
        // Создаём новый документ при старте
        CreateNewDocumentInternal();

        // Подписываемся на изменение текста
        if (textInputField != null)
        {
            textInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        // Обновляем интерфейс
        UpdateUI();
    }

    private void OnInputFieldValueChanged(string newText)
    {
        OnTextChanged();
    }

    // ---------- Создание документа ----------

    public void CreateNewDocument()
    {
        AskToSaveChanges(CreateNewDocumentInternal);
    }

    private void CreateNewDocumentInternal()
    {
        currentDocument = new TextDocument();
        currentFilePath = "";
        hasUnsavedChanges = false;
        ResetSearch();
        UpdateUI();

        Debug.Log("Создан новый документ");
    }

    // ---------- Загрузка документа ----------

    // Кнопка «Открыть»: показываем список документов
    public void LoadDocument()
    {
        AskToSaveChanges(ShowFileBrowser);
    }

    private void ShowFileBrowser()
    {
        if (fileBrowser != null)
        {
            fileBrowser.Show();
        }
        else
        {
            Debug.LogWarning("Панель выбора файла не назначена в инспекторе");
        }
    }

    // Открытие конкретного файла
    public void OpenDocument(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Файл не найден: " + filePath);
            ShowMessage("Файл не найден: " + Path.GetFileName(filePath));
            return;
        }

        try
        {
            currentDocument = new TextDocument();
            currentDocument.content = File.ReadAllText(filePath);
            currentDocument.documentName = Path.GetFileName(filePath);
            currentDocument.filePath = filePath;
            currentDocument.createdDate = File.GetCreationTime(filePath);
            currentDocument.UpdateStatistics();

            currentFilePath = filePath;
            hasUnsavedChanges = false;
            ResetSearch();
            UpdateUI();

            if (RecentFilesManager.Instance != null)
            {
                RecentFilesManager.Instance.Add(filePath);
            }

            ShowMessage("Документ открыт: " + currentDocument.documentName);
            Debug.Log("Документ загружен: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Ошибка загрузки: " + e.Message);
            ShowMessage("Ошибка загрузки файла");
        }
    }

    // ---------- Сохранение документа ----------

    public void SaveDocument()
    {
        if (string.IsNullOrEmpty(currentFilePath))
        {
            SaveDocumentAs();
            return;
        }

        SaveToFile(currentFilePath);
    }

    public void SaveDocumentAs()
    {
        if (saveDialog != null)
        {
            saveDialog.Show(currentDocument.documentName);
        }
        else
        {
            // Если диалог не назначен — сохраняем под текущим именем
            SaveWithName(currentDocument.documentName);
        }
    }

    // Вызывается из диалога сохранения
    public void SaveWithName(string fileName)
    {
        SaveToFile(DocumentPaths.GetFullPath(fileName));
    }

    private void SaveToFile(string filePath)
    {
        try
        {
            // Создаём директорию, если её не существует
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Сохраняем файл
            File.WriteAllText(filePath, currentDocument.content);

            currentFilePath = filePath;
            currentDocument.filePath = filePath;
            currentDocument.documentName = Path.GetFileName(filePath);
            hasUnsavedChanges = false;

            if (RecentFilesManager.Instance != null)
            {
                RecentFilesManager.Instance.Add(filePath);
            }

            UpdateUI();
            ShowMessage("Документ сохранён: " + currentDocument.documentName);
            Debug.Log("Документ сохранён: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Ошибка сохранения: " + e.Message);
            ShowMessage("Ошибка сохранения файла");
        }
    }

    // ---------- Обновление интерфейса ----------

    // Обновление содержимого документа из UI
    public void OnTextChanged()
    {
        if (currentDocument != null && textInputField != null)
        {
            currentDocument.content = textInputField.text;
            currentDocument.UpdateStatistics();
            hasUnsavedChanges = true;
            UpdateTitle();
            UpdateStatus();
        }
    }

    private void UpdateUI()
    {
        if (textInputField != null)
        {
            // Снимаем подписку, чтобы установка текста не помечала документ изменённым
            textInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            textInputField.text = currentDocument.content;
            textInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }

        UpdateTitle();
        UpdateStatus();
    }

    private void UpdateTitle()
    {
        if (titleText != null)
        {
            titleText.text = currentDocument.documentName + (hasUnsavedChanges ? " *" : "");
        }
    }

    // Обновление статусной строки
    private void UpdateStatus()
    {
        if (statusText != null && currentDocument != null)
        {
            statusText.text = string.Format(
                "Символов: {0} (без пробелов: {1}) | Слов: {2} | Строк: {3} | Абзацев: {4} | Чтение: ~{5} мин",
                currentDocument.characterCount,
                currentDocument.charactersWithoutSpaces,
                currentDocument.wordCount,
                currentDocument.lineCount,
                currentDocument.paragraphCount,
                currentDocument.GetReadingTimeMinutes());
        }
    }

    // Временное сообщение для пользователя
    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        StopCoroutine("HideMessageAfterDelay");
        messageText.text = message;
        StartCoroutine("HideMessageAfterDelay");
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        messageText.text = "";
    }

    // ---------- Поиск и замена ----------

    // Поиск с начала документа
    public void SearchText(string searchTerm)
    {
        ResetSearch();
        FindNext(searchTerm);
    }

    // Поиск следующего вхождения (по кругу)
    public bool FindNext(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm) || textInputField == null)
        {
            return false;
        }

        if (searchTerm != lastSearchTerm)
        {
            lastSearchTerm = searchTerm;
            lastFoundIndex = -1;
        }

        string content = textInputField.text;
        int startIndex = lastFoundIndex + 1;

        if (startIndex > content.Length)
        {
            startIndex = 0;
        }

        int index = content.IndexOf(searchTerm, startIndex, StringComparison.OrdinalIgnoreCase);

        // Если до конца текста не нашли — ищем с начала
        if (index < 0 && startIndex > 0)
        {
            index = content.IndexOf(searchTerm, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (index < 0)
        {
            lastFoundIndex = -1;
            return false;
        }

        lastFoundIndex = index;
        SelectRange(index, searchTerm.Length);
        return true;
    }

    // Выделение найденного фрагмента
    private void SelectRange(int start, int length)
    {
        textInputField.Select();
        textInputField.ActivateInputField();
        textInputField.caretPosition = start + length;
        textInputField.selectionAnchorPosition = start;
        textInputField.selectionFocusPosition = start + length;
        textInputField.ForceLabelUpdate();
    }

    // Количество вхождений в документе
    public int CountOccurrences(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm) || textInputField == null)
        {
            return 0;
        }

        int count = 0;
        int position = 0;
        string content = textInputField.text;

        while (position <= content.Length - searchTerm.Length)
        {
            int index = content.IndexOf(searchTerm, position, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                break;
            }

            count++;
            position = index + searchTerm.Length;
        }

        return count;
    }

    // Замена текущего найденного вхождения
    public bool ReplaceCurrent(string searchTerm, string replacement)
    {
        if (string.IsNullOrEmpty(searchTerm) || textInputField == null)
        {
            return false;
        }

        if (lastFoundIndex < 0 && !FindNext(searchTerm))
        {
            return false;
        }

        string content = textInputField.text;
        textInputField.text = content.Substring(0, lastFoundIndex)
            + replacement
            + content.Substring(lastFoundIndex + searchTerm.Length);

        lastFoundIndex = lastFoundIndex + replacement.Length - 1;
        OnTextChanged();
        return true;
    }

    // Замена всех вхождений, возвращает количество замен
    public int ReplaceAll(string searchTerm, string replacement)
    {
        if (string.IsNullOrEmpty(searchTerm) || textInputField == null)
        {
            return 0;
        }

        int count;
        textInputField.text = ReplaceIgnoreCase(textInputField.text, searchTerm, replacement, out count);

        ResetSearch();
        OnTextChanged();
        return count;
    }

    private string ReplaceIgnoreCase(string source, string oldValue, string newValue, out int count)
    {
        count = 0;
        StringBuilder builder = new StringBuilder();
        int position = 0;

        while (true)
        {
            int index = source.IndexOf(oldValue, position, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                break;
            }

            builder.Append(source, position, index - position);
            builder.Append(newValue);
            position = index + oldValue.Length;
            count++;
        }

        builder.Append(source, position, source.Length - position);
        return builder.ToString();
    }

    private void ResetSearch()
    {
        lastSearchTerm = "";
        lastFoundIndex = -1;
    }

    // ---------- Защита от потери данных ----------

    private void AskToSaveChanges(Action continueAction)
    {
        if (!hasUnsavedChanges || confirmDialog == null)
        {
            continueAction();
            return;
        }

        confirmDialog.Show("В документе есть несохранённые изменения.\nПродолжить без сохранения?", continueAction);
    }

    public void QuitApplication()
    {
        AskToSaveChanges(ExitNow);
    }

    private void ExitNow()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
