using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace MCD2SkinStudioWpf.Models;

public enum HeroCategory
{
    All,
    Modified,
    Base,
    Prisoner,
    PreOrder
}

public class HeroModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("skin_asset")]
    public string SkinAsset { get; set; } = string.Empty;

    [JsonPropertyName("skin_block")]
    public int SkinBlock { get; set; }

    [JsonPropertyName("skin_ucas_off")]
    public long SkinUcasOff { get; set; }

    [JsonPropertyName("skin_len")]
    public int SkinLen { get; set; }

    [JsonPropertyName("icon_asset")]
    public string IconAsset { get; set; } = string.Empty;

    [JsonPropertyName("icon_block")]
    public int IconBlock { get; set; }

    [JsonPropertyName("icon_ucas_off")]
    public long IconUcasOff { get; set; }

    [JsonPropertyName("icon_len")]
    public int IconLen { get; set; }

    [JsonPropertyName("icon_png")]
    public string IconPng { get; set; } = string.Empty;

    [JsonPropertyName("mres_asset")]
    public string MresAsset { get; set; } = string.Empty;

    [JsonPropertyName("mres_block")]
    public int MresBlock { get; set; }

    [JsonPropertyName("mres_ucas_off")]
    public long MresUcasOff { get; set; }

    [JsonPropertyName("mres_max_space")]
    public int MresMaxSpace { get; set; }

    [JsonPropertyName("mres_len")]
    public int MresLen { get; set; }

    [JsonIgnore]
    public string LocalIconPath { get; set; } = string.Empty;

    private ImageSource? _iconSource;
    [JsonIgnore]
    public ImageSource? IconSource
    {
        get => _iconSource;
        set
        {
            _iconSource = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public HeroCategory Category
    {
        get
        {
            if (Id.Contains("preorder", StringComparison.OrdinalIgnoreCase)) return HeroCategory.PreOrder;
            if (Name.Contains("Prisoner", StringComparison.OrdinalIgnoreCase)) return HeroCategory.Prisoner;
            return HeroCategory.Base;
        }
    }

    [JsonIgnore]
    public string BadgeText => Category switch
    {
        HeroCategory.PreOrder => "Pre-Order",
        HeroCategory.Prisoner => "Prisoner",
        _ => "Base"
    };

    [JsonIgnore]
    public string BadgeBackground => Category switch
    {
        HeroCategory.PreOrder => "#E8DAF7",
        HeroCategory.Prisoner => "#FFE7D1",
        _ => "#E1EBF7"
    };

    [JsonIgnore]
    public string BadgeForeground => Category switch
    {
        HeroCategory.PreOrder => "#6B35A8",
        HeroCategory.Prisoner => "#A85300",
        _ => "#005FB8"
    };

    [JsonIgnore]
    public string BadgeBorder => Category switch
    {
        HeroCategory.PreOrder => "#D3BCEB",
        HeroCategory.Prisoner => "#F5CA9E",
        _ => "#BFD6ED"
    };

    [JsonIgnore]
    public string FormattedDetails => $"Skin #{SkinBlock}  •  Icon #{IconBlock}";

    private bool _isModified;
    [JsonIgnore]
    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (_isModified != value)
            {
                _isModified = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModifiedBadgeText));
                OnPropertyChanged(nameof(ModifiedBadgeBackground));
                OnPropertyChanged(nameof(ModifiedBadgeForeground));
                OnPropertyChanged(nameof(ModifiedBadgeBorder));
            }
        }
    }

    [JsonIgnore]
    public string ModifiedBadgeText => IsModified ? "Custom" : "Vanilla";

    [JsonIgnore]
    public string ModifiedBadgeBackground => IsModified ? "#D8F5DB" : "#F0F0F0";

    [JsonIgnore]
    public string ModifiedBadgeForeground => IsModified ? "#1B6D2A" : "#666666";

    [JsonIgnore]
    public string ModifiedBadgeBorder => IsModified ? "#A6E3AB" : "#D0D0D0";

    [JsonIgnore]
    public Bitmap? CurrentGameSkin { get; set; }

    private ImageSource? _currentGameSkinSource;
    [JsonIgnore]
    public ImageSource? CurrentGameSkinSource
    {
        get => _currentGameSkinSource;
        set
        {
            _currentGameSkinSource = value;
            OnPropertyChanged();
        }
    }

    private ImageSource? _currentGameSkin3DSource;
    [JsonIgnore]
    public ImageSource? CurrentGameSkin3DSource
    {
        get => _currentGameSkin3DSource;
        set
        {
            _currentGameSkin3DSource = value;
            OnPropertyChanged();
        }
    }

    private string _customPlayerName = string.Empty;
    [JsonIgnore]
    public string CustomPlayerName
    {
        get => _customPlayerName;
        set
        {
            _customPlayerName = value;
            OnPropertyChanged();
        }
    }
}
