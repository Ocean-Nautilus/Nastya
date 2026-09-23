using System;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class TextDocument
{
    public string documentName;
    public string filePath;
    public string content;
    public DateTime createdDate;
    public DateTime modifiedDate;
    public int characterCount;
    public int wordCount;
    public int lineCount;

    // Теги форматирования (<b>, <i>, <u>, <size=..>) хранятся прямо в тексте,
    // но в статистику попадать не должны - считаем только видимый текст
    private static readonly Regex RichTextTag = new Regex(@"</?(b|i|u|s|size|font|color)(=[^>]*)?>", RegexOptions.IgnoreCase);

    public TextDocument()
    {
        documentName = "Новый документ";
        filePath = "";
        content = "";
        createdDate = DateTime.Now;
        modifiedDate = DateTime.Now;
        UpdateStatistics();
    }

    public static string StripFormatting(string text)
    {
        return string.IsNullOrEmpty(text) ? "" : RichTextTag.Replace(text, "");
    }

    public void UpdateStatistics()
    {
        string plain = StripFormatting(content);

        characterCount = plain.Length;
        wordCount = string.IsNullOrWhiteSpace(plain) ? 0 : plain.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        lineCount = string.IsNullOrEmpty(plain) ? 0 : plain.Split('\n').Length;
        modifiedDate = DateTime.Now;
    }

    public void Clear()
    {
        content = "";
        documentName = "Новый документ";
        filePath = "";
        UpdateStatistics();
    }
}
