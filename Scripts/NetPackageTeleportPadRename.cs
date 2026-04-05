using UnityEngine;

public class NetPackageTeleportPadRename : NetPackage
{
    private Vector3i _position;
    private string _newName;

    public NetPackageTeleportPadRename Setup(Vector3i position, string newName)
    {
        _position = position;
        _newName = newName;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        _position = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
        _newName = br.ReadString();
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(_position.x);
        bw.Write(_position.y);
        bw.Write(_position.z);
        bw.Write(_newName ?? "");
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        if (world == null) return;

        var te = world.GetTileEntity(0, _position) as TileEntityTeleportPad;
        if (te != null)
        {
            te.SetPadName(_newName);
        }

        TeleportPadManager.Instance.RenamePad(_position, _newName);
    }

    public override int GetLength()
    {
        return 12 + (_newName != null ? _newName.Length * 2 + 4 : 4);
    }
}
