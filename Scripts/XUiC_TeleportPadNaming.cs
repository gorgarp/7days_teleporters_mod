using Platform;
using UnityEngine;

public class XUiC_TeleportPadNaming : XUiController
{
    public static string ID = "";

    private Vector3i padPosition;
    private string currentName = "";
    private XUiC_TextInput textInput;

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;

        textInput = GetChildById("txtPadName") as XUiC_TextInput;

        foreach (var child in GetChildrenByType<XUiC_SimpleButton>())
        {
            string id = child.ViewComponent?.ID ?? "";
            Log.Out("[TeleportPads] Naming found SimpleButton: " + id);
            if (id == "btnSave") child.OnPressed += OnSavePressed;
            if (id == "btnCancel") child.OnPressed += OnCancelPressed;
        }

        if (textInput != null)
            textInput.OnSubmitHandler += OnTextSubmitted;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        if (textInput != null)
        {
            textInput.Text = currentName;
        }
        IsDirty = true;
        RefreshBindings(true);
    }

    public static void Open(LocalPlayerUI _playerUI, Vector3i _blockPos, string _currentName)
    {
        if (string.IsNullOrEmpty(ID)) return;

        var windowGroup = _playerUI.xui.FindWindowGroupByName(ID);
        if (windowGroup == null) return;

        var controller = windowGroup.GetChildByType<XUiC_TeleportPadNaming>();
        if (controller != null)
        {
            controller.padPosition = _blockPos;
            controller.currentName = _currentName ?? "";
        }

        _playerUI.windowManager.Open(ID, true);
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "currentname":
                value = currentName;
                return true;
            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    private void SaveName()
    {
        var newName = textInput != null ? textInput.Text.Trim() : "";
        if (string.IsNullOrEmpty(newName))
        {
            GameManager.ShowTooltip(xui.playerUI.entityPlayer,
                Localization.Get("teleportpad_name_empty"));
            return;
        }

        if (newName.Length > 24)
            newName = newName.Substring(0, 24);

        var world = GameManager.Instance.World;
        var te = world.GetTileEntity(0, padPosition) as TileEntityTeleportPad;
        if (te != null)
        {
            te.SetPadName(newName);
        }

        TeleportPadManager.Instance.RegisterPad(padPosition, newName);

        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(
                NetPackageManager.GetPackage<NetPackageTeleportPadRename>().Setup(padPosition, newName));
        }

        GameManager.ShowTooltip(xui.playerUI.entityPlayer,
            string.Format(Localization.Get("teleportpad_named"), newName));

        xui.playerUI.windowManager.Close(ID);
    }

    private void OnSavePressed(XUiController _sender, int _mouseButton)
    {
        SaveName();
    }

    private void OnCancelPressed(XUiController _sender, int _mouseButton)
    {
        xui.playerUI.windowManager.Close(ID);
    }

    private void OnTextSubmitted(XUiController _sender, string _text)
    {
        SaveName();
    }

    public override void Cleanup()
    {
        base.Cleanup();
        var btnSave = GetChildById("btnSave");
        if (btnSave != null) btnSave.OnPress -= OnSavePressed;
        var btnCancel = GetChildById("btnCancel");
        if (btnCancel != null) btnCancel.OnPress -= OnCancelPressed;
        if (textInput != null)
            textInput.OnSubmitHandler -= OnTextSubmitted;
    }
}
