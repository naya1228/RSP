using Godot;

public partial class SettingsPopup : CanvasLayer
{
    private Button _closeButton;
    private HSlider _masterSlider;
    private HSlider _sfxSlider;

    public override void _Ready()
    {
        _closeButton = GetNode<Button>("Panel/Layout/TitleBar/CloseButton");
        _masterSlider = GetNode<HSlider>("Panel/Layout/ContentArea/AudioContent/MasterRow/MasterSlider");
        _sfxSlider = GetNode<HSlider>("Panel/Layout/ContentArea/AudioContent/SfxRow/SfxSlider");

        _closeButton.Pressed += Close;
        _masterSlider.ValueChanged += OnMasterVolumeChanged;
        _sfxSlider.ValueChanged += OnSfxVolumeChanged;
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            Close();
    }

    private void OnMasterVolumeChanged(double value)
    {
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb((float)value));
    }

    private void OnSfxVolumeChanged(double value)
    {
        // TODO: SFX 버스 분리 후 연결
        GD.Print($"[Settings] SFX 볼륨: {value:F2}");
    }

    public void Close()
    {
        QueueFree();
    }
}
