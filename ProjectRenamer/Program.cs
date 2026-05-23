using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectRenamer
{
    class Program
    {
        private static readonly string[] TextExtensions = {
            ".cs", ".sln", ".csproj", ".json", ".xml", ".config",
            ".html", ".css", ".js", ".ts", ".txt", ".md", ".xaml", ".cshtml"
        };

        static void Main(string[] args)
        {
            Console.WriteLine("=== Visual Studio Safe Copy & Bulk Renamer ===");

            Console.Write("Enter the full path to your source project folder: ");
            string sourcePath = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                SetConsoleColor(ConsoleColor.Red, "Invalid source directory path. Exiting program.");
                return;
            }

            Console.Write("Enter the OLD name to find (e.g., Ordering): ");
            string oldName = Console.ReadLine()?.Trim();

            Console.Write("Enter the NEW name to use (e.g., Reservation): ");
            string newName = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                SetConsoleColor(ConsoleColor.Red, "Old name and New name cannot be empty. Exiting program.");
                return;
            }

            string pascalNewName = char.ToUpper(newName[0]) + newName.Substring(1);

            DirectoryInfo sourceInfo = new DirectoryInfo(sourcePath);
            string parentDirectory = sourceInfo.Parent?.FullName;
            if (string.IsNullOrEmpty(parentDirectory))
            {
                SetConsoleColor(ConsoleColor.Red, "Could not determine the parent directory. Exiting.");
                return;
            }

            string targetPath = Path.Combine(parentDirectory, pascalNewName);

            if (Directory.Exists(targetPath))
            {
                try
                {
                    SetConsoleColor(ConsoleColor.Yellow, $"\nTarget directory already exists at:\n--> {targetPath}\nCleaning up existing folder to perform a fresh replacement...");
                    Directory.Delete(targetPath, true);
                }
                catch (Exception ex)
                {
                    SetConsoleColor(ConsoleColor.Red, $"Failed to clear existing target folder. It might be open in VS or File Explorer. Error: {ex.Message}");
                    return;
                }
            }

            try
            {
                Console.WriteLine($"\nInitializing fresh target folder at:\n--> {targetPath}");
                Directory.CreateDirectory(targetPath);

                Console.WriteLine("\nProcessing, refactoring, and copying structure safely...");
                CopyAndRefactorDirectory(sourcePath, targetPath, oldName, newName);

                SetConsoleColor(ConsoleColor.Green, "\nSuccess! A clean, renamed copy has been successfully generated.");
                Console.WriteLine($"Original folder left 100% untouched at: {sourcePath}");
                Console.WriteLine($"New refactored folder ready at:         {targetPath}");
            }
            catch (Exception ex)
            {
                SetConsoleColor(ConsoleColor.Red, $"\nAn unexpected error occurred during reproduction: {ex.Message}");
            }
        }

        private static void CopyAndRefactorDirectory(string sourceDir, string targetDir, string oldName, string newName)
        {
            string pascalNewName = char.ToUpper(newName[0]) + newName.Substring(1);

            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                string folderName = Path.GetFileName(dirPath);
                string folderNameLower = folderName.ToLower();

                if (folderNameLower == "bin" || folderNameLower == "obj" || folderNameLower == ".vs" || folderNameLower == ".git")
                {
                    continue;
                }

                string targetFolderName = folderName;
                if (folderName.Contains(oldName, StringComparison.OrdinalIgnoreCase))
                {
                    targetFolderName = Regex.Replace(folderName, Regex.Escape(oldName), pascalNewName, RegexOptions.IgnoreCase);
                }

                string nextTargetDir = Path.Combine(targetDir, targetFolderName);
                Directory.CreateDirectory(nextTargetDir);

                CopyAndRefactorDirectory(dirPath, nextTargetDir, oldName, newName);
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(filePath);
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".dll" || ext == ".exe" || ext == ".suo" || ext == ".user")
                {
                    continue;
                }

                string targetFileName = fileName;
                if (fileName.Contains(oldName, StringComparison.OrdinalIgnoreCase))
                {
                    targetFileName = Regex.Replace(fileName, Regex.Escape(oldName), pascalNewName, RegexOptions.IgnoreCase);
                }

                string targetFilePath = Path.Combine(targetDir, targetFileName);

                if (TextExtensions.Contains(ext))
                {
                    try
                    {
                        string contents = File.ReadAllText(filePath);

                        if (contents.Contains(oldName, StringComparison.OrdinalIgnoreCase))
                        {
                            contents = ReplaceWithCasingSensitivity(contents, oldName, newName);
                        }

                        File.WriteAllText(targetFilePath, contents);
                        Console.WriteLine($"Refactored Text File: {targetFileName}");
                    }
                    catch
                    {
                        SafeBinaryCopy(filePath, targetFilePath, targetFileName);
                    }
                }
                else
                {
                    SafeBinaryCopy(filePath, targetFilePath, targetFileName);
                }
            }
        }

        private static string ReplaceWithCasingSensitivity(string input, string oldWord, string newWord)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldWord) || string.IsNullOrEmpty(newWord))
                return input;

            string newPascal = char.ToUpper(newWord[0]) + newWord.Substring(1);
            string newLower = newWord.ToLower();
            string newUpper = newWord.ToUpper();

            string oldPascal = char.ToUpper(oldWord[0]) + oldWord.Substring(1);
            string oldLower = oldWord.ToLower();
            string oldUpper = oldWord.ToUpper();

            input = Regex.Replace(input, @"\b" + Regex.Escape(oldUpper) + @"\b", newUpper);
            input = Regex.Replace(input, @"\b" + Regex.Escape(oldPascal) + @"\b", newPascal);
            input = Regex.Replace(input, @"\b" + Regex.Escape(oldLower) + @"\b", newLower);
            input = Regex.Replace(input, @"\b" + Regex.Escape(oldWord) + @"\b", newPascal);

            return input;
        }

        private static void SafeBinaryCopy(string source, string target, string targetName)
        {
            try
            {
                File.Copy(source, target, true);
                Console.WriteLine($"Copied Raw Binary:    {targetName}");
            }
            catch (Exception ex)
            {
                SetConsoleColor(ConsoleColor.Yellow, $"Warning: Could not duplicate {targetName}. Error: {ex.Message}");
            }
        }

        private static void SetConsoleColor(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}