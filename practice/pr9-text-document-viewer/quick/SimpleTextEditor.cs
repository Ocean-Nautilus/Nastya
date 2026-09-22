using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// Минимальный текстовый редактор: создание, сохранение, открытие,
// статистика и поиск. Один скрипт на один объект сцены.
public class SimpleTextEditor : MonoBehaviour
{
    [Header("Перетащите сюда объекты сцены")]
    public InputField textField;      // поле с текстом документа
    public InputField fileNameField;  // имя файла
    public InputField searchField;    // что искать
    public Dropdown fileList;         // список сохранённых документов
    public Text statusText;           // строка статистики

    // Папка, в которую можно писать и в редакторе, и в готовой сборке
    private string Folder
    {
        get
        {
            string path = Path.Combine(Application.persistentDataPath, "Documents");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }

    void Start()
    {
        textField.onValueChanged.AddListener(delegate { UpdateStatus(); });
        RefreshFileList();
        NewDocument();
        Debug.Log("Документы лежат в папке: " + Folder);
    }

    // Кнопка «Новый»
    public void NewDocument()
    {
        textField.text = "";
        fileNameField.text = "Новый документ";
        UpdateStatus();
    }

    // Кнопка «Сохранить»
    public void Save()
    {
        string name = fileNameField.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            statusText.text = "Введите имя файла";
            return;
        }

        try
        {
            File.WriteAllText(Path.Combine(Folder, name + ".txt"), textField.text);
            RefreshFileList();
            statusText.text = "Сохранено: " + name + ".txt";
        }
        catch (Exception e)
        {
            statusText.text = "Ошибка сохранения";
            Debug.LogError(e.Message);
        }
    }

    // Кнопка «Открыть» — открывает документ, выбранный в списке
    public void Open()
    {
        if (fileList.options.Count == 0)
        {
            statusText.text = "Нет сохранённых документов";
            return;
        }

        string name = fileList.options[fileList.value].text;
        string path = Path.Combine(Folder, name + ".txt");

        if (!File.Exists(path))
        {
            statusText.text = "Файл не найден";
            RefreshFileList();
            return;
        }

        textField.text = File.ReadAllText(path);
        fileNameField.text = name;
        UpdateStatus();
    }

    // Кнопка «Найти» — выделяет первое совпадение
    public void Search()
    {
        string term = searchField.text;
        if (string.IsNullOrEmpty(term))
        {
            return;
        }

        int index = textField.text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            statusText.text = "Не найдено: " + term;
            return;
        }

        textField.Select();
        textField.ActivateInputField();
        textField.caretPosition = index + term.Length;
        textField.selectionAnchorPosition = index;
        textField.selectionFocusPosition = index + term.Length;
    }

    // Список файлов в выпадающем меню
    private void RefreshFileList()
    {
        List<string> names = new List<string>();

        foreach (string file in Directory.GetFiles(Folder, "*.txt"))
        {
            names.Add(Path.GetFileNameWithoutExtension(file));
        }

        fileList.ClearOptions();
        fileList.AddOptions(names);
    }

    // Статистика документа
    private void UpdateStatus()
    {
        string text = textField.text;

        int words = string.IsNullOrEmpty(text)
            ? 0
            : text.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        int lines = string.IsNullOrEmpty(text) ? 0 : text.Split('\n').Length;

        statusText.text = string.Format("Символов: {0} | Слов: {1} | Строк: {2}",
            text.Length, words, lines);
    }
}
