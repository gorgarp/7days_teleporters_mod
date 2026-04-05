using System.Collections.Generic;
using UnityEngine;

public class NetPackageTeleportPadSync : NetPackage
{
    private Dictionary<Vector3i, string> _padMap;

    public NetPackageTeleportPadSync Setup(Dictionary<Vector3i, string> padMap)
    {
        _padMap = padMap;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        _padMap = new Dictionary<Vector3i, string>();
        var count = br.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
            var name = br.ReadString();
            _padMap[pos] = name;
        }
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(_padMap.Count);
        foreach (var entry in _padMap)
        {
            bw.Write(entry.Key.x);
            bw.Write(entry.Key.y);
            bw.Write(entry.Key.z);
            bw.Write(entry.Value);
        }
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        if (world == null) return;
        TeleportPadManager.Instance.ReplaceMap(_padMap);
    }

    public override int GetLength()
    {
        int len = 4;
        if (_padMap != null)
        {
            foreach (var entry in _padMap)
                len += 12 + (entry.Value != null ? entry.Value.Length * 2 + 4 : 4);
        }
        return len;
    }
}
