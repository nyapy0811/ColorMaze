using System;

/// <summary>settings.json으로 저장되는 설정값. GameSettings가 관리한다.</summary>
[Serializable]
public class SettingsData
{
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    public float mouseSensitivity = 0.1f;
}
