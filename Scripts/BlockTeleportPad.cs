using Platform;
using UnityEngine;

public class BlockTeleportPad : Block
{
    public BlockTeleportPad()
    {
        HasTileEntity = true;
        Log.Out("[TeleportPads] BlockTeleportPad constructor called");
    }

    public override void Init()
    {
        base.Init();
        Log.Out("[TeleportPads] BlockTeleportPad.Init() called for block: " + GetBlockName());
    }

    private new BlockActivationCommand[] cmds = new BlockActivationCommand[]
    {
        new BlockActivationCommand("teleport", "map_waypoints", true, true),
        new BlockActivationCommand("edit", "pen", false, true),
        new BlockActivationCommand("take", "hand", false, false)
    };

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos,
        BlockValue _blockValue, PlatformUserIdentifierAbs _addedByPlayer)
    {
        if (_blockValue.ischild)
        {
            base.OnBlockAdded(world, _chunk, _blockPos, _blockValue, _addedByPlayer);
            return;
        }

        var te = new TileEntityTeleportPad(_chunk);
        te.localChunkPos = World.toBlock(_blockPos);
        te.SetOwner(_addedByPlayer ?? PlatformManager.InternalLocalUserIdentifier);
        _chunk.AddTileEntity(te);
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue, _addedByPlayer);
    }

    public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos,
        BlockValue _blockValue)
    {
        if (!_blockValue.ischild)
        {
            TeleportPadManager.Instance.RemovePad(_blockPos);
            _chunk.RemoveTileEntityAt<TileEntityTeleportPad>((World)world, World.toBlock(_blockPos));
        }
        base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        if (_blockValue.ischild) return;

        var te = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityTeleportPad;
        if (te != null && !string.IsNullOrEmpty(te.PadName))
        {
            TeleportPadManager.Instance.RegisterPad(_blockPos, te.PadName);
        }
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos,
        int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        if (_blockValue.ischild) return;

        var te = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityTeleportPad;
        if (te != null && !string.IsNullOrEmpty(te.PadName))
        {
            TeleportPadManager.Instance.RegisterPad(_blockPos, te.PadName);
        }
    }

    public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return true;
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        Log.Out("[TeleportPads] GetActivationText called at " + _blockPos);
        var te = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityTeleportPad;
        if (te == null)
        {
            Log.Out("[TeleportPads] TileEntity is NULL at " + _blockPos);
            return "Press [action:Activate] to use Teleport Pad";
        }

        if (string.IsNullOrEmpty(te.PadName))
            return Localization.Get("teleportpad_configure");

        return string.Format(Localization.Get("teleportpad_use"), te.PadName);
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world,
        BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        var te = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityTeleportPad;
        bool isNamed = te != null && !string.IsNullOrEmpty(te.PadName);
        bool isInLandClaim = _world.IsMyLandProtectedBlock(_blockPos,
            _world.GetGameManager().GetPersistentLocalPlayer());

        cmds[0].enabled = isNamed;
        cmds[1].enabled = true;
        cmds[2].enabled = isInLandClaim;
        return cmds;
    }

    public override bool OnBlockActivated(string commandName, WorldBase _world, int _cIdx,
        Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            var parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            return OnBlockActivated(commandName, _world, _cIdx, parentPos,
                _world.GetBlock(parentPos), _player);
        }

        var te = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityTeleportPad;
        if (te == null) return false;

        switch (commandName)
        {
            case "teleport":
                OpenTeleportUI(_player, _blockPos);
                return true;

            case "edit":
                OpenNamingUI(_player, _blockPos, te);
                return true;

            case "take":
                TakeBlock(_world, _cIdx, _blockPos, _blockValue, _player);
                return true;

            default:
                return false;
        }
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            var parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            return OnBlockActivated(_world, _cIdx, parentPos, _world.GetBlock(parentPos), _player);
        }

        var te = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityTeleportPad;
        if (te == null) return false;

        if (string.IsNullOrEmpty(te.PadName))
            OpenNamingUI(_player, _blockPos, te);
        else
            OpenTeleportUI(_player, _blockPos);

        return true;
    }

    private void OpenTeleportUI(EntityPlayerLocal _player, Vector3i _blockPos)
    {
        _player.AimingGun = false;
        XUiC_TeleportPadWindow.Open(
            LocalPlayerUI.GetUIForPlayer(_player),
            _blockPos);
    }

    private void OpenNamingUI(EntityPlayerLocal _player, Vector3i _blockPos,
        TileEntityTeleportPad te)
    {
        _player.AimingGun = false;
        XUiC_TeleportPadNaming.Open(
            LocalPlayerUI.GetUIForPlayer(_player),
            _blockPos,
            te.PadName);
    }

    private void TakeBlock(WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityPlayerLocal _player)
    {
        var uiForPlayer = LocalPlayerUI.GetUIForPlayer(_player);
        var itemStack = new ItemStack(_blockValue.ToItemValue(), 1);
        if (!uiForPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiForPlayer.xui.PlayerInventory.DropItem(itemStack);

        _world.SetBlockRPC(_cIdx, _blockPos, BlockValue.Air);
    }
}
