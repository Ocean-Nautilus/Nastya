using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class RecentFilesManager : MonoBehaviour
{
    public static RecentFilesManager Instance { get; private set; }

    [Header("Связи")]
    public DocumentManager documentManager;

    [Header("UI Components")]
    public Dropdown recentDropdown;

    [Header("Настройки")]
    public int maxRecentFiles = 5;

    private const string PrefsKey = "RecentFiles";
    private const char Separator = '|';

    private readonly List<string> recentFiles = new List<string>();

    void Awake()
    {
        Instance = this;
        Load();
    }

    void Start()
    {
        RefreshDropdown();

        if (recentDropdown != null)
        {
            recentDropdown.onValueChanged.AddListener(OnRecentSelected);
        }
    }

    // Добавление файла в начало списка
    public void Add(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        recentFiles.Remove(filePath);
        recentFiles.Insert(0, filePath);

        while (recentFiles.Count > maxRecentFiles)
        {
            recentFiles.RemoveAt(recentFiles.Count - 1);
        }

        Save();
        RefreshDropdown();
    }

    public List<string> GetFiles()
    {
        return new List<string>(recentFiles);
    }

    public void Clear()
    {
        recentFiles.Clear();
        Save();
        RefreshDropdown();
    }

    private void OnRecentSelected(int index)
    {
        // Нулевой пункт — подпись «Недавние документы»
        int fileIndex = index - 1;

        if (fileIndex < 0 || fileIndex >= recentFiles.Count || documentManager == null)
        {
            return;
        }

        documentManager.OpenDocument(recentFiles[fileIndex]);
        recentDropdown.SetValueWithoutNotify(0);
    }

    private void RefreshDropdown()
    {
        if (recentDropdown == null)
        {
            return;
        }

        List<string> options = new List<string> { "Недавние документы" };

        foreach (string path in recentFiles)
        {
            options.Add(Path.GetFileNameWithoutExtension(path));
        }

        recentDropdown.ClearOptions();
        recentDropdown.AddOptions(options);
        recentDropdown.SetValueWithoutNotify(0);
    }

    private void Load()
    {
        recentFiles.Clear();

        string saved = PlayerPrefs.GetString(PrefsKey, "");

        if (string.IsNullOrEmpty(saved))
        {
            return;
        }

        foreach (string path in saved.Split(Separator))
        {
            // В список попадают только существующие файлы
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                recentFiles.Add(path);
            }
        }
    }

    private void Save()
    {
        PlayerPrefs.SetString(PrefsKey, string.Join(Separator.ToString(), recentFiles.ToArray()));
        PlayerPrefs.Save();
    }
}
