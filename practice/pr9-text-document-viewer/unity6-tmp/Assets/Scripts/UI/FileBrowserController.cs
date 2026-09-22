using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FileBrowserController : MonoBehaviour
{
    [Header("Ссылки")]
    public DocumentManager documentManager;
    public GameObject panelRoot;
    public Transform contentParent;
    public Button fileButtonPrefab;
    public TMP_Text emptyText;

    void Start()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void ShowFileList(string folderPath)
    {
        // Очищаем старые кнопки перед перестроением списка
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        Directory.CreateDirectory(folderPath);
        string[] files = Directory.GetFiles(folderPath, "*.txt");

        foreach (string filePath in files)
        {
            Button btn = Instantiate(fileButtonPrefab, contentParent);
            string capturedPath = filePath;

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = Path.GetFileName(filePath);
            }

            btn.onClick.AddListener(() => OnFileSelected(capturedPath));
        }

        if (emptyText != null)
        {
            emptyText.text = "Пока нет ни одного документа";
            emptyText.gameObject.SetActive(files.Length == 0);
        }

        panelRoot.SetActive(true);
    }

    private void OnFileSelected(string filePath)
    {
        documentManager.LoadDocument(filePath);
        panelRoot.SetActive(false);
    }

    public void ClosePanel()
    {
        panelRoot.SetActive(false);
    }
}
