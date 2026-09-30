using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MCD2SkinStudioWpf.Models;
using MCD2SkinStudioWpf.Services;
using Microsoft.Win32;

namespace MCD2SkinStudioWpf;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private GamePatcher? _patcher;
    private Bitmap? _loadedSkin;
    private Bitmap? _convertedSkin;
    private Bitmap? _generatedIcon;
    private bool _isSlim;
    private HeroModel? _selectedHero;
    private bool _isLoaded;

    private readonly List<HeroModel> _allHeroes = [];
    public ObservableCollection<HeroModel> DisplayedHeroes { get; } = [];
    private HeroCategory _activeCategory = HeroCategory.All;

    public MainWindow()
    {
        InitializeComponent();
        LbHeroes.ItemsSource = DisplayedHeroes;
        Loaded += MainWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int dark = 0;
            DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
            int corner = 2;
            DwmSetWindowAttribute(handle, 33, ref corner, sizeof(int));
        }
        catch { }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        InitializeApplication();
    }

    private void InitializeApplication()
    {
        string? paksDir = GamePatcher.FindGamePaksDir();

        if (paksDir == null)
        {
            MessageBox.Show(
                "Minecraft Dungeons installation directory was not automatically detected in default paths (Steam / Xbox).\n\n" +
                "Please select your game folder or the Content\\Paks subfolder.",
                "Locate Game",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            paksDir = PromptUserForGameDirectory();
        }

        if (paksDir != null)
        {
            _patcher = new GamePatcher(paksDir);
            GamePatcher.SaveCustomPaksDir(paksDir);
            TxtPaksStatus.Text = paksDir;
        }
        else
        {
            _patcher = new GamePatcher(AppDomain.CurrentDomain.BaseDirectory);
            TxtPaksStatus.Text = "Not located";
            SetStatus("Folder required", "Please specify your game folder via the 'Paks Folder' button.");
        }

        LoadHeroesCatalog();
    }

    private string? PromptUserForGameDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select game folder or Dungeons\\Content\\Paks"
        };

        if (dialog.ShowDialog() == true)
        {
            string chosen = dialog.FolderName;
            if (GamePatcher.ValidatePaksDirectory(chosen, out string validPath))
            {
                return validPath;
            }

            MessageBox.Show(
                "The file Dungeons-Windows.utoc was not found in this folder or its Content\\Paks subfolders.",
                "Invalid Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        return null;
    }

    private void LoadHeroesCatalog()
    {
        _allHeroes.Clear();

        foreach (var hero in _patcher?.Catalog ?? [])
        {
            if (hero.IconSource == null)
            {
                hero.IconSource = GamePatcher.GetEmbeddedHeroIcon(hero.IconPng);
            }
            _allHeroes.Add(hero);
        }

        if (_patcher != null)
        {
            _patcher.DetectModifiedHeroes(_allHeroes);
            foreach (var h in _allHeroes)
            {
                if (h.CurrentGameSkin != null)
                {
                    h.CurrentGameSkinSource = ConvertBitmapToImageSource(h.CurrentGameSkin);

                    try
                    {
                        bool slim = h.Id.Contains("alex", StringComparison.OrdinalIgnoreCase);
                        using var render3D = Skin3DRenderer.RenderIsometricHero(h.CurrentGameSkin, isSlim: slim);
                        h.CurrentGameSkin3DSource = ConvertBitmapToImageSource(render3D);
                    }
                    catch { }
                }
            }
        }

        UpdateCategoryCounts();
        ApplyFilters();

        if (DisplayedHeroes.Count > 0)
        {
            LbHeroes.SelectedIndex = 0;
        }
    }

    private void UpdateCategoryCounts()
    {
        if (!_isLoaded || NavAllHeroes == null || NavModifiedHeroes == null || NavBaseHeroes == null || NavPrisoners == null || NavPreOrders == null)
            return;

        int allCount = _allHeroes.Count;
        int modifiedCount = _allHeroes.Count(h => h.IsModified);
        int baseCount = _allHeroes.Count(h => h.Category == HeroCategory.Base);
        int prisonerCount = _allHeroes.Count(h => h.Category == HeroCategory.Prisoner);
        int preOrderCount = _allHeroes.Count(h => h.Category == HeroCategory.PreOrder);

        NavAllHeroes.Tag = allCount.ToString();
        NavModifiedHeroes.Tag = modifiedCount.ToString();
        NavBaseHeroes.Tag = baseCount.ToString();
        NavPrisoners.Tag = prisonerCount.ToString();
        NavPreOrders.Tag = preOrderCount.ToString();
    }

    private void ApplyFilters()
    {
        if (!_isLoaded || TxtSearch == null || TxtHeroCountSubtitle == null)
            return;

        string query = TxtSearch.Text.Trim().ToLowerInvariant();

        var filtered = _allHeroes.Where(h =>
        {
            if (_activeCategory == HeroCategory.Modified && !h.IsModified)
            {
                return false;
            }

            if (_activeCategory != HeroCategory.All && _activeCategory != HeroCategory.Modified && h.Category != _activeCategory)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(query))
            {
                bool matchesName = h.Name.ToLowerInvariant().Contains(query);
                bool matchesId = h.Id.ToLowerInvariant().Contains(query);
                bool matchesBlock = h.SkinBlock.ToString().Contains(query) || h.IconBlock.ToString().Contains(query);
                return matchesName || matchesId || matchesBlock;
            }

            return true;
        }).ToList();

        DisplayedHeroes.Clear();
        foreach (var h in filtered)
        {
            DisplayedHeroes.Add(h);
        }

        TxtHeroCountSubtitle.Text = $"{DisplayedHeroes.Count} hero{(DisplayedHeroes.Count > 1 ? "es" : "")} cataloged";

        if (DisplayedHeroes.Count > 0 && (LbHeroes.SelectedItem == null || !DisplayedHeroes.Contains((HeroModel)LbHeroes.SelectedItem)))
        {
            LbHeroes.SelectedIndex = 0;
        }
    }

    private void NavCategory_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || TxtPageTitle == null)
            return;

        if (NavAllHeroes.IsChecked == true)
        {
            _activeCategory = HeroCategory.All;
            TxtPageTitle.Text = "All heroes";
        }
        else if (NavModifiedHeroes.IsChecked == true)
        {
            _activeCategory = HeroCategory.Modified;
            TxtPageTitle.Text = "Modified skins";
        }
        else if (NavBaseHeroes.IsChecked == true)
        {
            _activeCategory = HeroCategory.Base;
            TxtPageTitle.Text = "Base heroes";
        }
        else if (NavPrisoners.IsChecked == true)
        {
            _activeCategory = HeroCategory.Prisoner;
            TxtPageTitle.Text = "Prisoners";
        }
        else if (NavPreOrders.IsChecked == true)
        {
            _activeCategory = HeroCategory.PreOrder;
            TxtPageTitle.Text = "Pre-Orders";
        }

        ApplyFilters();
    }

    private void TabAll_Click(object sender, RoutedEventArgs e)
    {
        if (NavAllHeroes != null) NavAllHeroes.IsChecked = true;
    }

    private void TabBase_Click(object sender, RoutedEventArgs e)
    {
        if (NavBaseHeroes != null) NavBaseHeroes.IsChecked = true;
    }

    private void TabPrisoners_Click(object sender, RoutedEventArgs e)
    {
        if (NavPrisoners != null) NavPrisoners.IsChecked = true;
    }

    private void TabPreOrders_Click(object sender, RoutedEventArgs e)
    {
        if (NavPreOrders != null) NavPreOrders.IsChecked = true;
    }

    private void TabModified_Click(object sender, RoutedEventArgs e)
    {
        if (NavModifiedHeroes != null) NavModifiedHeroes.IsChecked = true;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded || TxtSearch == null || TxtSearchPlaceholder == null || BtnClearSearch == null)
            return;

        bool hasText = !string.IsNullOrEmpty(TxtSearch.Text);
        TxtSearchPlaceholder.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
        BtnClearSearch.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
        ApplyFilters();
    }

    private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
    {
        TxtSearch.Text = string.Empty;
        TxtSearch.Focus();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        InitializeApplication();
        SetStatus("Refreshed", "Hero catalog reloaded and game archives checked.");
    }

    private void LbHeroes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;

        if (LbHeroes.SelectedItem is HeroModel hero)
        {
            _selectedHero = hero;
            TxtSelectedHeroName.Text = hero.Name;
            TxtSelectedHeroId.Text = hero.Id;
            TxtSelectedHeroBlocks.Text = $"Skin #{hero.SkinBlock}  •  Icon #{hero.IconBlock}";

            ImgSelectedHero.Source = hero.IconSource;

            UpdateInGameSkinCard(hero);
        }
    }

    private void UpdateInGameSkinCard(HeroModel hero)
    {
        if (hero.CurrentGameSkinSource != null)
        {
            ImgCurrentGameSkin.Source = hero.CurrentGameSkinSource;
        }
        else if (_patcher != null && hero.CurrentGameSkin != null)
        {
            hero.CurrentGameSkinSource = ConvertBitmapToImageSource(hero.CurrentGameSkin);
            ImgCurrentGameSkin.Source = hero.CurrentGameSkinSource;
        }
        else
        {
            ImgCurrentGameSkin.Source = null;
        }

        if (hero.CurrentGameSkin3DSource != null)
        {
            ImgCurrentGameSkin3D.Source = hero.CurrentGameSkin3DSource;
        }
        else if (hero.CurrentGameSkin != null)
        {
            try
            {
                bool slim = hero.Id.Contains("alex", StringComparison.OrdinalIgnoreCase);
                using var render3D = Skin3DRenderer.RenderIsometricHero(hero.CurrentGameSkin, isSlim: slim);
                hero.CurrentGameSkin3DSource = ConvertBitmapToImageSource(render3D);
                ImgCurrentGameSkin3D.Source = hero.CurrentGameSkin3DSource;
            }
            catch
            {
                ImgCurrentGameSkin3D.Source = hero.IconSource;
            }
        }
        else
        {
            ImgCurrentGameSkin3D.Source = hero.IconSource;
        }

        if (hero.IsModified)
        {
            BadgeInGameStatus.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(216, 245, 219));
            BadgeInGameStatus.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(166, 227, 171));
            TxtInGameStatus.Text = "Custom";
            TxtInGameStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(27, 109, 42));
            TxtCurrentSkinDetails.Text = "Custom skin active in game archives.";
            TxtCurrentSkinActionHint.Visibility = Visibility.Visible;
            BtnRestoreVanilla.IsEnabled = true;
        }
        else
        {
            BadgeInGameStatus.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249));
            BadgeInGameStatus.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            TxtInGameStatus.Text = "Vanilla";
            TxtInGameStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105));
            TxtCurrentSkinDetails.Text = "Official original vanilla texture active.";
            TxtCurrentSkinActionHint.Visibility = Visibility.Collapsed;
            BtnRestoreVanilla.IsEnabled = false;
        }
    }

    private async void BtnRestoreVanilla_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHero == null || !_selectedHero.IsModified) return;

        if (GamePatcher.IsGameRunning())
        {
            SetStatus("Game is running", "Please exit Minecraft Dungeons II before restoring files.", isError: true);
            return;
        }

        var result = MessageBox.Show(
            $"Restore official vanilla skin for '{_selectedHero.Name}'?",
            "Restore Vanilla Skin",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        BtnRestoreVanilla.IsEnabled = false;
        BtnRestoreVanilla.Content = "Restoring...";
        SetStatus("In progress", $"Restoring {_selectedHero.Name}...");

        string heroId = _selectedHero.Id;
        string heroName = _selectedHero.Name;

        await Task.Run(() =>
        {
            try
            {
                _patcher?.RestoreVanillaSkin(heroId);

                Dispatcher.Invoke(() =>
                {
                    _selectedHero.IsModified = false;
                    var restoredBmp = _patcher?.ExtractCurrentSkin(heroId);
                    if (restoredBmp != null)
                    {
                        _selectedHero.CurrentGameSkin = restoredBmp;
                        _selectedHero.CurrentGameSkinSource = ConvertBitmapToImageSource(restoredBmp);

                        try
                        {
                            bool slim = _selectedHero.Id.Contains("alex", StringComparison.OrdinalIgnoreCase);
                            using var render3D = Skin3DRenderer.RenderIsometricHero(restoredBmp, isSlim: slim);
                            _selectedHero.CurrentGameSkin3DSource = ConvertBitmapToImageSource(render3D);
                        }
                        catch { }
                    }

                    UpdateInGameSkinCard(_selectedHero);
                    UpdateCategoryCounts();
                    ApplyFilters();

                    BtnRestoreVanilla.Content = "Restore Vanilla";
                    SetStatus("Restored", $"Original skin for {heroName} has been restored.", isSuccess: true);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    BtnRestoreVanilla.IsEnabled = true;
                    BtnRestoreVanilla.Content = "Restore Vanilla";
                    SetStatus("Restore failed", ex.Message, isError: true);
                });
            }
        });
    }

    private void BtnExportInstalledSkin_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHero?.CurrentGameSkin == null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Export currently installed skin",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"skin_{_selectedHero.Id}.png"
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedHero.CurrentGameSkin.Save(dialog.FileName, ImageFormat.Png);
            SetStatus("Exported", $"File saved: {Path.GetFileName(dialog.FileName)}", isSuccess: true);
        }
    }

    private void BtnUploadSkin_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select a Minecraft skin (.png)",
            Filter = "PNG Skins (*.png)|*.png|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadSkinFromFile(dialog.FileName);
        }
    }

    private void DropArea_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BtnUploadSkin_Click(sender, e);
    }

    private void LoadSkinFromFile(string filePath)
    {
        try
        {
            using var fileBmp = new Bitmap(filePath);
            _loadedSkin = new Bitmap(fileBmp);
            TxtSkinFileName.Text = Path.GetFileName(filePath);

            UpdateSkinConversion();

            BtnExportSkin.IsEnabled = true;
            BtnExportIcon.IsEnabled = true;
            SetStatus("Skin loaded", $"'{Path.GetFileName(filePath)}' is ready to apply.", isSuccess: true);
        }
        catch (Exception ex)
        {
            SetStatus("Load error", ex.Message, isError: true);
        }
    }

    private void ArmModel_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _loadedSkin == null) return;
        UpdateSkinConversion();
    }

    private void UpdateSkinConversion()
    {
        if (_loadedSkin == null) return;

        if (RbAuto.IsChecked == true)
        {
            _isSlim = SkinConverter.DetectSlimArm(_loadedSkin);
            TxtModelDetected.Text = _isSlim ? "Auto: Alex (3px arm)" : "Auto: Steve (4px arm)";
        }
        else if (RbSteve.IsChecked == true)
        {
            _isSlim = false;
            TxtModelDetected.Text = "Forced: Steve (4px)";
        }
        else
        {
            _isSlim = true;
            TxtModelDetected.Text = "Forced: Alex (3px)";
        }

        _convertedSkin = SkinConverter.ConvertJavaSkinToMcd(_loadedSkin, _isSlim);

        try
        {
            _generatedIcon?.Dispose();
            _generatedIcon = Skin3DRenderer.RenderIsometricHero(_loadedSkin, _isSlim);
        }
        catch
        {
            _generatedIcon = SkinConverter.GeneratePortraitVignette(_convertedSkin);
        }

        ImgOriginal.Source = ConvertBitmapToImageSource(_loadedSkin);
        ImgConverted.Source = ConvertBitmapToImageSource(_convertedSkin);
        ImgIcon.Source = ConvertBitmapToImageSource(_generatedIcon);
        ImgOriginal3D.Source = ImgIcon.Source;
    }

    private static BitmapImage ConvertBitmapToImageSource(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Seek(0, SeekOrigin.Begin);

        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.StreamSource = ms;
        bi.EndInit();
        bi.Freeze();
        return bi;
    }

    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_convertedSkin == null || _loadedSkin == null)
        {
            SetStatus("Missing skin", "Please select a Minecraft skin file first.", isError: true);
            return;
        }

        if (_selectedHero == null)
        {
            SetStatus("No hero selected", "Select a target hero from the catalog.", isError: true);
            return;
        }

        if (_patcher == null || !Directory.Exists(_patcher.PaksDir) || !File.Exists(Path.Combine(_patcher.PaksDir, "Dungeons-Windows.utoc")))
        {
            SetStatus("Folder not found", "Please indicate your Content\\Paks game folder.", isError: true);
            BtnPaksFolder_Click(sender, e);
            return;
        }

        if (GamePatcher.IsGameRunning())
        {
            SetStatus("Game is running", "Please exit Minecraft Dungeons II before injecting data.", isError: true);
            return;
        }

        BtnApply.IsEnabled = false;
        BtnApply.Content = "Applying...";
        SetStatus("In progress", $"Applying texture to {_selectedHero.Name}...");

        string heroId = _selectedHero.Id;
        string heroName = _selectedHero.Name;
        bool removeRelief = ChkRemoveRelief.IsChecked ?? true;

        await Task.Run(() =>
        {
            try
            {
                _patcher.PatchSkin(heroId, _convertedSkin);

                var iconToPatch = _generatedIcon ?? Skin3DRenderer.RenderIsometricHero(_loadedSkin, _isSlim);
                _patcher.PatchIcon(heroId, iconToPatch);

                if (removeRelief)
                {
                    _patcher.PatchMres(heroId);
                }

                Dispatcher.Invoke(() =>
                {
                    _selectedHero.IsModified = true;
                    _selectedHero.CurrentGameSkin = new Bitmap(_convertedSkin);
                    _selectedHero.CurrentGameSkinSource = ConvertBitmapToImageSource(_convertedSkin);

                    _selectedHero.CurrentGameSkin3DSource = ConvertBitmapToImageSource(iconToPatch);
                    _selectedHero.IconSource = _selectedHero.CurrentGameSkin3DSource;

                    UpdateInGameSkinCard(_selectedHero);
                    UpdateCategoryCounts();
                    ApplyFilters();

                    BtnApply.IsEnabled = true;
                    BtnApply.Content = "Apply to Game";
                    SetStatus("Success", $"Skin successfully applied to {heroName}. You can now launch Minecraft Dungeons.", isSuccess: true);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    BtnApply.IsEnabled = true;
                    BtnApply.Content = "Apply to Game";
                    SetStatus("Injection error", ex.Message, isError: true);
                });
            }
        });
    }

    private void BtnPaksFolder_Click(object sender, RoutedEventArgs e)
    {
        string? chosen = PromptUserForGameDirectory();
        if (chosen != null)
        {
            _patcher = new GamePatcher(chosen);
            GamePatcher.SaveCustomPaksDir(chosen);
            TxtPaksStatus.Text = chosen;
            LoadHeroesCatalog();
            SetStatus("Folder configured", $"Paks directory verified: {chosen}", isSuccess: true);
        }
    }

    private void BtnExportSkin_Click(object sender, RoutedEventArgs e)
    {
        if (_convertedSkin == null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export converted MCD2 skin",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"skin_{_selectedHero?.Id ?? "mcd2"}.png"
        };

        if (dialog.ShowDialog() == true)
        {
            _convertedSkin.Save(dialog.FileName, ImageFormat.Png);
            SetStatus("Exported", $"File saved: {Path.GetFileName(dialog.FileName)}", isSuccess: true);
        }
    }

    private void BtnExportIcon_Click(object sender, RoutedEventArgs e)
    {
        if (_generatedIcon == null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Export hero 3D icon",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"icon_{_selectedHero?.Id ?? "mcd2"}.png"
        };

        if (dialog.ShowDialog() == true)
        {
            _generatedIcon.Save(dialog.FileName, ImageFormat.Png);
            SetStatus("Exported", $"File saved: {Path.GetFileName(dialog.FileName)}", isSuccess: true);
        }
    }

    private void SetStatus(string title, string message, bool isError = false, bool isSuccess = false)
    {
        if (isError)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}