using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FileBrowserController : MonoBehaviour
{
    [Header("Связи")]
    public DocumentManager documentManager;

    [Header("UI Components")]
    public GameObject browserPanel;
    public Transform fileListContent;
    public GameObject fileButtonPrefab;
    public Text emptyListText;
    public Text folderPathText;

    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    void Start()
    {
        if (browserPanel != null)
        {
            browserPanel.SetActive(false);
        }
    }

    // Открыть панель и показать список документов
    public void Show()
    {
        if (browserPanel != null)
        {
            browserPanel.SetActive(true);
        }

        if (folderPathText != null)
        {
            folderPathText.text = "Папка документов: " + DocumentPaths.DocumentsFolder;
        }

        RefreshFileList();
    }

    public void Hide()
    {
        if (browserPanel != null)
        {
            browserPanel.SetActive(false);
        }
    }

    // Построение списка файлов
    public void RefreshFileList()
    {
        ClearFileList();

        string[] files = DocumentPaths.GetDocumentFiles();

        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(files.Length == 0);
            emptyListText.text = "Пока нет ни одного документа";
        }

        for (int i = 0; i < files.Length; i++)
        {
            CreateFileButton(files[i]);
        }
    }

    private void CreateFileButton(string filePath)
    {
        if (fileButtonPrefab == null || fileListContent == null)
        {
            return;
        }

        GameObject buttonObject = Instantiate(fileButtonPrefab, fileListContent);
        spawnedButtons.Add(buttonObject);

        Text label = buttonObject.GetComponentInChildren<Text>();
        if (label != null)
        {
            FileInfo info = new FileInfo(filePath);
            label.text = string.Format("{0}    ({1:dd.MM.yyyy HH:mm}, {2} КБ)",
                Path.GetFileNameWithoutExtension(filePath),
                info.LastWriteTime,
                Mathf.Max(1, Mathf.RoundToInt(info.Length / 1024f)));
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            string path = filePath;
            button.onClick.AddListener(delegate { OnFileSelected(path); });
        }
    }

    private void OnFileSelected(string filePath)
    {
        if (documentManager != null)
        {
            documentManager.OpenDocument(filePath);
        }

        Hide();
    }

    private void ClearFileList()
    {
        foreach (GameObject buttonObject in spawnedButtons)
        {
            Destroy(buttonObject);
        }

        spawnedButtons.Clear();
    }
}
