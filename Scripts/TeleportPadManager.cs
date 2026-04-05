using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TeleportPadManager
{
    private const string SAVE_FILE = "TeleportPads.dat";
    private static readonly byte Version = 1;

    private Dictionary<Vector3i, string> PadMap = new Dictionary<Vector3i, string>();
    private static TeleportPadManager _instance;
    private ThreadManager.ThreadInfo dataSaveThreadInfo;

    private ConnectionManager _connectionManager => SingletonMonoBehaviour<ConnectionManager>.Instance;

    public static TeleportPadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TeleportPadManager();
                _instance.Init();
            }
            return _instance;
        }
    }

    public void Init()
    {
        if (!_connectionManager.IsServer) return;
        Log.Out("[TeleportPads] Initializing TeleportPadManager...");
        ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawned);
    }

    private void OnGameStartDone(ref ModEvents.SGameStartDoneData data)
    {
        Log.Out("[TeleportPads] Loading pad data...");
        Load();
    }

    private void OnPlayerSpawned(ref ModEvents.SPlayerSpawnedInWorldData data)
    {
        if (data.ClientInfo == null) return;
        Log.Out($"[TeleportPads] Syncing pad map to player {data.ClientInfo.entityId} ({PadMap.Count} pads)");
        var package = NetPackageManager.GetPackage<NetPackageTeleportPadSync>();
        package.Setup(PadMap);
        data.ClientInfo.SendPackage(package);
    }

    public void RegisterPad(Vector3i position, string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        if (!_connectionManager.IsServer)
        {
            _connectionManager.SendToServer(
                NetPackageManager.GetPackage<NetPackageTeleportPadAdd>().Setup(position, name));
            return;
        }

        if (PadMap.ContainsKey(position))
        {
            if (PadMap[position] == name) return;
            PadMap[position] = name;
        }
        else
        {
            PadMap.Add(position, name);
        }

        _connectionManager.SendPackage(
            NetPackageManager.GetPackage<NetPackageTeleportPadAdd>().Setup(position, name));
        Save();
    }

    public void RemovePad(Vector3i position)
    {
        if (!_connectionManager.IsServer)
        {
            _connectionManager.SendToServer(
                NetPackageManager.GetPackage<NetPackageTeleportPadRemove>().Setup(position));
            return;
        }

        PadMap.Remove(position);
        _connectionManager.SendPackage(
            NetPackageManager.GetPackage<NetPackageTeleportPadRemove>().Setup(position));
        Save();
    }

    public void RenamePad(Vector3i position, string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            RemovePad(position);
            return;
        }
        RegisterPad(position, newName);
    }

    public void ReplaceMap(Dictionary<Vector3i, string> newMap)
    {
        if (_connectionManager.IsServer) return;
        PadMap.Clear();
        foreach (var entry in newMap)
            PadMap[entry.Key] = entry.Value;
    }

    public void AddFromNetwork(Vector3i position, string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        PadMap[position] = name;

        if (_connectionManager.IsServer)
        {
            _connectionManager.SendPackage(
                NetPackageManager.GetPackage<NetPackageTeleportPadAdd>().Setup(position, name));
            Save();
        }
    }

    public void RemoveFromNetwork(Vector3i position)
    {
        PadMap.Remove(position);

        if (_connectionManager.IsServer)
        {
            _connectionManager.SendPackage(
                NetPackageManager.GetPackage<NetPackageTeleportPadRemove>().Setup(position));
            Save();
        }
    }

    public List<KeyValuePair<Vector3i, string>> GetDestinations(Vector3i excludePos)
    {
        var result = new List<KeyValuePair<Vector3i, string>>();
        foreach (var entry in PadMap)
        {
            if (entry.Key != excludePos)
                result.Add(entry);
        }
        result.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public string GetPadName(Vector3i position)
    {
        return PadMap.TryGetValue(position, out var name) ? name : "";
    }

    public int PadCount => PadMap.Count;

    private void Save()
    {
        if (dataSaveThreadInfo != null && ThreadManager.ActiveThreads.ContainsKey("silent_TeleportPadSave"))
            return;

        var stream = MemoryPools.poolMemoryStream.AllocSync(true);
        using (var bw = MemoryPools.poolBinaryWriter.AllocSync(false))
        {
            bw.SetBaseStream(stream);
            bw.Write(Version);
            bw.Write(PadMap.Count);
            foreach (var entry in PadMap)
            {
                bw.Write(entry.Key.x);
                bw.Write(entry.Key.y);
                bw.Write(entry.Key.z);
                bw.Write(entry.Value);
            }
        }

        dataSaveThreadInfo = ThreadManager.StartThread("silent_TeleportPadSave", null,
            new ThreadManager.ThreadFunctionLoopDelegate(SaveThreaded), null, stream, null, false);
    }

    private int SaveThreaded(ThreadManager.ThreadInfo _threadInfo)
    {
        var stream = (PooledExpandableMemoryStream)_threadInfo.parameter;
        var path = $"{GameIO.GetSaveGameDir()}/{SAVE_FILE}";
        if (!Directory.Exists(GameIO.GetSaveGameDir())) return -1;

        if (File.Exists(path))
            File.Copy(path, $"{path}.bak", true);

        stream.Position = 0L;
        StreamUtils.WriteStreamToFile(stream, path);
        MemoryPools.poolMemoryStream.FreeSync(stream);
        return -1;
    }

    private void Load()
    {
        PadMap.Clear();
        var path = $"{GameIO.GetSaveGameDir()}/{SAVE_FILE}";

        if (!File.Exists(path))
        {
            Log.Out("[TeleportPads] No save file found, starting fresh.");
            return;
        }

        try
        {
            using (var fs = File.OpenRead(path))
            using (var br = MemoryPools.poolBinaryReader.AllocSync(false))
            {
                br.SetBaseStream(fs);
                br.ReadByte();
                var count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var pos = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
                    var name = br.ReadString();
                    PadMap[pos] = name;
                }
            }
            Log.Out($"[TeleportPads] Loaded {PadMap.Count} pads.");
        }
        catch (Exception ex)
        {
            Log.Error($"[TeleportPads] Error loading pad data: {ex.Message}");
            var backup = $"{path}.bak";
            if (File.Exists(backup))
            {
                try
                {
                    using (var fs = File.OpenRead(backup))
                    using (var br = MemoryPools.poolBinaryReader.AllocSync(false))
                    {
                        br.SetBaseStream(fs);
                        br.ReadByte();
                        var count = br.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            var pos = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
                            var name = br.ReadString();
                            PadMap[pos] = name;
                        }
                    }
                    Log.Out($"[TeleportPads] Loaded {PadMap.Count} pads from backup.");
                }
                catch (Exception ex2)
                {
                    Log.Error($"[TeleportPads] Error loading backup: {ex2.Message}");
                }
            }
        }
    }
}
