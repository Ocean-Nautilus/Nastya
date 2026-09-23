using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FormatController : MonoBehaviour
{
    public TMP_InputField textInputField;
    public TMP_Dropdown fontDropdown;
    public TMP_InputField fontSizeInput;

    [Header("Font Settings")]
    // Папка внутри Resources, из которой загружаются шрифты (.ttf / .otf)
    public string fontsResourcesPath = "Fonts";
    public int minFontSize = 8;
    public int maxFontSize = 72;

    private readonly List<TMP_FontAsset> fontAssets = new List<TMP_FontAsset>();

    // Последнее выделение в тексте. Запоминаем его, пока поле в фокусе:
    // при нажатии на кнопку фокус уходит, и читать выделение уже поздно.
    private int selectionStart;
    private int selectionEnd;

    private string lastAppliedSize = "";

    void Start()
    {
        if (textInputField != null)
        {
            textInputField.resetOnDeActivation = false;
            textInputField.onFocusSelectAll = false;
        }

        InitFontDropdown();

        if (fontSizeInput != null && textInputField != null)
        {
            lastAppliedSize = Mathf.RoundToInt(textInputField.pointSize).ToString();
            fontSizeInput.SetTextWithoutNotify(lastAppliedSize);
        }
    }

    void Update()
    {
        if (textInputField != null && textInputField.isFocused)
        {
            InputFieldSelection.GetRange(textInputField, out selectionStart, out selectionEnd);
        }
    }

    // ---------- Стили: жирный, курсив, подчёркнутый ----------

    public void OnBoldButtonClicked()
    {
        ApplyFormatting("<b>", "</b>");
    }

    public void OnItalicButtonClicked()
    {
        ApplyFormatting("<i>", "</i>");
    }

    public void OnUnderlineButtonClicked()
    {
        ApplyFormatting("<u>", "</u>");
    }

    // TMP поддерживает Rich Text, поэтому стиль задаётся тегами вокруг выделенного фрагмента.
    // Повторное нажатие на уже оформленный фрагмент снимает стиль.
    private void ApplyFormatting(string prefix, string suffix)
    {
        if (textInputField == null) return;

        string text = textInputField.text;
        int start = Mathf.Clamp(selectionStart, 0, text.Length);
        int end = Mathf.Clamp(selectionEnd, start, text.Length);

        if (start == end)
        {
            Debug.Log("Выделите текст, чтобы применить форматирование");
            return;
        }

        string selectedText = text.Substring(start, end - start);

        // Теги вокруг выделения могут идти не вплотную, а через другие теги:
        // <b><i>[текст]</i></b> - поэтому ищем среди всех тегов, примыкающих к выделению
        int prefixIndex = FindAdjacentTagBefore(text, start, prefix);
        int suffixIndex = FindAdjacentTagAfter(text, end, suffix);

        bool tagsInsideSelection = selectedText.Length >= prefix.Length + suffix.Length
            && selectedText.StartsWith(prefix)
            && selectedText.EndsWith(suffix);

        if (prefixIndex >= 0 && suffixIndex >= 0)
        {
            // <b>[текст]</b> -> [текст]
            text = text.Remove(suffixIndex, suffix.Length).Remove(prefixIndex, prefix.Length);
            start -= prefix.Length;
            end -= prefix.Length;
        }
        else if (tagsInsideSelection)
        {
            // [<b>текст</b>] -> [текст]
            string inner = selectedText.Substring(prefix.Length, selectedText.Length - prefix.Length - suffix.Length);
            text = text.Substring(0, start) + inner + text.Substring(end);
            end = start + inner.Length;
        }
        else
        {
            // [текст] -> <b>[текст]</b>
            text = text.Substring(0, start) + prefix + selectedText + suffix + text.Substring(end);
            start += prefix.Length;
            end += prefix.Length;
        }

        SetTextAndSelect(text, start, end);
    }

    // ---------- Размер шрифта ----------

    // Вызывается из On End Edit поля размера.
    // Есть выделение - меняется размер только выделенного фрагмента (тег <size>),
    // нет выделения - размер всего текста.
    public void OnFontSizeChanged()
    {
        if (textInputField == null || fontSizeInput == null) return;

        string value = fontSizeInput.text.Trim();
        if (value == lastAppliedSize)
            return; // поле просто потеряло фокус, размер не менялся

        if (!int.TryParse(value, out int size))
        {
            fontSizeInput.SetTextWithoutNotify(lastAppliedSize);
            return;
        }

        size = Mathf.Clamp(size, minFontSize, maxFontSize);
        lastAppliedSize = size.ToString();
        fontSizeInput.SetTextWithoutNotify(lastAppliedSize);

        string text = textInputField.text;
        int start = Mathf.Clamp(selectionStart, 0, text.Length);
        int end = Mathf.Clamp(selectionEnd, start, text.Length);

        if (end > start)
        {
            string selectedText = text.Substring(start, end - start);
            string prefix = $"<size={size}>";
            text = text.Substring(0, start) + prefix + selectedText + "</size>" + text.Substring(end);
            SetTextAndSelect(text, start + prefix.Length, end + prefix.Length);
        }
        else
        {
            textInputField.pointSize = size;
        }
    }

    // ---------- Шрифты ----------

    // Список шрифтов: текущий шрифт поля + все шрифты из Resources/Fonts.
    // Для .ttf на лету создаётся динамический TMP_FontAsset, поэтому
    // вручную делать SDF-атласы через Font Asset Creator не нужно.
    private void InitFontDropdown()
    {
        if (fontDropdown == null || textInputField == null) return;

        fontAssets.Clear();
        var options = new List<string>();

        TMP_FontAsset defaultFont = textInputField.fontAsset != null ? textInputField.fontAsset : textInputField.textComponent.font;
        if (defaultFont != null)
        {
            fontAssets.Add(defaultFont);
            options.Add(defaultFont.faceInfo.familyName);
        }

        Font[] fonts = Resources.LoadAll<Font>(fontsResourcesPath);
        foreach (Font font in fonts)
        {
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
            if (asset == null)
            {
                Debug.LogWarning("Не удалось создать шрифт TMP из " + font.name);
                continue;
            }

            asset.name = font.name;

            // Символы, которых нет в шрифте, берутся из шрифта по умолчанию
            if (defaultFont != null)
            {
                if (asset.fallbackFontAssetTable == null)
                    asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                asset.fallbackFontAssetTable.Add(defaultFont);
            }

            fontAssets.Add(asset);
            options.Add(string.IsNullOrEmpty(asset.faceInfo.familyName) ? font.name : asset.faceInfo.familyName);
        }

        fontDropdown.ClearOptions();
        fontDropdown.AddOptions(options);
        fontDropdown.SetValueWithoutNotify(0);
        fontDropdown.RefreshShownValue();
    }

    // Вызывается из On Value Changed выпадающего списка шрифтов
    public void OnFontChanged(int index)
    {
        if (textInputField == null || index < 0 || index >= fontAssets.Count) return;

        // fontAsset у TMP_InputField меняет шрифт и текста, и подсказки (Placeholder)
        textInputField.fontAsset = fontAssets[index];
        textInputField.ForceLabelUpdate();
    }

    // ---------- Вспомогательное ----------

    // Ищет тег среди тегов, идущих подряд сразу перед позицией position
    private static int FindAdjacentTagBefore(string text, int position, string tag)
    {
        int index = position;
        while (index > 0 && text[index - 1] == '>')
        {
            int open = text.LastIndexOf('<', index - 1);
            if (open < 0)
                break;

            if (index - open == tag.Length && string.CompareOrdinal(text, open, tag, 0, tag.Length) == 0)
                return open;

            index = open;
        }
        return -1;
    }

    // Ищет тег среди тегов, идущих подряд сразу после позиции position
    private static int FindAdjacentTagAfter(string text, int position, string tag)
    {
        int index = position;
        while (index < text.Length && text[index] == '<')
        {
            int close = text.IndexOf('>', index);
            if (close < 0)
                break;

            if (close + 1 - index == tag.Length && string.CompareOrdinal(text, index, tag, 0, tag.Length) == 0)
                return index;

            index = close + 1;
        }
        return -1;
    }

    private void SetTextAndSelect(string newText, int start, int end)
    {
        // Обычное присваивание text вызывает On Value Changed -
        // DocumentManager пересчитает статистику и пометит документ изменённым
        textInputField.text = newText;

        selectionStart = start;
        selectionEnd = end;
        StartCoroutine(InputFieldSelection.Select(textInputField, start, end));
    }
}
