using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MCD2SkinStudioWpf.Models;
using Microsoft.Win32;

namespace MCD2SkinStudioWpf.Services;

internal static class OodleNative
{
    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int OodleLZ_Decompress(
        byte[] compBuf, long compBufSize,
        byte[] rawBuf, long rawLen,
        int fuzzSafe, int checkCRC, int verbosity,
        IntPtr decBufBase, long decBufSize,
        IntPtr fpCallback, IntPtr callbackUserData,
        IntPtr decoderMemory, long decoderMemorySize, int threadPhase);

    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern long OodleLZ_Compress(
        int codec, byte[] rawBuf, long rawLen,
        byte[] compBuf, int level,
        IntPtr pOptions, IntPtr dictionaryBase, IntPtr lrm,
        IntPtr scratchMem, long scratchSize);
}

public class GamePatcher
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    static GamePatcher()
    {
        EnsureNativeDependencies();
    }

    public static void EnsureNativeDependencies()
    {
        try
        {
            string localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MCD2SkinStudio");
            Directory.CreateDirectory(localAppData);

            string targetDll = Path.Combine(localAppData, "oo2core_9_win64.dll");
            if (!File.Exists(targetDll))
            {
                string baseDll = Path.Combine(AppContext.BaseDirectory, "oo2core_9_win64.dll");
                if (File.Exists(baseDll))
                {
                    File.Copy(baseDll, targetDll, true);
                }
                else
                {
                    string nativeDirDll = @"C:\Users\MeowMeow\Desktop\MCD2_Skin_Studio_Native\oo2core_9_win64.dll";
                    if (File.Exists(nativeDirDll))
                    {
                        File.Copy(nativeDirDll, targetDll, true);
                    }
                    else
                    {
                        using var stream = typeof(GamePatcher).Assembly.GetManifestResourceStream("MCD2SkinStudioWpf.oo2core_9_win64.dll");
                        if (stream != null)
                        {
                            using var fs = File.Create(targetDll);
                            stream.CopyTo(fs);
                        }
                    }
                }
            }

            if (File.Exists(targetDll))
            {
                SetDllDirectory(localAppData);
            }
            SetDllDirectory(AppContext.BaseDirectory);
        }
        catch { }
    }

    private static readonly byte[] AesKey = [
        0x1F, 0x81, 0x80, 0x33, 0x53, 0x09, 0x55, 0x34,
        0xB0, 0x54, 0x4C, 0x9E, 0x26, 0x08, 0x8A, 0x3D,
        0x4D, 0xB0, 0x69, 0xC9, 0x72, 0x7D, 0x38, 0xDE,
        0x64, 0x9C, 0xB1, 0x8E, 0x4F, 0xD4, 0xA3, 0xEC
    ];

    private const int UtocTableOffset = 1479504;
    private const int UtocEntrySize = 12;

    private static readonly Dictionary<string, string> VanillaSkinHashes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["alex_preorder"] = "f3f77eeec6ae7ac4abc13e8ba204e1209ab9c11f87d0657f50d9a601549a55e0",
        ["steve_preorder"] = "e31b4f9bbc19f43e2238c9ef036a68d5fc3b7861d557fd3d5fec8fb982115d70",
        ["valorie"] = "41ecb7d7664881a18330f5c43cf31a8f54050ef199ad427a6daf2025fbdd38d5",
        ["greta"] = "0cc8837dfba9e5863e27dadda58c1a33b852ca9e121b4e4024a5d02959b53049",
        ["nuru"] = "e0ea8199fb80000146ed5624f9177f0b0804ff121fd4e7ae1684eb2bbdba8604",
        ["qamar"] = "5dad5da5621554cec12657c910dc5d655697c47f49d478b055994bd6a6072afa",
        ["ranger"] = "55690a70a47a2bb5a78da4d3758de53c15ae567f9e17a33bb9fd45b645437ecf",
        ["tank"] = "a3a238605ca9973370113eb9aef35dbcd2be9818dafce3a46156a2497d0e0d13",
        ["violet"] = "58071084e7b0a75e8621dbe5449397e0e02de4138652f998058f2c222a9cd1f9",
        ["darian"] = "40b4175d97d69fa18222b675993c4dbfaa6fb2e6aa1bcb539cda38d9a75829fa",
        ["eshe"] = "41ea5d77dc63d0ed85fce45566fca3674cb55d2489ff5b7cc537efc95381fde0",
        ["esperanza"] = "a85e62dbd5e853023cf0e5ed07f90ed72d331224cf45f2c8cdd087e294e0e7a9",
        ["healer"] = "a6c7fb9a35e7865c74ebdf98f3e5e327f1b6f09127ea3f198b9e966a36d3ace9",
        ["javier"] = "7f73c8b9e11eec4bc23068a92069a6af87c68daed85d5c4ee8b27bae0b40f2f5",
        ["pizzachef"] = "24a0169041eb9799af326dd132116184f9a2ebed6596e465442abf9cc0ff7325",
        ["brown"] = "2f7cb07092aa1c52ece6a782d022bc7189cf50e3c7f0cbfc539d54c92279cae1",
        ["gray"] = "fa201ae843f325e24304cba3ec1a069f1a3b53b8d344487ecad796255a0c39ec",
        ["green"] = "ac9c1651f94e6995686efe9eec5d825d592d7e4d825dbad9c0db76a92f84d064",
        ["mint"] = "52026612a3f936a0a5ed3b23cd6ca075e1ae4ac737ff11cd7deaf025f0fe3269",
        ["pink"] = "28c37d1cb10e9d3338b4b7d0ddc0e8f7fb8b5acbd05bb750e6578cffc22a63a1",
        ["plum"] = "9f95944af7640efb6a73b6bedd680c1dcc7a78d698d53f239985a6476aa4fdae",
        ["purple"] = "547244dc8248a00680f7e2052f98d7714af6da15dae20f203d643b0598e8af23",
        ["silver"] = "eff1869ca83f03cd44970790aebecf263e91664e75a5868131cb2cc8bc372bb4",
        ["yellow"] = "dc7cb7cea18f8e7ebfb59599f2d513a234dbcd048f0b4bdc5fac9f618d48b136"
    };

    public string PaksDir { get; set; }
    private readonly string _utocPath;
    private readonly string _ucasPath;

    public List<HeroModel> Catalog { get; private set; } = [];

    public GamePatcher(string paksDir)
    {
        PaksDir = paksDir;
        _utocPath = Path.Combine(paksDir, "Dungeons-Windows.utoc");
        _ucasPath = Path.Combine(paksDir, "Dungeons-Windows.ucas");
        LoadCatalog();
    }

    public static bool IsGameRunning()
    {
        try
        {
            var p1 = Process.GetProcessesByName("Dungeons");
            var p2 = Process.GetProcessesByName("Dungeons-Win64-Shipping");
            return p1.Length > 0 || p2.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static string GetSettingsFilePath()
    {
        string baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "settings.json");
    }

    public static void SaveCustomPaksDir(string path)
    {
        try
        {
            var dict = new Dictionary<string, string> { ["paks_dir"] = path };
            File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(dict));
        }
        catch { }
    }

    public static string? LoadSavedPaksDir()
    {
        try
        {
            string file = GetSettingsFilePath();
            if (File.Exists(file))
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
                if (dict != null && dict.TryGetValue("paks_dir", out string? p) && !string.IsNullOrEmpty(p))
                {
                    if (ValidatePaksDirectory(p, out string valid))
                        return valid;
                }
            }
        }
        catch { }
        return null;
    }

    public static bool ValidatePaksDirectory(string candidatePath, out string validatedPath)
    {
        validatedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(candidatePath) || !Directory.Exists(candidatePath))
            return false;

        string[] checks = [
            candidatePath,
            Path.Combine(candidatePath, "Dungeons", "Content", "Paks"),
            Path.Combine(candidatePath, "Content", "Paks")
        ];

        foreach (var dir in checks)
        {
            if (File.Exists(Path.Combine(dir, "Dungeons-Windows.utoc")) &&
                File.Exists(Path.Combine(dir, "Dungeons-Windows.ucas")))
            {
                validatedPath = Path.GetFullPath(dir);
                return true;
            }
        }

        return false;
    }

    public static string? FindGamePaksDir()
    {
        string? saved = LoadSavedPaksDir();
        if (saved != null) return saved;

        string? envPath = Environment.GetEnvironmentVariable("MCD2_PAKS_DIR");
        if (!string.IsNullOrEmpty(envPath) && ValidatePaksDirectory(envPath, out string validEnv))
        {
            return validEnv;
        }

        List<string> candidates = [
            @"C:\Program Files (x86)\Steam\steamapps\common\Minecraft Dungeons II\Dungeons\Content\Paks",
            @"C:\Program Files\Steam\steamapps\common\Minecraft Dungeons II\Dungeons\Content\Paks",
            @"D:\SteamLibrary\steamapps\common\Minecraft Dungeons II\Dungeons\Content\Paks",
            @"E:\SteamLibrary\steamapps\common\Minecraft Dungeons II\Dungeons\Content\Paks",
            @"F:\SteamLibrary\steamapps\common\Minecraft Dungeons II\Dungeons\Content\Paks",
            @"C:\XboxGames\Minecraft Dungeons II\Content\Dungeons\Content\Paks",
            @"D:\XboxGames\Minecraft Dungeons II\Content\Dungeons\Content\Paks"
        ];

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key != null)
            {
                var steamPath = key.GetValue("SteamPath")?.ToString();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(vdf))
                    {
                        string content = File.ReadAllText(vdf);
                        var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                        foreach (Match m in matches)
                        {
                            string p = m.Groups[1].Value.Replace(@"\\", @"\");
                            candidates.Add(Path.Combine(p, "steamapps", "common", "Minecraft Dungeons II", "Dungeons", "Content", "Paks"));
                        }
                    }
                }
            }
        }
        catch { }

        foreach (var c in candidates)
        {
            if (ValidatePaksDirectory(c, out string valid))
            {
                return valid;
            }
        }

        return null;
    }

    private void LoadCatalog()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates = [
            Path.Combine(baseDir, "heroes_catalog.json"),
            Path.Combine(baseDir, "Assets", "heroes_catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "heroes_catalog.json"),
            @"C:\Users\MeowMeow\Desktop\MCD2_Skin_Studio_Native\heroes_catalog.json",
            @"C:\Users\MeowMeow\Desktop\Minecraft  Dunjon\heroes_catalog.json"
        ];

        string? json = null;
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
                break;
            }
        }

        if (json == null)
        {
            using var stream = typeof(GamePatcher).Assembly.GetManifestResourceStream("MCD2SkinStudioWpf.heroes_catalog.json");
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                json = reader.ReadToEnd();
            }
        }

        if (json != null)
        {
            Catalog = JsonSerializer.Deserialize<List<HeroModel>>(json) ?? [];

            string[] iconDirs = [
                Path.Combine(baseDir, "icons"),
                Path.Combine(baseDir, "Assets", "icons"),
                @"C:\Users\MeowMeow\Desktop\MCD2_Skin_Studio_Native\icons",
                @"C:\Users\MeowMeow\Desktop\Minecraft  Dunjon\icons"
            ];

            foreach (var h in Catalog)
            {
                string iconName = Path.GetFileName(h.IconPng);
                string localIcon = string.Empty;
                foreach (var idir in iconDirs)
                {
                    string p = Path.Combine(idir, iconName);
                    if (File.Exists(p))
                    {
                        localIcon = p;
                        break;
                    }
                }
                h.LocalIconPath = localIcon;
                h.IconSource = GetEmbeddedHeroIcon(h.IconPng);
            }
        }
    }

    public static System.Windows.Media.Imaging.BitmapImage? GetEmbeddedHeroIcon(string iconPngPath)
    {
        string iconFilename = Path.GetFileName(iconPngPath);
        string resourceName = $"MCD2SkinStudioWpf.icons.{iconFilename}";
        using var stream = typeof(GamePatcher).Assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            var bi = new System.Windows.Media.Imaging.BitmapImage();
            bi.BeginInit();
            bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bi.StreamSource = stream;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        string[] iconDirs = [
            Path.Combine(AppContext.BaseDirectory, "icons"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "icons"),
            @"C:\Users\MeowMeow\Desktop\MCD2_Skin_Studio_Native\icons",
            @"C:\Users\MeowMeow\Desktop\Minecraft  Dunjon\icons",
            @"C:\Users\MeowMeow\Desktop\icons"
        ];

        foreach (var idir in iconDirs)
        {
            string p = Path.Combine(idir, iconFilename);
            if (File.Exists(p))
            {
                var bi = new System.Windows.Media.Imaging.BitmapImage();
                bi.BeginInit();
                bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(p, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
        }
        return null;
    }

    public HeroModel GetHero(string heroId)
    {
        var h = Catalog.Find(x => string.Equals(x.Id, heroId, StringComparison.OrdinalIgnoreCase));
        if (h == null) throw new ArgumentException($"Héros '{heroId}' introuvable dans le catalogue.");
        return h;
    }

    public byte[] ReadBlock(int blockIndex)
    {
        ulong offsetComp;
        uint uncompMethod;

        using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var br = new BinaryReader(fs))
        {
            fs.Seek(UtocTableOffset + (long)blockIndex * UtocEntrySize, SeekOrigin.Begin);
            offsetComp = br.ReadUInt64();
            uncompMethod = br.ReadUInt32();
        }

        long ucasOffset = (long)(offsetComp & ((1UL << 40) - 1UL));
        int compSize = (int)((offsetComp >> 40) & 0xFFFFFF);
        int uncompSize = (int)(uncompMethod & 0xFFFFFF);
        int method = (int)(uncompMethod >> 24);

        int paddedSize = ((compSize + 15) / 16) * 16;
        byte[] encryptedData = new byte[paddedSize];

        using (var fs = new FileStream(_ucasPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            fs.Seek(ucasOffset, SeekOrigin.Begin);
            fs.ReadExactly(encryptedData, 0, paddedSize);
        }

        byte[] decryptedData = new byte[paddedSize];
        using (var aes = Aes.Create())
        {
            aes.Key = AesKey;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            using var decryptor = aes.CreateDecryptor();
            decryptor.TransformBlock(encryptedData, 0, paddedSize, decryptedData, 0);
        }

        byte[] compressedOnly = new byte[compSize];
        Array.Copy(decryptedData, compressedOnly, compSize);

        if (method == 1)
        {
            byte[] raw = new byte[uncompSize];
            int result = OodleNative.OodleLZ_Decompress(
                compressedOnly, compSize, raw, uncompSize, 1, 0, 0,
                IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, 0);

            if (result != uncompSize)
            {
                throw new InvalidOperationException($"Décompression Oodle échouée pour le bloc {blockIndex}.");
            }
            return raw;
        }

        return compressedOnly;
    }

    public void WriteBlock(int blockIndex, long ucasOffset, byte[] rawBytes, int maxSpace)
    {
        byte[] compBuffer = new byte[rawBytes.Length * 2];
        long compSize = OodleNative.OodleLZ_Compress(
            8, rawBytes, rawBytes.Length, compBuffer, 9,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0);

        if (compSize <= 0)
        {
            throw new InvalidOperationException($"Compression Oodle échouée pour le bloc {blockIndex}.");
        }

        int paddedSize = (((int)compSize + 15) / 16) * 16;
        byte[] encInput = new byte[paddedSize];
        Array.Copy(compBuffer, encInput, compSize);

        byte[] encOutput = new byte[paddedSize];
        using (var aes = Aes.Create())
        {
            aes.Key = AesKey;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            using var encryptor = aes.CreateEncryptor();
            encryptor.TransformBlock(encInput, 0, paddedSize, encOutput, 0);
        }

        const long originalBaseSize = 9110910432;
        long targetOffset;

        if (compSize <= maxSpace)
        {
            targetOffset = ucasOffset;
            using var fs = new FileStream(_ucasPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            fs.Seek(targetOffset, SeekOrigin.Begin);
            fs.Write(encOutput, 0, paddedSize);
        }
        else
        {
            ulong currentOffsetComp;
            using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var br = new BinaryReader(fs))
            {
                fs.Seek(UtocTableOffset + (long)blockIndex * UtocEntrySize, SeekOrigin.Begin);
                currentOffsetComp = br.ReadUInt64();
            }

            long currentOffset = (long)(currentOffsetComp & ((1UL << 40) - 1UL));
            long actualSize = new FileInfo(_ucasPath).Length;
            actualSize = ((actualSize + 15) / 16) * 16;

            if (currentOffset >= originalBaseSize && currentOffset + 65536 >= actualSize)
            {
                targetOffset = currentOffset;
            }
            else
            {
                targetOffset = actualSize;
            }

            using (var fs = new FileStream(_ucasPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(targetOffset, SeekOrigin.Begin);
                fs.Write(encOutput, 0, paddedSize);
            }
        }

        ulong newOffComp = (ulong)(targetOffset & ((1L << 40) - 1L)) | (((ulong)compSize & 0xFFFFFFUL) << 40);
        uint uncompMethod = (1U << 24) | (uint)rawBytes.Length;

        using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        using (var bw = new BinaryWriter(fs))
        {
            fs.Seek(UtocTableOffset + (long)blockIndex * UtocEntrySize, SeekOrigin.Begin);
            bw.Write(newOffComp);
            bw.Write(uncompMethod);
        }
    }

    public void DetectModifiedHeroes(IEnumerable<HeroModel> heroes)
    {
        if (!File.Exists(_utocPath) || !File.Exists(_ucasPath))
            return;

        using var sha = SHA256.Create();

        foreach (var hero in heroes)
        {
            try
            {
                byte[] raw = ReadBlock(hero.SkinBlock);
                int hdrLen = raw.Length - 28 - 16384;
                if (hdrLen >= 0)
                {
                    byte[] hashBytes = sha.ComputeHash(raw, hdrLen, 16384);
                    string hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    if (VanillaSkinHashes.TryGetValue(hero.Id, out string? vanillaHash))
                    {
                        hero.IsModified = !string.Equals(hashStr, vanillaHash, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        hero.IsModified = false;
                    }

                    hero.CurrentGameSkin = ExtractSkinBitmapFromRaw(raw, hdrLen);
                }
            }
            catch
            {
                hero.IsModified = false;
            }
        }
    }

    public Bitmap? ExtractCurrentSkin(string heroId)
    {
        var hero = GetHero(heroId);
        byte[] raw = ReadBlock(hero.SkinBlock);
        int hdrLen = raw.Length - 28 - 16384;
        if (hdrLen < 0) return null;
        return ExtractSkinBitmapFromRaw(raw, hdrLen);
    }

    private static Bitmap ExtractSkinBitmapFromRaw(byte[] raw, int offset)
    {
        var bmp = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        int pos = offset;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                byte b = raw[pos + 0];
                byte g = raw[pos + 1];
                byte r = raw[pos + 2];
                byte a = raw[pos + 3];
                bmp.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                pos += 4;
            }
        }
        return bmp;
    }

    public void PatchSkin(string heroId, Bitmap skinImage)
    {
        var hero = GetHero(heroId);

        BackupBlock(hero.SkinBlock);

        ulong oc0, oc1;
        using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var br = new BinaryReader(fs))
        {
            fs.Seek(UtocTableOffset + (long)hero.SkinBlock * UtocEntrySize, SeekOrigin.Begin);
            oc0 = br.ReadUInt64();
            br.ReadUInt32();
            oc1 = br.ReadUInt64();
        }

        long ucasOffset = (long)(oc0 & ((1UL << 40) - 1UL));
        long nextOffset = (long)(oc1 & ((1UL << 40) - 1UL));

        int maxSpace = (nextOffset > ucasOffset && nextOffset - ucasOffset < 65536)
            ? (int)(nextOffset - ucasOffset)
            : 2048;

        byte[] origRaw = ReadBlock(hero.SkinBlock);
        int headerLength = origRaw.Length - 28 - 16384;
        byte[] header = new byte[headerLength];
        byte[] footer = new byte[28];
        Array.Copy(origRaw, 0, header, 0, headerLength);
        Array.Copy(origRaw, origRaw.Length - 28, footer, 0, 28);

        using var img64 = new Bitmap(skinImage, new Size(64, 64));
        byte[] bgra = new byte[16384];
        int pos = 0;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                var pixel = img64.GetPixel(x, y);
                bgra[pos + 0] = pixel.B;
                bgra[pos + 1] = pixel.G;
                bgra[pos + 2] = pixel.R;
                bgra[pos + 3] = pixel.A;
                pos += 4;
            }
        }

        byte[] newUasset = new byte[headerLength + 16384 + 28];
        Array.Copy(header, 0, newUasset, 0, headerLength);
        Array.Copy(bgra, 0, newUasset, headerLength, 16384);
        Array.Copy(footer, 0, newUasset, headerLength + 16384, 28);

        WriteBlock(hero.SkinBlock, ucasOffset, newUasset, maxSpace);
        hero.IsModified = true;
    }

    public void PatchIcon(string heroId, Bitmap iconImage)
    {
        var hero = GetHero(heroId);
        int block0 = hero.IconBlock;
        int block1 = block0 + 1;

        BackupBlock(block0);
        BackupBlock(block1);

        ulong offComp0, offComp1, offComp2;
        using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var br = new BinaryReader(fs))
        {
            fs.Seek(UtocTableOffset + (long)block0 * UtocEntrySize, SeekOrigin.Begin);
            offComp0 = br.ReadUInt64(); br.ReadUInt32();
            offComp1 = br.ReadUInt64(); br.ReadUInt32();
            offComp2 = br.ReadUInt64();
        }

        long offset0 = (long)(offComp0 & ((1UL << 40) - 1UL));
        long offset1 = (long)(offComp1 & ((1UL << 40) - 1UL));
        long offset2 = (long)(offComp2 & ((1UL << 40) - 1UL));

        int maxSpace0 = (offset1 > offset0 && offset1 - offset0 < 65536) ? (int)(offset1 - offset0) : 20000;
        int maxSpace1 = (offset2 > offset1 && offset2 - offset1 < 65536) ? (int)(offset2 - offset1) : 2048;

        byte[] raw0 = ReadBlock(block0);
        byte[] raw1 = ReadBlock(block1);
        byte[] original = new byte[raw0.Length + raw1.Length];
        Array.Copy(raw0, 0, original, 0, raw0.Length);
        Array.Copy(raw1, 0, original, raw0.Length, raw1.Length);

        int headerLength = original.Length - 28 - 65536;
        byte[] header = new byte[headerLength];
        byte[] footer = new byte[28];
        Array.Copy(original, 0, header, 0, headerLength);
        Array.Copy(original, original.Length - 28, footer, 0, 28);

        byte[] dxt = SkinConverter.EncodeDxt5(iconImage);
        byte[] newUasset = new byte[headerLength + 65536 + 28];
        Array.Copy(header, 0, newUasset, 0, headerLength);
        Array.Copy(dxt, 0, newUasset, headerLength, 65536);
        Array.Copy(footer, 0, newUasset, headerLength + 65536, 28);

        byte[] chunk0 = new byte[65536];
        byte[] chunk1 = new byte[newUasset.Length - 65536];
        Array.Copy(newUasset, 0, chunk0, 0, 65536);
        Array.Copy(newUasset, 65536, chunk1, 0, chunk1.Length);

        byte[] compBuf0 = new byte[chunk0.Length * 2];
        long compSize0 = OodleNative.OodleLZ_Compress(
            8, chunk0, chunk0.Length, compBuf0, 9,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0);

        int pad0 = (((int)compSize0 + 15) / 16) * 16;
        byte[] encIn0 = new byte[pad0];
        Array.Copy(compBuf0, encIn0, compSize0);
        byte[] enc0 = new byte[pad0];
        using (var aes = Aes.Create())
        {
            aes.Key = AesKey; aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None;
            using var encryptor = aes.CreateEncryptor();
            encryptor.TransformBlock(encIn0, 0, pad0, enc0, 0);
        }

        long targetOffset0 = (compSize0 <= maxSpace0) ? offset0 : (((new FileInfo(_ucasPath).Length + 15) / 16) * 16);
        using (var fs = new FileStream(_ucasPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        {
            fs.Seek(targetOffset0, SeekOrigin.Begin);
            fs.Write(enc0, 0, pad0);
        }

        ulong newOff0 = (ulong)(targetOffset0 & ((1L << 40) - 1L)) | (((ulong)compSize0 & 0xFFFFFFUL) << 40);
        using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        {
            using var bw = new BinaryWriter(fs);
            fs.Seek(UtocTableOffset + (long)block0 * UtocEntrySize, SeekOrigin.Begin);
            bw.Write(newOff0);
            bw.Write((1U << 24) | 65536U);
        }

        int pad1 = ((chunk1.Length + 15) / 16) * 16;
        byte[] encIn1 = new byte[pad1];
        Array.Copy(chunk1, encIn1, chunk1.Length);
        byte[] enc1 = new byte[pad1];
        using (var aes = Aes.Create())
        {
            aes.Key = AesKey; aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None;
            using var encryptor = aes.CreateEncryptor();
            encryptor.TransformBlock(encIn1, 0, pad1, enc1, 0);
        }

        long targetOffset1 = (chunk1.Length <= maxSpace1) ? offset1 : (((new FileInfo(_ucasPath).Length + 15) / 16) * 16);
        using (var fs = new FileStream(_ucasPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        {
            fs.Seek(targetOffset1, SeekOrigin.Begin);
            fs.Write(enc1, 0, pad1);
        }

        ulong newOff1 = (ulong)(targetOffset1 & ((1L << 40) - 1L)) | (((ulong)chunk1.Length & 0xFFFFFFUL) << 40);
        using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        {
            using var bw = new BinaryWriter(fs);
            fs.Seek(UtocTableOffset + (long)block1 * UtocEntrySize, SeekOrigin.Begin);
            bw.Write(newOff1);
            bw.Write((uint)chunk1.Length);
        }
    }

    public void PatchMres(string heroId)
    {
        var hero = GetHero(heroId);
        if (hero.MresBlock == 0) return;

        BackupBlock(hero.MresBlock);

        int block = hero.MresBlock;
        long offset = hero.MresUcasOff;
        int maxSpace = hero.MresMaxSpace > 0 ? hero.MresMaxSpace : 4096;

        byte[] raw = ReadBlock(block);
        byte[] header;
        byte[] footer;
        byte[] payload;

        if (hero.MresLen < 10000)
        {
            int headerLength = raw.Length - 28 - 4096;
            header = new byte[headerLength];
            footer = new byte[28];
            Array.Copy(raw, 0, header, 0, headerLength);
            Array.Copy(raw, raw.Length - 28, footer, 0, 28);

            byte[] solid16 = SkinConverter.GenerateBc7Mode6FlatNormal(0, 240, 0, 128);
            payload = new byte[4096];
            for (int i = 0; i < 256; i++)
            {
                Array.Copy(solid16, 0, payload, i * 16, 16);
            }
        }
        else
        {
            int headerLength = raw.Length - 28 - 16384;
            header = new byte[headerLength];
            footer = new byte[28];
            Array.Copy(raw, 0, header, 0, headerLength);
            Array.Copy(raw, raw.Length - 28, footer, 0, 28);

            payload = new byte[16384];
            for (int i = 0; i < 16384; i += 4)
            {
                payload[i + 0] = 255;
                payload[i + 1] = 255;
                payload[i + 2] = 255;
                payload[i + 3] = 0;
            }
        }

        byte[] combined = new byte[header.Length + payload.Length + footer.Length];
        Array.Copy(header, 0, combined, 0, header.Length);
        Array.Copy(payload, 0, combined, header.Length, payload.Length);
        Array.Copy(footer, 0, combined, header.Length + payload.Length, footer.Length);

        WriteBlock(block, offset, combined, maxSpace);
    }

    public void RestoreVanillaSkin(string heroId)
    {
        var hero = GetHero(heroId);

        string baseDir = AppContext.BaseDirectory;
        string[] candidates = [
            Path.Combine(baseDir, "mcd2_extracted_skins"),
            Path.Combine(baseDir, "..", "mcd2_extracted_skins"),
            @"C:\Users\MeowMeow\Desktop\Minecraft  Dunjon\mcd2_extracted_skins"
        ];

        string? originalAssetPath = null;
        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir, hero.SkinAsset, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    originalAssetPath = files[0];
                    break;
                }
            }
        }

        if (originalAssetPath != null && File.Exists(originalAssetPath))
        {
            byte[] origUasset = File.ReadAllBytes(originalAssetPath);
            int maxSpace = hero.SkinLen > 0 ? 2048 : 4096;
            WriteBlock(hero.SkinBlock, hero.SkinUcasOff, origUasset, maxSpace);
            hero.IsModified = false;
            hero.CurrentGameSkin = ExtractCurrentSkin(hero.Id);
            return;
        }

        string backupUcas = Path.Combine(baseDir, "backups", $"backup_block_{hero.SkinBlock}_ucas.bin");
        string backupUtoc = Path.Combine(baseDir, "backups", $"backup_block_{hero.SkinBlock}_utoc.bin");

        if (File.Exists(backupUcas) && File.Exists(backupUtoc))
        {
            byte[] ucasBytes = File.ReadAllBytes(backupUcas);
            byte[] utocBytes = File.ReadAllBytes(backupUtoc);

            using (var fs = new FileStream(_ucasPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(hero.SkinUcasOff, SeekOrigin.Begin);
                fs.Write(ucasBytes, 0, ucasBytes.Length);
            }

            using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(UtocTableOffset + (long)hero.SkinBlock * UtocEntrySize, SeekOrigin.Begin);
                fs.Write(utocBytes, 0, utocBytes.Length);
            }

            hero.IsModified = false;
            hero.CurrentGameSkin = ExtractCurrentSkin(hero.Id);
            return;
        }

        throw new FileNotFoundException($"Fichier original {hero.SkinAsset} introuvable pour restaurer la texture.");
    }

    private void BackupBlock(int blockIndex)
    {
        try
        {
            string backupDir = Path.Combine(AppContext.BaseDirectory, "backups");
            Directory.CreateDirectory(backupDir);

            string utocBackup = Path.Combine(backupDir, $"backup_block_{blockIndex}_utoc.bin");
            string ucasBackup = Path.Combine(backupDir, $"backup_block_{blockIndex}_ucas.bin");

            if (File.Exists(utocBackup) && File.Exists(ucasBackup))
                return;

            ulong oc;
            using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var br = new BinaryReader(fs))
            {
                fs.Seek(UtocTableOffset + (long)blockIndex * UtocEntrySize, SeekOrigin.Begin);
                oc = br.ReadUInt64();
                br.ReadUInt32();
            }

            long ucasOff = (long)(oc & ((1UL << 40) - 1UL));
            int compSz = (int)((oc >> 40) & 0xFFFFFF);
            int padSz = ((compSz + 15) / 16) * 16;

            byte[] utocData = new byte[12];
            using (var fs = new FileStream(_utocPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(UtocTableOffset + (long)blockIndex * UtocEntrySize, SeekOrigin.Begin);
                fs.ReadExactly(utocData, 0, 12);
            }
            File.WriteAllBytes(utocBackup, utocData);

            byte[] ucasData = new byte[padSz];
            using (var fs = new FileStream(_ucasPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(ucasOff, SeekOrigin.Begin);
                fs.ReadExactly(ucasData, 0, padSz);
            }
            File.WriteAllBytes(ucasBackup, ucasData);
        }
        catch { }
    }
}
