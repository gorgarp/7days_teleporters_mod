using System;
using UnityEngine;

public class XUiC_TeleportPadEntry : XUiController
{
    public event Action<Vector3i> TeleportRequested;

    private string padName = "";
    private Vector3i padPosition = Vector3i.zero;
    private string distanceText = "";

    public override void Init()
    {
        base.Init();
        OnPress += OnEntryClicked;

        var btn = GetChildById("btnGo");
        if (btn != null)
        {
            btn.OnPress += OnEntryClicked;
            Log.Out("[TeleportPads] Found btnGo in entry");
        }
        else
        {
            Log.Out("[TeleportPads] btnGo NOT found, using row click only");
        }

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child is XUiC_SimpleButton simpleBtn)
            {
                simpleBtn.OnPressed += OnEntryClicked;
                Log.Out("[TeleportPads] Wired XUiC_SimpleButton at index " + i);
            }
        }
    }

    public void SetData(string name, Vector3i position)
    {
        padName = name ?? "";
        padPosition = position;

        var player = xui?.playerUI?.entityPlayer;
        if (player != null && position != Vector3i.zero)
        {
            float dist = Vector3.Distance(player.position, position.ToVector3());
            distanceText = $"{dist:F0}m";
        }
        else
        {
            distanceText = "";
        }

        IsDirty = true;
        RefreshBindings(true);
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "padname":
                value = padName;
                return true;
            case "distance":
                value = distanceText;
                return true;
            case "coords":
                if (padPosition == Vector3i.zero)
                    value = "";
                else
                    value = $"({padPosition.x}, {padPosition.y}, {padPosition.z})";
                return true;
            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    private void OnEntryClicked(XUiController _sender, int _mouseButton)
    {
        Log.Out("[TeleportPads] Entry clicked: " + padName + " at " + padPosition);
        if (padPosition != Vector3i.zero)
            TeleportRequested?.Invoke(padPosition);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OnPress -= OnEntryClicked;
    }
}
