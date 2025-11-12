using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SpaceToWall.Installer
{
    class Program
    {
        private const string ADDIN_NAME = "space_to_wall.app";
        
        static void Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("   Space to Wall - Installateur Revit");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            try
            {
                // Déterminer le chemin du zip
                string zipPath = DetermineZipPath(args);
                
                if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
                {
                    Console.WriteLine("Erreur: Fichier ZIP introuvable.");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  space-to-wall.installer.exe [chemin-vers-zip]");
                    Console.WriteLine("  ou placez le ZIP dans le même dossier que l'installateur");
                    Console.WriteLine();
                    Environment.Exit(1);
                }

                Console.WriteLine($"Fichier trouvé: {Path.GetFileName(zipPath)}");
                Console.WriteLine();

                // Déterminer la version de Revit à partir du nom du zip ou demander
                string revitVersion = DetermineRevitVersion(zipPath);
                
                Console.WriteLine($"Version Revit cible: {revitVersion}");
                Console.WriteLine();

                // Installer
                InstallAddin(zipPath, revitVersion);

                Console.WriteLine();
                Console.WriteLine("===========================================");
                Console.WriteLine("Installation terminée avec succès !");
                Console.WriteLine("===========================================");
                Console.WriteLine();
                Console.WriteLine("Redémarrez Revit pour voir l'extension.");
                Console.WriteLine();
                Console.WriteLine("Appuyez sur une touche pour quitter...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"ERREUR: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Appuyez sur une touche pour quitter...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        static string DetermineZipPath(string[] args)
        {
            // 1. Vérifier si un chemin a été fourni en argument
            if (args.Length > 0 && File.Exists(args[0]))
            {
                return args[0];
            }

            // 2. Chercher dans le répertoire courant
            string currentDir = Directory.GetCurrentDirectory();
            var zipFiles = Directory.GetFiles(currentDir, $"{ADDIN_NAME}*.zip");
            
            if (zipFiles.Length > 0)
            {
                // Prendre le plus récent
                return zipFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();
            }

            // 3. Chercher dans le répertoire de l'exécutable
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            zipFiles = Directory.GetFiles(exeDir, $"{ADDIN_NAME}*.zip");
            
            if (zipFiles.Length > 0)
            {
                return zipFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();
            }

            return null;
        }

        static string DetermineRevitVersion(string zipPath)
        {
            // Essayer de détecter la version depuis le nom du fichier
            string fileName = Path.GetFileNameWithoutExtension(zipPath);
            
            if (fileName.Contains("2023"))
                return "2023";
            if (fileName.Contains("2024"))
                return "2024";
            if (fileName.Contains("2025"))
                return "2025";

            // Si pas trouvé, demander à l'utilisateur
            Console.WriteLine("Versions Revit disponibles:");
            Console.WriteLine("  1. Revit 2023");
            Console.WriteLine("  2. Revit 2024");
            Console.WriteLine("  3. Revit 2025");
            Console.WriteLine();
            Console.Write("Choisissez la version (1-3): ");
            
            string choice = Console.ReadLine();
            
            return choice switch
            {
                "1" => "2023",
                "2" => "2024",
                "3" => "2025",
                _ => "2023" // Par défaut
            };
        }

        static void InstallAddin(string zipPath, string revitVersion)
        {
            // Déterminer le dossier Addins de Revit
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string addinsPath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins", revitVersion);

            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(addinsPath))
            {
                Console.WriteLine($"Création du dossier: {addinsPath}");
                Directory.CreateDirectory(addinsPath);
            }

            Console.WriteLine($"Dossier d'installation: {addinsPath}");
            Console.WriteLine();

            // Créer un dossier temporaire pour extraire
            string tempPath = Path.Combine(Path.GetTempPath(), $"{ADDIN_NAME}_temp_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);

            try
            {
                Console.WriteLine("Extraction du contenu...");
                ZipFile.ExtractToDirectory(zipPath, tempPath);

                // Trouver le dossier contenant les fichiers (peut être dans un sous-dossier)
                string sourcePath = FindSourceFolder(tempPath, revitVersion);

                if (string.IsNullOrEmpty(sourcePath))
                {
                    throw new Exception($"Impossible de trouver les fichiers pour Revit {revitVersion} dans le ZIP");
                }

                // Copier le fichier .addin
                string addinFile = Path.Combine(sourcePath, $"{ADDIN_NAME}.addin");
                if (File.Exists(addinFile))
                {
                    string destAddin = Path.Combine(addinsPath, $"{ADDIN_NAME}.addin");
                    Console.WriteLine($"Installation: {Path.GetFileName(addinFile)}");
                    File.Copy(addinFile, destAddin, true);
                }
                else
                {
                    throw new Exception($"Fichier .addin introuvable: {addinFile}");
                }

                // Copier la DLL et ses dépendances dans un sous-dossier
                string targetFolder = Path.Combine(addinsPath, ADDIN_NAME);
                if (Directory.Exists(targetFolder))
                {
                    Console.WriteLine($"Suppression de l'ancienne version...");
                    Directory.Delete(targetFolder, true);
                }

                Console.WriteLine($"Création du dossier: {targetFolder}");
                Directory.CreateDirectory(targetFolder);

                // Copier tous les fichiers .dll, .pdb, .config
                var filesToCopy = Directory.GetFiles(sourcePath, "*.*")
                    .Where(f => f.EndsWith(".dll") || f.EndsWith(".pdb") || f.EndsWith(".config"));

                foreach (var file in filesToCopy)
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(targetFolder, fileName);
                    Console.WriteLine($"  Copie: {fileName}");
                    File.Copy(file, destFile, true);
                }

                Console.WriteLine();
                Console.WriteLine($"✓ {filesToCopy.Count()} fichiers installés");
            }
            finally
            {
                // Nettoyer le dossier temporaire
                if (Directory.Exists(tempPath))
                {
                    try
                    {
                        Directory.Delete(tempPath, true);
                    }
                    catch
                    {
                        // Ignorer les erreurs de nettoyage
                    }
                }
            }
        }

        static string FindSourceFolder(string extractPath, string revitVersion)
        {
            // Chercher le dossier qui contient les fichiers compilés pour la version de Revit
            // Format attendu: extractPath/2023/ ou extractPath/bin/Debug/2023/ etc.

            // 1. Vérifier directement dans extractPath
            if (ContainsAddinFiles(extractPath))
                return extractPath;

            // 2. Chercher un dossier avec le numéro de version
            string versionFolder = Path.Combine(extractPath, revitVersion);
            if (Directory.Exists(versionFolder) && ContainsAddinFiles(versionFolder))
                return versionFolder;

            // 3. Chercher récursivement (max 3 niveaux)
            var allDirs = Directory.GetDirectories(extractPath, revitVersion, SearchOption.AllDirectories);
            foreach (var dir in allDirs)
            {
                if (ContainsAddinFiles(dir))
                    return dir;
            }

            // 4. Si toujours pas trouvé, chercher le premier dossier qui contient un .addin
            var addinFiles = Directory.GetFiles(extractPath, "*.addin", SearchOption.AllDirectories);
            if (addinFiles.Length > 0)
            {
                return Path.GetDirectoryName(addinFiles[0]);
            }

            return null;
        }

        static bool ContainsAddinFiles(string path)
        {
            // Vérifier si le dossier contient les fichiers essentiels
            return File.Exists(Path.Combine(path, $"{ADDIN_NAME}.addin")) &&
                   File.Exists(Path.Combine(path, $"{ADDIN_NAME}.dll"));
        }
    }
}
