using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

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
                // Détecter les versions Revit disponibles dans les ressources embarquées
                var embeddedVersions = GetEmbeddedVersions();
                
                if (embeddedVersions.Count == 0)
                {
                    Console.WriteLine("ERREUR: Aucune version embarquee trouvee.");
                    Console.WriteLine("L'installateur doit etre recompile avec les ZIP embarques.");
                    Console.WriteLine();
                    Console.WriteLine("Appuyez sur une touche pour quitter...");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                Console.WriteLine("Versions Revit disponibles dans cet installateur:");
                foreach (var version in embeddedVersions)
                {
                    Console.WriteLine($"  - Revit {version}");
                }
                Console.WriteLine();

                // Détecter les versions Revit installées sur le système
                var installedVersions = DetectInstalledRevitVersions();
                
                if (installedVersions.Count == 0)
                {
                    Console.WriteLine("ATTENTION: Aucune installation Revit detectee sur ce systeme.");
                    Console.WriteLine();
                    Console.Write("Voulez-vous continuer quand meme ? (o/N): ");
                    var response = Console.ReadLine()?.ToLower();
                    if (response != "o" && response != "oui")
                    {
                        Console.WriteLine("Installation annulee.");
                        Environment.Exit(0);
                    }
                }
                else
                {
                    Console.WriteLine("Versions Revit detectees sur votre systeme:");
                    foreach (var version in installedVersions)
                    {
                        Console.WriteLine($"  - Revit {version}");
                    }
                    Console.WriteLine();
                }

                // Déterminer quelles versions installer
                var versionsToInstall = DetermineVersionsToInstall(embeddedVersions, installedVersions);
                
                if (versionsToInstall.Count == 0)
                {
                    Console.WriteLine("Aucune version a installer.");
                    Console.WriteLine();
                    Console.WriteLine("Appuyez sur une touche pour quitter...");
                    Console.ReadKey();
                    Environment.Exit(0);
                }

                Console.WriteLine("Versions qui seront installees:");
                foreach (var version in versionsToInstall)
                {
                    Console.WriteLine($"  - Revit {version}");
                }
                Console.WriteLine();

                Console.Write("Continuer l'installation ? (O/n): ");
                var confirm = Console.ReadLine()?.ToLower();
                if (confirm == "n" || confirm == "non")
                {
                    Console.WriteLine("Installation annulee.");
                    Environment.Exit(0);
                }

                Console.WriteLine();
                Console.WriteLine("===========================================");
                Console.WriteLine("Installation en cours...");
                Console.WriteLine("===========================================");
                Console.WriteLine();

                // Installer chaque version
                int successCount = 0;
                int errorCount = 0;

                foreach (var version in versionsToInstall)
                {
                    try
                    {
                        Console.WriteLine($"Installation pour Revit {version}...");
                        InstallVersion(version);
                        Console.WriteLine($"  [OK] Revit {version} installe avec succes");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  [ERREUR] Revit {version}: {ex.Message}");
                        errorCount++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("===========================================");
                Console.WriteLine("Installation terminee !");
                Console.WriteLine("===========================================");
                Console.WriteLine();
                Console.WriteLine($"Succes: {successCount} version(s)");
                if (errorCount > 0)
                {
                    Console.WriteLine($"Erreurs: {errorCount} version(s)");
                }
                Console.WriteLine();
                Console.WriteLine("Redemarrez Revit pour voir l'extension.");
                Console.WriteLine();
                Console.WriteLine("Appuyez sur une touche pour quitter...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"ERREUR FATALE: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Appuyez sur une touche pour quitter...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        static List<string> GetEmbeddedVersions()
        {
            var versions = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                // Format attendu: space_to_wall.installer.Resources.space_to_wall.app_2023.zip
                if (resourceName.Contains($"{ADDIN_NAME}_") && resourceName.EndsWith(".zip"))
                {
                    // Extraire la version (2023, 2024, etc.)
                    var parts = resourceName.Split('_');
                    var versionPart = parts.LastOrDefault()?.Replace(".zip", "");
                    
                    if (!string.IsNullOrEmpty(versionPart) && int.TryParse(versionPart, out _))
                    {
                        versions.Add(versionPart);
                    }
                }
            }

            versions.Sort();
            return versions;
        }

        static List<string> DetectInstalledRevitVersions()
        {
            var versions = new List<string>();
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string addinsBasePath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins");

            if (Directory.Exists(addinsBasePath))
            {
                var yearDirs = Directory.GetDirectories(addinsBasePath)
                    .Select(d => Path.GetFileName(d))
                    .Where(d => int.TryParse(d, out int year) && year >= 2020 && year <= 2030)
                    .OrderBy(d => d);

                versions.AddRange(yearDirs);
            }

            return versions;
        }

        static List<string> DetermineVersionsToInstall(List<string> embeddedVersions, List<string> installedVersions)
        {
            if (installedVersions.Count == 0)
            {
                // Si aucune version Revit détectée, proposer toutes les versions embarquées
                Console.WriteLine("Selection des versions a installer:");
                Console.WriteLine("  1. Installer toutes les versions disponibles");
                Console.WriteLine("  2. Choisir manuellement");
                Console.WriteLine();
                Console.Write("Choix (1-2): ");
                
                var choice = Console.ReadLine();
                
                if (choice == "1")
                {
                    return embeddedVersions;
                }
                else if (choice == "2")
                {
                    return SelectVersionsManually(embeddedVersions);
                }
                else
                {
                    return embeddedVersions; // Par défaut, tout installer
                }
            }

            // Installer uniquement les versions Revit détectées qui ont un ZIP embarqué
            var versionsToInstall = embeddedVersions.Intersect(installedVersions).ToList();
            
            if (versionsToInstall.Count < embeddedVersions.Count)
            {
                var missingVersions = embeddedVersions.Except(versionsToInstall).ToList();
                if (missingVersions.Count > 0)
                {
                    Console.WriteLine($"Note: Versions disponibles mais non detectees: {string.Join(", ", missingVersions)}");
                    Console.Write("Voulez-vous les installer quand meme ? (o/N): ");
                    var response = Console.ReadLine()?.ToLower();
                    if (response == "o" || response == "oui")
                    {
                        versionsToInstall.AddRange(missingVersions);
                    }
                    Console.WriteLine();
                }
            }

            return versionsToInstall;
        }

        static List<string> SelectVersionsManually(List<string> availableVersions)
        {
            var selected = new List<string>();
            Console.WriteLine();
            
            foreach (var version in availableVersions)
            {
                Console.Write($"Installer Revit {version} ? (O/n): ");
                var response = Console.ReadLine()?.ToLower();
                if (response != "n" && response != "non")
                {
                    selected.Add(version);
                }
            }
            
            Console.WriteLine();
            return selected;
        }

        static void InstallVersion(string version)
        {
            // Extraire le ZIP depuis les ressources embarquées
            string tempZipPath = ExtractEmbeddedZip(version);

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                
                // Dossier pour le fichier .addin (dans Revit\Addins\{Version})
                string addinsPath = Path.Combine(appDataPath, "Autodesk", "Revit", "Addins", version);
                
                // Dossier pour les DLL (dans ApplicationPlugins\SpaceToWall\{Version})
                string pluginsPath = Path.Combine(appDataPath, "Autodesk", "ApplicationPlugins", "SpaceToWall", version);

                // Créer les dossiers s'ils n'existent pas
                if (!Directory.Exists(addinsPath))
                {
                    Directory.CreateDirectory(addinsPath);
                }
                
                if (!Directory.Exists(pluginsPath))
                {
                    Directory.CreateDirectory(pluginsPath);
                }

                // Créer un dossier temporaire pour extraire
                string tempExtractPath = Path.Combine(Path.GetTempPath(), $"{ADDIN_NAME}_extract_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempExtractPath);

                try
                {
                    // Extraire le ZIP
                    ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

                    // 1. Copier les DLL et dépendances dans ApplicationPlugins
                    if (Directory.Exists(pluginsPath))
                    {
                        // Nettoyer l'ancienne version
                        Directory.Delete(pluginsPath, true);
                        Directory.CreateDirectory(pluginsPath);
                    }

                    var filesToCopy = Directory.GetFiles(tempExtractPath)
                        .Where(f => f.EndsWith(".dll") || f.EndsWith(".pdb") || f.EndsWith(".config"));

                    foreach (var file in filesToCopy)
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(pluginsPath, fileName);
                        File.Copy(file, destFile, true);
                    }

                    // 2. Copier et modifier le fichier .addin
                    string addinTemplatePath = GetEmbeddedAddinTemplate();
                    if (File.Exists(addinTemplatePath))
                    {
                        // Lire le contenu du template
                        string addinContent = File.ReadAllText(addinTemplatePath);
                        
                        // Remplacer {VERSIONS} par la version réelle
                        addinContent = addinContent.Replace("{VERSIONS}", version);
                        
                        // Écrire dans le dossier Addins
                        string destAddinPath = Path.Combine(addinsPath, "SpaceToWall.addin");
                        File.WriteAllText(destAddinPath, addinContent);
                        
                        // Nettoyer le template temporaire
                        try
                        {
                            File.Delete(addinTemplatePath);
                        }
                        catch { }
                    }
                    else
                    {
                        throw new Exception("Fichier .addin template introuvable dans les ressources embarquées");
                    }
                }
                finally
                {
                    // Nettoyer le dossier temporaire d'extraction
                    if (Directory.Exists(tempExtractPath))
                    {
                        try
                        {
                            Directory.Delete(tempExtractPath, true);
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                // Nettoyer le ZIP temporaire
                if (File.Exists(tempZipPath))
                {
                    try
                    {
                        File.Delete(tempZipPath);
                    }
                    catch { }
                }
            }
        }

        static string ExtractEmbeddedZip(string version)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.Contains($"{ADDIN_NAME}_{version}.zip"));

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new Exception($"ZIP pour la version {version} introuvable dans les ressources embarquees");
            }

            string tempZipPath = Path.Combine(Path.GetTempPath(), $"{ADDIN_NAME}_{version}_{Guid.NewGuid()}.zip");

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (FileStream fileStream = File.Create(tempZipPath))
            {
                if (resourceStream == null)
                {
                    throw new Exception($"Impossible de lire la ressource {resourceName}");
                }
                resourceStream.CopyTo(fileStream);
            }

            return tempZipPath;
        }

        static string GetEmbeddedAddinTemplate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.Contains("SpaceToWall.addin"));

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new Exception("Fichier SpaceToWall.addin introuvable dans les ressources embarquees");
            }

            string tempAddinPath = Path.Combine(Path.GetTempPath(), $"SpaceToWall_{Guid.NewGuid()}.addin");

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (FileStream fileStream = File.Create(tempAddinPath))
            {
                if (resourceStream == null)
                {
                    throw new Exception($"Impossible de lire la ressource {resourceName}");
                }
                resourceStream.CopyTo(fileStream);
            }

            return tempAddinPath;
        }
    }
}
