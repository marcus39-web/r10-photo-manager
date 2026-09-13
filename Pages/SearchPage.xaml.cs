using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using R10CSharp.Models;
using Microsoft.VisualBasic;
using R10CSharp.Services;

namespace R10CSharp.Pages
{
    public partial class SearchPage : Page
    {
        private readonly List<PhotoIndexEntry> _index = new List<PhotoIndexEntry>();
        private readonly SearchEngine _engine;
        private string _currentFolderRelativePath = string.Empty;

        public SearchPage()
        {
            InitializeComponent();
            var settings = SettingsService.Load();
            var indexPath = settings?.IndexPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "index.json");

            if (File.Exists(indexPath))
            {
                _index = IndexBuilder.LoadIndex(indexPath);
            }
            else
            {
                var root = settings?.ArchivePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Canon_R10_Bilder");
                _index = IndexBuilder.BuildIndex(root);
                IndexBuilder.SaveIndex(_index, indexPath);
            }

            // Nur Einträge aus dem Canon_R10_Bilder-Hauptordner berücksichtigen (falls vorhanden)
            try
            {
                var mainFolder = "Canon_R10_Bilder";
                if (_index != null)
                {
                    _index = _index.Where(i => i.FilePath != null && i.FilePath.Contains(mainFolder, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            catch { }

            // Build and show folder tree for deeper navigation will be invoked after folders are populated

            _engine = new SearchEngine(_index ?? new List<PhotoIndexEntry>());

            // Ergebnisliste initial befüllen (neueste zuerst)
            ResultList.ItemsSource = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).OrderByDescending(i => i.Timestamp).ToList();

            // Filter-Comboboxen aus dem Index füllen
            var categories = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).Select(i => i.Category).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            CategoryFilter.ItemsSource = categories.Count != 0 ? categories : new List<string> { "(keine)" };

            var rawOrJpg = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).Select(i => i.RawOrJpg).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            if (rawOrJpg.Count == 0) rawOrJpg = new List<string> { "JPG", "RAW" };
            RawJpgFilter.ItemsSource = rawOrJpg;

            var series = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).Select(i => i.Series).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            SeriesFilter.ItemsSource = series.Count != 0 ? series : new List<string> { "(keine)" };

            // Ordner-Auswahl: direkte Unterordner-Namen unter Canon_R10_Bilder (z.B. 01_Bibioothek_JPG)
            var folders = (_index ?? Enumerable.Empty<PhotoIndexEntry>())
                .Select(i => System.IO.Path.GetDirectoryName(i.FilePath) ?? string.Empty)
                .Select(d =>
                {
                    var parts = d.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    for (int p = 0; p < parts.Length; p++)
                    {
                        if (string.Equals(parts[p], "Canon_R10_Bilder", StringComparison.OrdinalIgnoreCase) && p + 1 < parts.Length)
                        {
                            return parts[p + 1];
                        }
                    }
                    return string.Empty;
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            FolderFilter.ItemsSource = folders.Count != 0 ? folders : new List<string> { "(keiner)" };
            if (folders.Count != 0) 
            {
                FolderFilter.SelectedIndex = 0;
                // Build tree for initially selected folder
                BuildFolderTree(FolderFilter.SelectedItem as string);
            }

            // Favoriten (Tags) als Platzhalter
            var favs = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).Select(i => i.Tags).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            FavoritesFilter.ItemsSource = favs.Count != 0 ? favs : new List<string> { "(keine)" };
        }

        private static readonly char[] separator = new[] { '\\' };

