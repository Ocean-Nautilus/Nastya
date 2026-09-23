using UnityEngine;
using TMPro;

public class FormatController : MonoBehaviour
{
    public TMP_InputField textInputField;
    public TMP_Dropdown fontDropdown;
    public TMP_InputField fontSizeInput;

    public void OnBoldButtonClicked()
    {
        ApplyFormatting("<b>", "</b>");
    }

    public void OnItalicButtonClicked()
    {
        ApplyFormatting("<i>", "</i>");
    }

    // В сцене есть кнопка Underline, но метода для неё не было - добавлен
    public void OnUnderlineButtonClicked()
    {
        ApplyFormatting("<u>", "</u>");
    }

    public void OnFontSizeChanged()
    {
        // TMP поддерживает Rich Text, поэтому размер шрифта
        // выделенного фрагмента можно менять тегом <size=...></size>,
        // а размер всего поля - через textInputField.pointSize
    }

    private void ApplyFormatting(string prefix, string suffix)
    {
        if (textInputField == null) return;

        string selectedText = GetSelectedText();
        if (!string.IsNullOrEmpty(selectedText))
        {
            string newText = prefix + selectedText + suffix;
            ReplaceSelectedText(newText);
        }
    }

    private string GetSelectedText()
    {
        // Получение выделенного текста
        int start = textInputField.selectionAnchorPosition;
        int end = textInputField.selectionFocusPosition;

        if (start > end)
        {
            int temp = start;
            start = end;
            end = temp;
        }

        if (start >= 0 && end <= textInputField.text.Length)
        {
            return textInputField.text.Substring(start, end - start);
        }

        return "";
    }

    private void ReplaceSelectedText(string newText)
    {
        int start = textInputField.selectionAnchorPosition;
        int end = textInputField.selectionFocusPosition;

        if (start > end)
        {
            int temp = start;
            start = end;
            end = temp;
        }

        if (start >= 0 && end <= textInputField.text.Length)
        {
            string currentText = textInputField.text;
            textInputField.text = currentText.Substring(0, start) + newText + currentText.Substring(end);
        }
    }
}
