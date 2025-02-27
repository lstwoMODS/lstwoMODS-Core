using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace lstwoMODS_Core;

public class AssetUtils
{
    public List<UnitySpecificAssetBundle> AssetBundles = new();
    
    public AssetBundle LoadCompatibleAssetBundle()
    {
        AssetBundles = AssetBundles.OrderBy(b => b.Version).ToList();
        var currentVersion = new UnityVersion(Application.unityVersion);

        var compatibleBundles = from assetBundle in AssetBundles where assetBundle.CheckCompatible(currentVersion) select assetBundle;
        var compatibleBundle = compatibleBundles.FirstOrDefault();
        var bundle = compatibleBundle?.Load();
        
        Debug.Log(string.Join(", ", AssetBundles));
        Debug.Log(currentVersion);
        Debug.Log(string.Join(", ", compatibleBundles));
        Debug.Log(compatibleBundle);
        Debug.Log(bundle);
            
        return bundle;
    }
}

public class UnitySpecificAssetBundle
{
    public string AssetBundleResourceName;
    public UnityVersion Version;

    public UnitySpecificAssetBundle(string assetBundleResourceName, UnityVersion version)
    {
        AssetBundleResourceName = assetBundleResourceName;
        Version = version;
    }
    
    public bool CheckCompatible(UnityVersion current)
    {
        return Version.major switch
        {
            4 => current.major == 4,
            5 => current.major >= 5 && current.major < 2018,
            >= 2017 and < 6000 => current.major >= Version.major,
            >= 6000 => current.major >= 6000,
            _ => false
        };
    }

    public AssetBundle Load()
    {
        return AssetBundle.LoadFromMemory(ReadFully(typeof(Plugin).Assembly.GetManifestResourceStream(AssetBundleResourceName)));
    }
    
    private static byte[] ReadFully(Stream input)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        
        while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
        {
            ms.Write(buffer, 0, read);
        }
        
        return ms.ToArray();
    }

    public override string ToString()
    {
        return Version.ToString();
    }
}

public struct UnityVersion : IComparable<UnityVersion>
{
    public int major;
    public int minor;
    public int patch;

    public UnityVersion(int major, int minor, int patch)
    {
        this.major = major;
        this.minor = minor;
        this.patch = patch;
    }

    public UnityVersion(string versionString)
    {
        var split = versionString.Split('.');

        major = int.Parse(split[0]);
        minor = int.Parse(split[1]);

        var patchString = split[2];
        var patchNumber = System.Text.RegularExpressions.Regex.Match(patchString, @"^\d+");
    
        patch = int.Parse(patchNumber.Value);
    }
    
    public int CompareTo(UnityVersion other)
    {
        if (major != other.major) return other.major.CompareTo(major);
        if (minor != other.minor) return other.minor.CompareTo(minor);
        return other.patch.CompareTo(patch);
    }

    public override string ToString()
    {
        return $"{major}.{minor}.{patch}";
    }
}