        // Overload: erlaubt das Starten der Seite mit einer Suchanfrage
        public SearchPage(string query) : this()
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                PerformSearch(query);
            }
        }
        private void BuildFolderTree(string library)
        {
            FolderTree.Items.Clear();
            if (string.IsNullOrWhiteSpace(library) || _index == null) return;

            var marker = "\\Canon_R10_Bilder\\" + library + "\\";
            var relSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in _index)
            {
                if (string.IsNullOrWhiteSpace(item.FilePath)) continue;
                var fp = item.FilePath.Replace('/', '\\');
                var idx = fp.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var after = fp.Substring(idx + marker.Length);
                var dir = System.IO.Path.GetDirectoryName(after) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(dir)) relSet.Add(dir);
            }

            // Build hierarchical nodes
            var rootNodes = new Dictionary<string, TreeViewItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var rel in relSet.OrderBy(s => s))
            {
                var parts = rel.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var currentDict = rootNodes;
                ItemsControlCollectionEnsure();
                // walk/create nodes
                TreeViewItem parent = null;
                string accum = string.Empty;
                for (int i = 0; i < parts.Length; i++)
                {
                    accum = (accum == string.Empty) ? parts[i] : accum + "/" + parts[i];
                    // find or create node under parent
                    TreeViewItem node = null;
                    if (parent == null)
                    {
                        // top-level
                        node = FolderTree.Items.OfType<TreeViewItem>().FirstOrDefault(t => string.Equals((string)t.Tag, parts[i], StringComparison.OrdinalIgnoreCase));
                        if (node == null)
                        {
                            node = new TreeViewItem { Header = parts[i], Tag = parts[i] };
                            FolderTree.Items.Add(node);
                        }
                    }
                    else
                    {
                        node = parent.Items.OfType<TreeViewItem>().FirstOrDefault(t => string.Equals((string)t.Tag, accum, StringComparison.OrdinalIgnoreCase));
                        if (node == null)
                        {
                            node = new TreeViewItem { Header = parts[i], Tag = accum };
                            parent.Items.Add(node);
                        }
                    }

                    parent = node;
                }
            }
        }

        // Helper to satisfy analyzer (no-op)
        private void ItemsControlCollectionEnsure() { }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not TreeViewItem tvi) return;
            var lib = FolderFilter.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(lib)) return;

            var rel = tvi.Tag as string ?? string.Empty; // e.g. "03_Passbilder/Marcus"
            _currentFolderRelativePath = rel;

            // Filter results to files under selected library + rel
            var marker = "\\Canon_R10_Bilder\\" + lib + "\\" + rel.Replace('/', '\\') + "\\";
            var results = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).Where(i => i.FilePath != null && i.FilePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(i => i.Timestamp).ToList();
            ResultList.ItemsSource = results;
        }

        private void FolderBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFolderRelativePath))
            {
                // nothing selected or already at root -> clear selection
                ClearTreeSelection();
                ResultList.ItemsSource = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).OrderByDescending(i => i.Timestamp).ToList();
                _currentFolderRelativePath = string.Empty;
                return;
            }

            var parts = _currentFolderRelativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
            {
                // move to library root
                ClearTreeSelection();
                _currentFolderRelativePath = string.Empty;
                FolderFilter_SelectionChanged(FolderFilter, null);
                return;
            }

            var parentRel = string.Join("/", parts.Take(parts.Length - 1));
            // find TreeViewItem by Tag
            var target = FindTreeViewItemByTag(parentRel);
            if (target != null)
            {
                target.IsSelected = true;
            }
            else
            {
                // fallback: set to root
                ClearTreeSelection();
                _currentFolderRelativePath = string.Empty;
                FolderFilter_SelectionChanged(FolderFilter, null);
            }
        }

        private void ClearTreeSelection()
        {
            foreach (var top in FolderTree.Items.OfType<TreeViewItem>())
            {
                ClearSelectionRecursive(top);
            }
        }

        private void ClearSelectionRecursive(TreeViewItem node)
        {
            if (node.IsSelected) node.IsSelected = false;
            foreach (var child in node.Items.OfType<TreeViewItem>()) ClearSelectionRecursive(child);
        }

        private TreeViewItem FindTreeViewItemByTag(string tag)
        {
            foreach (var top in FolderTree.Items.OfType<TreeViewItem>())
            {
                var found = FindInTree(top, tag);
                if (found != null) return found;
            }
            return null;
        }

        private TreeViewItem FindInTree(TreeViewItem node, string tag)
        {
            if ((node.Tag as string) == tag) return node;
            foreach (var child in node.Items.OfType<TreeViewItem>())
            {
                var found = FindInTree(child, tag);
                if (found != null) return found;
            }
            return null;
        }

        private void PerformSearch(string query)
        {
            try
            {
                var category = CategoryFilter.SelectedItem?.ToString();
                var rawOrJpg = RawJpgFilter.SelectedItem?.ToString();
                var series = SeriesFilter.SelectedItem?.ToString();
                var favorites = FavoritesFilter.SelectedItem?.ToString();

                var results = _engine.Search(query, category, rawOrJpg, series, favorites).ToList();

                // Apply date range if set
                if (DateFromPicker.SelectedDate.HasValue || DateToPicker.SelectedDate.HasValue)
                {
                    var from = DateFromPicker.SelectedDate ?? DateTime.MinValue;
                    var to = DateToPicker.SelectedDate ?? DateTime.MaxValue;
                    results = results.Where(r => r.Timestamp >= from && r.Timestamp <= to).ToList();
                }

                // Apply folder filter if set
                var folder = FolderFilter.SelectedItem?.ToString();
                if (!string.IsNullOrWhiteSpace(folder) && folder != "(keiner)")
                {
                    results = results.Where(i => i.FilePath != null && i.FilePath.Contains(folder, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                ResultList.ItemsSource = results.OrderByDescending(i => i.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                try { App.Log($"Search error: {ex}"); } catch { }
                MessageBox.Show("Beim Ausführen der Suche ist ein Fehler aufgetreten. Details wurden ins Log geschrieben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Wenn Raw/JPG geändert wurde und ein Ordner ausgewählt ist, dann
            // soll die 2. Dropdown-Liste die Unterordner (Kategorien) dieses Bibliotheksordners anzeigen.
            try
            {
                var selectedFolder = FolderFilter.SelectedItem as string;
                var selectedRaw = RawJpgFilter.SelectedItem as string;

                if (!string.IsNullOrWhiteSpace(selectedFolder) && _index != null)
                {
                    // Sammle die nächsten Unterordner direkt unter dem ausgewählten Bibliotheksordner
                    // Sammle alle Unterordner-Pfade unterhalb des ausgewählten Bibliotheksordners.
                    // Dabei werden auch tiefere Ebenen als "Pfad/Unterpfad" angeboten, damit der Benutzer
                    // die gesamte Tree-Struktur als Auswahl bekommt.
                    var relSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in _index)
                    {
                        if (string.IsNullOrWhiteSpace(item.FilePath)) continue;
                        // Wenn Raw/JPG ausgewählt ist, nur passende Einträge berücksichtigen; sonst alle
                        if (!string.IsNullOrWhiteSpace(selectedRaw) && !string.Equals(item.RawOrJpg, selectedRaw, StringComparison.OrdinalIgnoreCase)) continue;

                        var dir = System.IO.Path.GetDirectoryName(item.FilePath) ?? string.Empty;
                        var parts = dir.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        for (int p = 0; p < parts.Length; p++)
                        {
                            if (string.Equals(parts[p], "Canon_R10_Bilder", StringComparison.OrdinalIgnoreCase) && p + 1 < parts.Length)
                            {
                                if (string.Equals(parts[p + 1], selectedFolder, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Baue alle Präfix-Pfade der Unterordner ab parts[p+2]
                                    if (p + 2 < parts.Length)
                                    {
                                        for (int end = p + 2; end < parts.Length; end++)
                                        {
                                            var rel = string.Join("/", parts.Skip(p + 2).Take(end - (p + 2) + 1));
                                            if (!string.IsNullOrWhiteSpace(rel)) relSet.Add(rel);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    var subfolders = relSet.OrderBy(s => s).ToList();

                    if (subfolders.Count != 0)
                    {
                        CategoryFilter.ItemsSource = subfolders;
                        CategoryFilter.IsEnabled = true;
                        CategoryFilter.SelectedIndex = -1;
                    }
                    else
                    {
                        // Falls keine Unterordner vorhanden sind, versuche Kategorien aus Index-Einträgen zu verwenden
                        var categories = _index.Where(i => i.FilePath != null && i.FilePath.Contains(selectedFolder, StringComparison.OrdinalIgnoreCase) && string.Equals(i.RawOrJpg, selectedRaw, StringComparison.OrdinalIgnoreCase))
                                               .Select(i => i.Category)
                                               .Where(s => !string.IsNullOrWhiteSpace(s))
                                               .Distinct()
                                               .OrderBy(s => s)
                                               .ToList();

                        if (categories.Count != 0)
                        {
                            CategoryFilter.ItemsSource = categories;
                            CategoryFilter.IsEnabled = true;
                            CategoryFilter.SelectedIndex = -1;
                        }
                        else
                        {
                            CategoryFilter.ItemsSource = new List<string> { "(keine)" };
                            CategoryFilter.IsEnabled = false;
                            CategoryFilter.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch { }

            // Re-run search with current top-search text (if any)
            string query = string.Empty;
            try { if (Application.Current?.MainWindow is MainWindow mw) query = mw.TopSearchBox?.Text ?? string.Empty; } catch { }
            PerformSearch(query);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = string.Empty;
                try
                {
                    if (Application.Current?.MainWindow is MainWindow mw)
                    {
                        query = mw.TopSearchBox?.Text ?? string.Empty;
                    }
                }
                catch { }

                PerformSearch(query);
            }
            catch (Exception ex)
            {
            try
            {
                App.Log($"Search error: {ex}");
            }
            catch { }
            MessageBox.Show("Beim Ausführen der Suche ist ein Fehler aufgetreten. Details wurden ins Log geschrieben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultList.SelectedItem is PhotoIndexEntry entry)
            {
                try
                {
                    // Lade das Bild synchron und ignoriere den Image-Cache, damit bei schneller
                    // Auswahl in der Liste immer das korrekte Bild angezeigt wird.
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.UriSource = new Uri(entry.FilePath);
                    bmp.EndInit();
                    bmp.Freeze();
                    PreviewImage.Source = bmp;
                }
                catch
                {
                    // Falls Laden fehlschlägt, Bild zurücksetzen
                    PreviewImage.Source = null;
                }

                PreviewFileName.Text = entry.FileName;
                PreviewCategory.Text = entry.Category;
                PreviewPath.Text = entry.FilePath;
            }
        }

        // single Filter_Changed handler defined below

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResultList.SelectedItem is not PhotoIndexEntry entry) return;

            var input = Interaction.InputBox("Neuer Dateiname (ohne Pfad, inkl. Endung):", "Datei umbenennen", entry.FileName);
            if (string.IsNullOrWhiteSpace(input) || input == entry.FileName) return;

            try
            {
                var dir = Path.GetDirectoryName(entry.FilePath) ?? string.Empty;
                var newPath = Path.Combine(dir, input);
                if (File.Exists(newPath))
                {
                    MessageBox.Show("Zieldatei existiert bereits.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                File.Move(entry.FilePath, newPath);
                var thumb = IndexBuilder.RegenerateThumbnail(newPath);
                entry.FileName = Path.GetFileName(newPath);
                entry.FilePath = newPath;
                entry.Thumbnail = thumb ?? entry.Thumbnail;
                ResultList.Items.Refresh();
                PreviewFileName.Text = entry.FileName;
                PreviewPath.Text = entry.FilePath;

                MessageBox.Show("Datei umbenannt.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Umbenennen: " + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not MenuItem mi) return;
                if (mi.Parent is not ContextMenu cm) return;
                if (cm.PlacementTarget is not FrameworkElement fe) return;
                if (fe.DataContext is not PhotoIndexEntry entry) return;

                var input = Interaction.InputBox("Neuer Dateiname (ohne Pfad, inkl. Endung):", "Datei umbenennen", entry.FileName);
                if (string.IsNullOrWhiteSpace(input) || input == entry.FileName) return;

                var dir = Path.GetDirectoryName(entry.FilePath) ?? string.Empty;
                var newPath = Path.Combine(dir, input);
                if (File.Exists(newPath))
                {
                    MessageBox.Show("Zieldatei existiert bereits.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                File.Move(entry.FilePath, newPath);

                var thumb = IndexBuilder.RegenerateThumbnail(newPath);
                entry.FileName = Path.GetFileName(newPath);
                entry.FilePath = newPath;
                entry.Thumbnail = thumb ?? entry.Thumbnail;

                ResultList.Items.Refresh();
                PreviewFileName.Text = entry.FileName;
                PreviewPath.Text = entry.FilePath;

                MessageBox.Show("Datei umbenannt.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Umbenennen: " + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FolderFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var folder = FolderFilter.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(folder) || folder == "(keiner)")
            {
                ResultList.ItemsSource = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).OrderByDescending(i => i.Timestamp).ToList();
                return;
            }
            var filtered = (_index ?? Enumerable.Empty<PhotoIndexEntry>()).Where(i => i.FilePath != null && i.FilePath.Contains(folder, StringComparison.OrdinalIgnoreCase)).ToList();

            // 1) Kategorie-Dropdown mit direkten Unterordnern der gewählten Bibliothek füllen
            try
            {
                var relSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var marker = "\\Canon_R10_Bilder\\" + folder + "\\";
                foreach (var item in filtered)
                {
                    var fp = item.FilePath.Replace('/', '\\');
                    var idx = fp.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) continue;
                    var after = fp.Substring(idx + marker.Length);
                    if (string.IsNullOrWhiteSpace(after)) continue;
                    var next = after.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(next)) relSet.Add(next);
                }

                var childFolders = relSet.OrderBy(s => s).ToList();
                if (childFolders.Count != 0)
                {
                    CategoryFilter.ItemsSource = childFolders;
                    CategoryFilter.IsEnabled = true;
                    CategoryFilter.SelectedIndex = -1;
                }
                else
                {
                    // Fallback: benutze ParentFolder oder Category-Feld
                    var fallback = filtered.Select(i => i.ParentFolder).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
                    if (fallback.Count != 0)
                    {
                        CategoryFilter.ItemsSource = fallback;
                        CategoryFilter.IsEnabled = true;
                        CategoryFilter.SelectedIndex = -1;
                    }
                    else
                    {
                        CategoryFilter.ItemsSource = new List<string> { "(keine)" };
                        CategoryFilter.IsEnabled = false;
                        CategoryFilter.SelectedIndex = 0;
                    }
                }
            }
            catch { }

            // 2) Raw/JPG-Filter aktualisieren basierend auf Einträgen in diesem Ordner
            try
            {
                var rawOrJpg = filtered.Select(i => i.RawOrJpg).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
                if (rawOrJpg.Count != 0)
                {
                    RawJpgFilter.ItemsSource = rawOrJpg;
                    RawJpgFilter.IsEnabled = true;
                    RawJpgFilter.SelectedIndex = -1;
                }
                else
                {
                    RawJpgFilter.ItemsSource = new List<string> { "(keine)" };
                    RawJpgFilter.IsEnabled = false;
                    RawJpgFilter.SelectedIndex = 0;
                }
            }
            catch { }

            // 3) Ergebnisliste aktualisieren (neueste zuerst)
            ResultList.ItemsSource = filtered.OrderByDescending(i => i.Timestamp).ToList();

            // 4) Re-apply search with top-search text
            string query = string.Empty;
            try { if (Application.Current?.MainWindow is MainWindow mw) query = mw.TopSearchBox?.Text ?? string.Empty; } catch { }
            PerformSearch(query);
        }
    }
}
