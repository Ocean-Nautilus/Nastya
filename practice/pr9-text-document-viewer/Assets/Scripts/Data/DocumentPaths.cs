using System.IO;
using UnityEngine;

// Единая точка, которая знает, где приложение хранит документы.
// В редакторе и в собранном приложении путь разный, поэтому используем
// persistentDataPath: в готовой сборке папка Assets доступна только для чтения.
public static class DocumentPaths
{
    public const string FileExtension = ".txt";

    public static string DocumentsFolder
    {
        get
        {
            string folder = Path.Combine(Application.persistentDataPath, "Documents");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }
    }

    // Полный путь к файлу по его имени
    public static string GetFullPath(string fileName)
    {
        fileName = MakeSafeFileName(fileName);

        if (!fileName.EndsWith(FileExtension))
        {
            fileName += FileExtension;
        }

        return Path.Combine(DocumentsFolder, fileName);
    }

    // Список всех текстовых документов, отсортированный по дате изменения
    public static string[] GetDocumentFiles()
    {
        string[] files = Directory.GetFiles(DocumentsFolder, "*" + FileExtension);
        System.Array.Sort(files, CompareByWriteTime);
        return files;
    }

    private static int CompareByWriteTime(string first, string second)
    {
        return File.GetLastWriteTime(second).CompareTo(File.GetLastWriteTime(first));
    }

    // Убираем символы, запрещённые в именах файлов
    public static string MakeSafeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return "Новый документ";
        }

        foreach (char forbidden in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(forbidden, '_');
        }

        return fileName.Trim();
    }
}
