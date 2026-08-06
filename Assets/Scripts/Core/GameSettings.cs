using Framework.Core;

/// <summary>
/// BGM/SFX 음량, 마우스 감도를 settings.json 하나로 관리하는 static 헬퍼.
/// SaveManager의 범용 JSON API(SaveJson/LoadJson) 위에서 동작하며, 별도 씬 오브젝트가 필요 없다.
/// 세 값이 한 파일에 같이 저장되므로, 하나만 바뀌어도 항상 Current 전체를 다시 저장해서
/// 다른 필드가 덮어써지지 않게 한다.
/// </summary>
public static class GameSettings
{
    const string FileName = "settings.json";

    static SettingsData current;

    public static SettingsData Current
    {
        get
        {
            if (current == null) current = SaveManager.Instance.LoadJson<SettingsData>(FileName);
            return current;
        }
    }

    public static void Save() => SaveManager.Instance.SaveJson(FileName, Current);

    public static bool HasSave() => SaveManager.Instance.HasJson(FileName);
}
