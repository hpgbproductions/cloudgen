using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using IniParser;
using IniParser.Model;
using Jundroo.SimplePlanes.ModTools;
using UnityEngine;

public class CloudGeneratorScript : MonoBehaviour
{
    [SerializeField] private ParticleSystem CloudParticleSystem;
    private ParticleSystem.MainModule psMainModule;
    private ParticleSystemRenderer psRenderer;
    private ParticleSystem.Particle[] psParticles;

    // The default cloud generator settings
    [SerializeField] private TextAsset DefaultSettings;
    [SerializeField] private string DefaultSettingsName;
    private string DefaultSettingsPath;

    // Other cloud generator settings provided with the mod
    [SerializeField] private TextAsset[] OtherSettings;
    [SerializeField] private string[] OtherSettingsNames;

    // The save data contains the name of the last used settings
    [SerializeField] private TextAsset DefaultSaveData;
    [SerializeField] private string DefaultSaveDataName;
    private string DefaultSaveDataPath;

    private string CloudGenFolderName = "CLOUDGEN";
    private string CloudGenFolderPath;

    private FileIniDataParser IniParser = new FileIniDataParser();
    private IniData SaveData;
    private IniData SettingsData;

    private bool CurrentSettingsValid = true;
    private string LastRun = "";
    private string SaveDataVersion = "1";
    private string SettingsVersion = "1";

    // [CloudGeneratorSettings]
    private int MinParticles = 49900;
    private int MaxParticles = 50000;
    private float NoiseScale = 0.0001f;
    private float CloudTypeNoiseScale = 0.000001f;
    private float StartAltitude = 1500f;
    private float IntervalX = 400f;
    private float IntervalY = 400f;
    private float IntervalZ = 400f;
    private float CloudChunkSize = 4000f;
    private int MaxChunks = 200;

    private int Octaves = 4;
    private float Persistence = 0.5f;
    private float Lacunarity = 2f;

    private string StartWeather = "0";

    private bool CastShadows = false;
    private bool ReceiveShadows = false;
    private float MaxParticleSizeOnScreen = 5f;
    private float CameraNearFade = 100f;
    private float CameraFarFade = 0f;

    // [BaseClouds]
    private float BaseScale = 4000f;
    private float BaseMinParticleSize = 1000f;
    private float BaseMaxParticleSize = 3000f;
    private Vector3 BaseRandomOffset = new Vector3(150f, 150f, 150f);

    // [HeadClouds]
    private float HeadScale = 7000f;
    private float HeadMinParticleSize = 2000f;
    private float HeadMaxParticleSize = 6000f;
    private float HeadUpperHalfScale = 2f;
    private float HeadLowerHalfScale = 5f;
    private int HeadIntervalCount = 2;
    private Vector3 HeadRandomOffset = new Vector3(300f, 300f, 300f);

    // Linear maps
    private LinearMap CloudTypeMap = new LinearMap(0f, 1f, 0f, 1f);
    private LinearMap BaseHeightMap = new LinearMap(0f, 0.5f, 3f, 1f);
    private LinearMap BaseSinkFactorMap = new LinearMap(0f, 0.7f, 0.9f, 0.2f);
    private LinearMap HeadHeightMap = new LinearMap(0.3f, 0.7f, 0.5f, 1f);
    private LinearMap HeadSinkFactorMap = new LinearMap(0f, 1f, 0.7f, 0.2f);
    private LinearMap HeadLowerFactorMap = new LinearMap(0f, 1f, 0.5f, 0.1f);
    private LinearMap HeadGenerateThresholdMap = new LinearMap(0f, 1f, 1f, 0.3f);

    private MultiPerlinNoise CloudTypeNoise;
    private MultiPerlinNoise CloudPatternNoise;

    // Material adjustment to lighting
    private Light SkyDirectionalLight;
    private Color CloudOriginalEmissionColor;
    private int EmissionColorProperty;

