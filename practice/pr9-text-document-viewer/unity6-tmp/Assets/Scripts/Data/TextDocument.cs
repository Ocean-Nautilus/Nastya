using System;
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

    public TextDocument()
    {
        documentName = "Новый документ";
        filePath = "";
        content = "";
        createdDate = DateTime.Now;
        modifiedDate = DateTime.Now;
        UpdateStatistics();
    }

    public void UpdateStatistics()
    {
        if (content == null)
        {
            content = "";
        }

        characterCount = content.Length;

        char[] separators = new char[] { ' ', '\n', '\r', '\t' };

        wordCount = string.IsNullOrEmpty(content)
            ? 0
            : content.Split(separators, StringSplitOptions.RemoveEmptyEntries).Length;

        lineCount = string.IsNullOrEmpty(content) ? 0 : content.Split('\n').Length;
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
