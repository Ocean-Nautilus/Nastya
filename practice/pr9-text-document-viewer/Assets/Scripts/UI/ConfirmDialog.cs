using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialog : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject dialogPanel;
    public Text messageText;
    public Button confirmButton;
    public Button cancelButton;

    private Action onConfirm;

    void Start()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    // Показать вопрос и запомнить действие, которое выполнится при согласии
    public void Show(string message, Action confirmAction)
    {
        onConfirm = confirmAction;

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }
    }

    public void OnConfirmClicked()
    {
        Action action = onConfirm;
        onConfirm = null;
        Hide();

        if (action != null)
        {
            action();
        }
    }

    public void OnCancelClicked()
    {
        onConfirm = null;
        Hide();
    }

    private void Hide()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }
}