    private void Awake()
    {
        CloudGenFolderPath = Path.Combine(Application.persistentDataPath, "NACHSAVE", CloudGenFolderName);
        if (!Directory.Exists(CloudGenFolderPath))
        {
            Directory.CreateDirectory(CloudGenFolderPath);
        }
        DefaultSettingsPath = Path.Combine(CloudGenFolderPath, DefaultSettingsName);
        DefaultSaveDataPath = Path.Combine(CloudGenFolderPath, DefaultSaveDataName);
        LastRun = DefaultSettingsName;

        // Register commands
        ServiceProvider.Instance.DevConsole.RegisterCommand<string>("LoadCloudSettings", SelectSettingsFile);
        ServiceProvider.Instance.DevConsole.RegisterCommand("RegenerateClouds", Regenerate);
    }

    private void Start()
    {
        psMainModule = CloudParticleSystem.main;
        psRenderer = CloudParticleSystem.GetComponent<ParticleSystemRenderer>();
        EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        CloudOriginalEmissionColor = psRenderer.material.GetColor("_EmissionColor");
        
        if (!File.Exists(DefaultSaveDataPath))
        {
            // Create the default files
            if (!File.Exists(DefaultSettingsPath))
            {
                File.WriteAllText(DefaultSettingsPath, DefaultSettings.text);
            }
            if (!File.Exists(DefaultSaveDataPath))
            {
                File.WriteAllText(DefaultSaveDataPath, DefaultSaveData.text);
            }
            for (int i = 0; i < OtherSettings.Length; i++)
            {
                string path = Path.Combine(CloudGenFolderPath, OtherSettingsNames[i]);
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, OtherSettings[i].text);
                }
            }
        }

        if (File.Exists(DefaultSaveDataPath))
        {
            // Try to load the last run settings
            Debug.Log("Reading cloud generator save data...");
            TryReadLastRunSettings(DefaultSaveDataPath);
        }
        else
        {
            Debug.LogError("Failed to generate cloud generator save data file");
        }
        Generate();

        Component[] allComponents = FindObjectsOfType<Component>();
        TypeInfo skyComponentInfo;
        PropertyInfo lightSourceInfo;
        foreach (Component c in allComponents)
        {
            if (c.GetType().Name == "TOD_Components")
            {
                skyComponentInfo = c.GetType().GetTypeInfo();
                lightSourceInfo = skyComponentInfo.GetProperty("LightSource");
                SkyDirectionalLight = (Light)lightSourceInfo.GetValue(c);
                break;
            }
        }

        // Start the automatic emission color adjustment
        if (CloudOriginalEmissionColor != null && SkyDirectionalLight != null)
        {
            InvokeRepeating(nameof(UpdateMaterialBrightness), 1f, 0.1f);
        }
    }

    private void Regenerate()
    {
        if (string.IsNullOrEmpty(LastRun))
        {
            Debug.LogError("Cannot regenerate clouds: No last run settings");
            return;
        }

        string LastRunPath = Path.Combine(CloudGenFolderPath, LastRun);
        if (!File.Exists(LastRunPath))
        {
            Debug.LogError("Cannot regenerate clouds: Last run settings file does not exist, or has been moved");
            return;
        }
        else if (!TryReadSettings(LastRunPath))
        {
            Debug.LogError("Cannot regenerate clouds: Failed to load settings data");
            return;
        }

        Generate();
    }

    private void OnDestroy()
    {
        ServiceProvider.Instance.DevConsole.UnregisterCommand("RegenerateClouds");
    }

    private void Generate()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        CloudParticleSystem.transform.position = new Vector3(
            ServiceProvider.Instance.PlayerAircraft.MainCockpitPosition.x - CloudChunkSize * 0.5f,
            ServiceProvider.Instance.PlayerAircraft.MainCockpitPosition.y - ServiceProvider.Instance.PlayerAircraft.Altitude,
            ServiceProvider.Instance.PlayerAircraft.MainCockpitPosition.z - CloudChunkSize * 0.5f
            );

        CloudParticleSystem.Clear();
        psMainModule.maxParticles = MaxParticles;
        CloudParticleSystem.Emit(MaxParticles);

        psParticles = new ParticleSystem.Particle[MaxParticles];
        CloudParticleSystem.GetParticles(psParticles);

        psRenderer.shadowCastingMode = CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        psRenderer.receiveShadows = ReceiveShadows;
        psRenderer.maxParticleSize = MaxParticleSizeOnScreen;
        psRenderer.material.SetFloat("_CameraNearFadeDistance", CameraNearFade);
        psRenderer.material.SetFloat("_CameraFarFadeDistance", CameraFarFade);

        CloudTypeNoise = new MultiPerlinNoise(Octaves, Persistence, Lacunarity);
        CloudPatternNoise = new MultiPerlinNoise(Octaves, Persistence, Lacunarity);

        // Number of particles edited
        int ctr = 0;

        int usedChunks;
        int maxChunks = Mathf.Min(MaxChunks, ManhattanSpiral.Length);
        int columnsPerChunkX = Mathf.RoundToInt(CloudChunkSize / IntervalX);
        int columnsPerChunkZ = Mathf.RoundToInt(CloudChunkSize / IntervalZ);

        float chunkStartX, chunkStartZ;
        float x, z;
        float cloudType, baseHeight, baseSinkFactor, headHeight, headStartAltitude, headSinkFactor, headLowerFactor, headGenerateThreshold;
        float cloudPattern, baseTop, headTop, headBottom;
        float headIntervalY = IntervalY * HeadIntervalCount;

        for (int chunk = 0; chunk < maxChunks; chunk++)
        {
            chunkStartX = ManhattanSpiral.GetValue(chunk).x * CloudChunkSize;
            chunkStartZ = ManhattanSpiral.GetValue(chunk).y * CloudChunkSize;

            for (int columnZ = 0; columnZ < columnsPerChunkZ; columnZ++)
            {
                z = chunkStartZ + columnZ * IntervalZ;

                for (int columnX = 0; columnX < columnsPerChunkX; columnX++)
                {
                    x = chunkStartX + columnX * IntervalX;

                    // Get the cloud type and associated modifier values
                    cloudType = CloudTypeMap.GetValueAt(CloudTypeNoise.GetValueAt(x * CloudTypeNoiseScale, z * CloudTypeNoiseScale));
                    baseHeight = BaseScale * BaseHeightMap.GetValueAt(cloudType);
                    baseSinkFactor = BaseSinkFactorMap.GetValueAt(cloudType);
                    headHeight = HeadScale * HeadHeightMap.GetValueAt(cloudType);
                    headStartAltitude = StartAltitude + headHeight;
                    headSinkFactor = HeadSinkFactorMap.GetValueAt(cloudType);
                    headLowerFactor = HeadLowerFactorMap.GetValueAt(cloudType);
                    headGenerateThreshold = HeadGenerateThresholdMap.GetValueAt(cloudType);

                    // Get the noise values at the coordinate
                    cloudPattern = CloudPatternNoise.GetValueAt(x * NoiseScale, z * NoiseScale);
                    baseTop = StartAltitude + baseHeight * (cloudPattern - baseSinkFactor);
                    headTop = headStartAltitude + headHeight * HeadUpperHalfScale * (cloudPattern - headSinkFactor) - headHeight * headLowerFactor;
                    headBottom = Mathf.Max(baseTop, StartAltitude, headStartAltitude - headHeight * HeadLowerHalfScale * ((cloudPattern * cloudPattern) - headSinkFactor) - headHeight * headLowerFactor);

                    /*
                    Debug.Log(@$"[Chunk {chunk}]: ({x}, {z})
Cloud type {cloudType}
Cloud pattern {cloudPattern}
Head range {headBottom} ~ {headTop}
Base range {StartAltitude} ~ {baseTop}");
                    */

                    // Lower clouds
                    for (float y = baseTop; y > StartAltitude; y -= IntervalY)
                    {
                        psParticles[ctr].position = new Vector3(
                            x + UnityEngine.Random.Range(-BaseRandomOffset.x, BaseRandomOffset.x),
                            y + UnityEngine.Random.Range(-BaseRandomOffset.y, BaseRandomOffset.y),
                            z + UnityEngine.Random.Range(-BaseRandomOffset.z, BaseRandomOffset.z)
                            );
                        psParticles[ctr].startSize = UnityEngine.Random.Range(BaseMinParticleSize, BaseMaxParticleSize);
                        ctr++;
                    }

                    // Upper clouds
                    if (columnX % HeadIntervalCount == 0 && columnZ % HeadIntervalCount == 0 && cloudPattern > headGenerateThreshold)
                    {
                        for (float y = headTop; y > headBottom; y -= headIntervalY)
                        {
                            psParticles[ctr].position = new Vector3(
                                x + UnityEngine.Random.Range(-HeadRandomOffset.x, HeadRandomOffset.x),
                                y + UnityEngine.Random.Range(-HeadRandomOffset.y, HeadRandomOffset.y),
                                z + UnityEngine.Random.Range(-HeadRandomOffset.z, HeadRandomOffset.z)
                                );
                            psParticles[ctr].startSize = UnityEngine.Random.Range(HeadMinParticleSize, HeadMaxParticleSize);
                            ctr++;
                        }
                    }

                    if (ctr >= MinParticles)
                    {
                        // Stop generating particles once the minimum number is reached
                        usedChunks = chunk;
                        goto EndParticleGenerator;
                    }
                }
            }
        }
        usedChunks = MaxChunks;

    EndParticleGenerator:;

        // Remove the remaining particles that failed to generate
        for (int i = ctr; i < MaxParticles; i++)
        {
            psParticles[i].remainingLifetime = -1f;
        }

        CloudParticleSystem.SetParticles(psParticles);
        Debug.Log($@"Produced {ctr} cloud particles in {sw.ElapsedMilliseconds} ms
{usedChunks} of {maxChunks} chunks used
Cloud generation profile: {LastRun}");

        // Set the weather
        WeatherPreset weather;
        if (Enum.TryParse(StartWeather, true, out weather))
        {
            ServiceProvider.Instance.EnvironmentManager.UpdateWeather(weather, 0f, true);
            ServiceProvider.Instance.EnvironmentManager.DynamicWeatherEnabled = false;
        }
    }

    private void UpdateMaterialBrightness()
    {
        psRenderer.material.SetColor(EmissionColorProperty, CloudOriginalEmissionColor * SkyDirectionalLight.intensity);
    }

    private void SelectSettingsFile(string name)
    {
        string path = Path.Combine(CloudGenFolderPath, name);
        bool success;

        if (File.Exists(path))
        {
            success = TryReadSettings(path);
        }
        else
        {
            Debug.LogError("The cloud generator file does not exist: " + name);
            return;
        }

        if (success)
        {
            CurrentSettingsValid = true;
            LastRun = name;
            SaveData["SaveData"]["LastRun"] = name;
            IniParser.WriteFile(DefaultSaveDataPath, SaveData);

            Debug.Log($"Loaded cloud settings data: {name} (restart or use RegenerateClouds to get new clouds)");
        }
    }

    /// <summary>
    /// Read the save data, and read the cloud settings file associated with the save data
    /// </summary>
    private bool TryReadLastRunSettings(string saveDataPath)
    {
        CurrentSettingsValid = false;

        try
        {
            SaveData = IniParser.ReadFile(saveDataPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse cloud generator save data:" + ex.Message);
            return false;
        }

        try
        {
            SaveDataVersion = SaveData["CloudGenerator"]["Version"];
            LastRun = SaveData["SaveData"]["LastRun"];
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to apply cloud generator save data:" + ex.Message);
            return false;
        }

        bool result = TryReadSettings(Path.Combine(CloudGenFolderPath, LastRun));
        return result;
    }

    /// <summary>
    /// Read and apply cloud settings. Returns true if successful, otherwise returns false.
    /// </summary>
    private bool TryReadSettings(string settingsPath)
    {
        Debug.Log("Reading cloud generator settings: " + Path.GetFileName(settingsPath));

        try
        {
            SettingsData = IniParser.ReadFile(settingsPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse cloud generator settings data:" + ex.Message);
            return false;
        }

        try
        {
            SettingsVersion = SettingsData["CloudGenerator"]["Version"];

            MinParticles = int.Parse(SettingsData["CloudGeneratorSettings"]["MinParticles"]);
            MaxParticles = int.Parse(SettingsData["CloudGeneratorSettings"]["MaxParticles"]);
            NoiseScale = float.Parse(SettingsData["CloudGeneratorSettings"]["NoiseScale"]);
            CloudTypeNoiseScale = float.Parse(SettingsData["CloudGeneratorSettings"]["CloudTypeNoiseScale"]);
            StartAltitude = float.Parse(SettingsData["CloudGeneratorSettings"]["StartAltitude"]);
            IntervalX = float.Parse(SettingsData["CloudGeneratorSettings"]["IntervalX"]);
            IntervalY = float.Parse(SettingsData["CloudGeneratorSettings"]["IntervalY"]);
            IntervalZ = float.Parse(SettingsData["CloudGeneratorSettings"]["IntervalZ"]);
            MaxChunks = int.Parse(SettingsData["CloudGeneratorSettings"]["MaxChunks"]);
            CloudChunkSize = float.Parse(SettingsData["CloudGeneratorSettings"]["ChunkSize"]);
            Octaves = int.Parse(SettingsData["CloudGeneratorSettings"]["Octaves"]);
            Persistence = float.Parse(SettingsData["CloudGeneratorSettings"]["Persistence"]);
            Lacunarity = float.Parse(SettingsData["CloudGeneratorSettings"]["Lacunarity"]);
            StartWeather = SettingsData["CloudGeneratorSettings"]["OverrideWeather"];

            CastShadows = bool.Parse(SettingsData["CloudGeneratorSettings"]["CastShadows"]);
            ReceiveShadows = bool.Parse(SettingsData["CloudGeneratorSettings"]["ReceiveShadows"]);
            MaxParticleSizeOnScreen = float.Parse(SettingsData["CloudGeneratorSettings"]["MaxParticleSizeOnScreen"]);
            CameraNearFade = float.Parse(SettingsData["CloudGeneratorSettings"]["CameraNearFade"]);
            CameraFarFade = float.Parse(SettingsData["CloudGeneratorSettings"]["CameraFarFade"]);

            BaseScale = float.Parse(SettingsData["BaseClouds"]["Scale"]);
            BaseMinParticleSize = float.Parse(SettingsData["BaseClouds"]["MinParticleSize"]);
            BaseMaxParticleSize = float.Parse(SettingsData["BaseClouds"]["MaxParticleSize"]);
            BaseRandomOffset = ReadVector3Keys(SettingsData, "BaseClouds", "RandomOffsetX", "RandomOffsetY", "RandomOffsetZ");

            HeadScale = float.Parse(SettingsData["HeadClouds"]["Scale"]);
            HeadMinParticleSize = float.Parse(SettingsData["HeadClouds"]["MinParticleSize"]);
            HeadMaxParticleSize = float.Parse(SettingsData["HeadClouds"]["MaxParticleSize"]);
            HeadUpperHalfScale = float.Parse(SettingsData["HeadClouds"]["UpperHalfScale"]);
            HeadLowerHalfScale = float.Parse(SettingsData["HeadClouds"]["LowerHalfScale"]);
            HeadIntervalCount = int.Parse(SettingsData["HeadClouds"]["IntervalCount"]);
            HeadRandomOffset = ReadVector3Keys(SettingsData, "HeadClouds", "RandomOffsetX", "RandomOffsetY", "RandomOffsetZ");

            CloudTypeMap = ReadLinearMapSection(SettingsData, "CloudTypeMap");
            BaseHeightMap = ReadLinearMapSection(SettingsData, "BaseHeightMap");
            BaseSinkFactorMap = ReadLinearMapSection(SettingsData, "BaseSinkFactorMap");
            HeadHeightMap = ReadLinearMapSection(SettingsData, "HeadHeightMap");
            HeadSinkFactorMap = ReadLinearMapSection(SettingsData, "HeadSinkFactorMap");
            HeadLowerFactorMap = ReadLinearMapSection(SettingsData, "HeadLowerFactorMap");
            HeadGenerateThresholdMap = ReadLinearMapSection(SettingsData, "HeadGenerateThresholdMap");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to apply cloud generator settings data:" + ex.Message);
            return false;
        }

        Debug.Log("Applied cloud generator settings.");
        CurrentSettingsValid = true;
        return true;
    }

    private LinearMap ReadLinearMapSection(IniData data, string sectionName)
    {
        return new LinearMap(
            float.Parse(data[sectionName]["t0"]),
            float.Parse(data[sectionName]["t1"]),
            float.Parse(data[sectionName]["x0"]),
            float.Parse(data[sectionName]["x1"])
            );
    }

    private Vector3 ReadVector3Keys(IniData data, string sectionName, string x_key, string y_key, string z_key)
    {
        return new Vector3(
            float.Parse(data[sectionName][x_key]),
            float.Parse(data[sectionName][y_key]),
            float.Parse(data[sectionName][z_key])
            );
    }
}
