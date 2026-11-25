using UnityEngine;

// 白盒 HUD：浅金色字 + 深色描边（浅色天空下也清晰）+ 醒狮边框叠加
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    string tip = "";
    float tipTimer;
    bool won;
    bool settingsOpen;
    float winDistance;
    int winDeaths;

    GUIStyle title, sub, tipStyle, settingsButton;
    Font font;
    Texture2D frame;
    Texture2D dimTex;
    Texture2D barTex;
    Texture2D panelTex;
    Texture2D settingsNormal, settingsHover, settingsActive;

    static readonly Color Gold = new Color(1f, 0.88f, 0.55f);   // 浅金色
    static readonly Color Outline = new Color(0.24f, 0.08f, 0.04f, 0.95f); // 深棕描边

    void Awake()
    {
        Instance = this;
        font = Resources.Load<Font>("Fonts/DaoFont");
        frame = Resources.Load<Texture2D>("GameFrame");
        dimTex = MakeTex(new Color(0f, 0f, 0f, 0.68f));
        barTex = MakeTex(new Color(0.08f, 0.04f, 0.02f, 0.72f));
        panelTex = MakeTex(new Color(0.04f, 0.06f, 0.14f, 0.92f));
        settingsNormal = MakeTex(new Color(0.12f, 0.42f, 0.82f, 0.95f));
        settingsHover = MakeTex(new Color(0.24f, 0.6f, 0.96f, 0.95f));
        settingsActive = MakeTex(new Color(0.08f, 0.28f, 0.62f, 0.95f));
        if (frame == null) Debug.LogWarning("[醒狮] 未找到边框图 Resources/GameFrame.png");
    }

    void Update()
    {
        if (!TitleScreen.GameStarted) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsOpen) CloseSettings();
            else OpenSettings();
            return;
        }

        if (settingsOpen) return;
        if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
    }

    void InitStyles(float h)
    {
        title = Make(h * 0.06f, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.65f));
        sub = Make(h * 0.032f, TextAnchor.MiddleLeft, Gold);
        tipStyle = Make(h * 0.035f, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.6f));

        if (settingsButton == null)
        {
            settingsButton = new GUIStyle(GUI.skin.button);
            if (font != null) settingsButton.font = font;
            settingsButton.fontSize = Mathf.Max(14, Mathf.RoundToInt(h * 0.03f));
            settingsButton.alignment = TextAnchor.MiddleCenter;
            settingsButton.normal.background = settingsNormal;
            settingsButton.hover.background = settingsHover;
            settingsButton.active.background = settingsActive;
            settingsButton.focused.background = settingsNormal;
            settingsButton.onNormal.background = settingsNormal;
            settingsButton.onHover.background = settingsHover;
            settingsButton.onActive.background = settingsActive;
            settingsButton.onFocused.background = settingsNormal;
            settingsButton.normal.textColor = Color.white;
            settingsButton.hover.textColor = Color.white;
            settingsButton.active.textColor = Color.white;
            settingsButton.focused.textColor = Color.white;
            settingsButton.onNormal.textColor = Color.white;
            settingsButton.onHover.textColor = Color.white;
            settingsButton.onActive.textColor = Color.white;
            settingsButton.onFocused.textColor = Color.white;
        }
    }

    GUIStyle Make(float size, TextAnchor anchor, Color color)
    {
        var s = new GUIStyle(GUI.skin.label);
        if (font != null) s.font = font;
        s.fontSize = Mathf.Max(12, Mathf.RoundToInt(size));
        s.alignment = anchor;
        // 统一所有状态颜色，避免鼠标悬停显示白色
        s.normal.textColor = color;
        s.hover.textColor = color;
        s.active.textColor = color;
        s.focused.textColor = color;
        s.onNormal.textColor = color;
        s.onHover.textColor = color;
        s.onActive.textColor = color;
        s.onFocused.textColor = color;
        s.wordWrap = true;
        return s;
    }

    // 纯色文字 + 轻描边，不加灰色底板
    void LabelWithOutline(Rect r, string text, GUIStyle style)
    {
        Color c = style.normal.textColor;
        style.normal.textColor = Outline;
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style);
        style.normal.textColor = c;
        GUI.Label(r, text, style);
    }

    void OnGUI()
    {
        float w = Screen.width, h = Screen.height;

        // 封面显示时不画 HUD，避免文字与封面重叠
        if (TitleScreen.Instance != null && !TitleScreen.GameStarted) return;

        InitStyles(h);

        float padX = w * 0.06f;
        int level = TitleScreen.SelectedLevel;
        LabelWithOutline(new Rect(padX, h * 0.16f, w * 0.72f, 36),
            "关卡 " + level + " · " + LevelRuntimeTuner.GetName(level) + "  WASD/方向键 移动  空格 跳跃  R 重开  ESC 设置", sub);

        PlayerController3D p = PlayerController3D.Instance;
        if (p != null)
            LabelWithOutline(new Rect(padX, h * 0.215f, w * 0.5f, 36),
                "进度 " + Mathf.FloorToInt(p.Progress) + " m   跌落 " + p.DeathCount + " 次", sub);

        if (tipTimer > 0f)
        {
            if (!settingsOpen) tipTimer -= Time.unscaledDeltaTime;
            // 下移到进度行下方（16%/21.5%），避免与其他文字重叠
            LabelWithOutline(new Rect(w * 0.2f, h * 0.28f, w * 0.6f, 50f), tip, tipStyle);
        }

        if (won && !settingsOpen)
        {
            GUI.DrawTexture(new Rect(0, 0, w, h), dimTex);
            LabelWithOutline(new Rect(w * 0.1f, h * 0.32f, w * 0.8f, h * 0.12f), "关卡 " + level + " 采 青 成 功", title);
            LabelWithOutline(new Rect(w * 0.1f, h * 0.46f, w * 0.8f, h * 0.12f),
                "通关进度 " + Mathf.FloorToInt(winDistance) + " m · 跌落 " + winDeaths + " 次\n按 R 重新开始  ·  ESC 设置", sub);
        }

        if (!settingsOpen)
        {
            float setW = Mathf.Min(w * 0.1f, 110f);
            float setH = Mathf.Min(h * 0.05f, 34f);
            Rect setRect = new Rect(w - padX - setW, h * 0.16f, setW, setH);
            if (GUI.Button(setRect, "设置", settingsButton)) OpenSettings();
        }
        else
        {
            DrawSettings(w, h);
        }

        if (frame != null) GUI.DrawTexture(new Rect(0, 0, w, h), frame);
    }

    void DrawSettings(float w, float h)
    {
        GUI.DrawTexture(new Rect(0, 0, w, h), dimTex);

        float panelW = Mathf.Min(w * 0.5f, 540f);
        float panelH = Mathf.Min(h * 0.56f, 400f);
        Rect panel = new Rect((w - panelW) * 0.5f, (h - panelH) * 0.5f, panelW, panelH);
        GUI.DrawTexture(panel, panelTex);

        LabelWithOutline(new Rect(panel.x, panel.y + panel.height * 0.08f, panel.width, panel.height * 0.18f), "设置 / 暂停", title);
        LabelWithOutline(new Rect(panel.x, panel.y + panel.height * 0.24f, panel.width, panel.height * 0.12f),
            "关卡 " + TitleScreen.SelectedLevel + " · " + LevelRuntimeTuner.GetName(TitleScreen.SelectedLevel), sub);

        float btnW = panelW * 0.72f;
        float btnH = Mathf.Min(panelH * 0.14f, 48f);
        float btnX = panel.x + (panelW - btnW) * 0.5f;
        float btnY = panel.y + panelH * 0.4f;
        float gap = btnH * 0.28f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "继续游戏", settingsButton)) CloseSettings();
        if (GUI.Button(new Rect(btnX, btnY + btnH + gap, btnW, btnH), "重新开始", settingsButton)) RestartLevel();
        if (GUI.Button(new Rect(btnX, btnY + (btnH + gap) * 2f, btnW, btnH), "退出关卡（返回关卡选择）", settingsButton)) ExitToLevelSelect();
    }

    void OpenSettings()
    {
        settingsOpen = true;
        TitleScreen.SetPaused(true);
    }

    void CloseSettings()
    {
        settingsOpen = false;
        TitleScreen.SetPaused(false);
    }

    void ExitToLevelSelect()
    {
        CloseSettings();
        TitleScreen.ShowLevelSelect();
    }

    void RestartLevel()
    {
        CloseSettings();
        if (PlayerController3D.Instance != null) PlayerController3D.Instance.ResetCharacter();
        GoalQing.ResetAll();
        won = false;
        tip = "";
        tipTimer = 0f;
    }

    public void ResetForNewLevel()
    {
        settingsOpen = false;
        TitleScreen.SetPaused(false);
        won = false;
        tip = "";
        tipTimer = 0f;
    }

    // 边框图中间是白色占位（非透明），所以只取四边的纹样绘制，中间完全留空
    void DrawFrame(float w, float h)
    {
        const float top = 0.085f;    // 顶部纹样占图高比例
        const float bottom = 0.085f; // 底部火焰/铜钱
        const float side = 0.045f;   // 左右立柱

        // DrawTextureWithTexCoords 的 texRect 原点在左下角
        GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h * top), frame, new Rect(0, 1 - top, 1, top));
        GUI.DrawTextureWithTexCoords(new Rect(0, h * (1 - bottom), w, h * bottom), frame, new Rect(0, 0, 1, bottom));
        GUI.DrawTextureWithTexCoords(new Rect(0, h * top, w * side, h * (1 - top - bottom)), frame, new Rect(0, bottom, side, 1 - top - bottom));
        GUI.DrawTextureWithTexCoords(new Rect(w * (1 - side), h * top, w * side, h * (1 - top - bottom)), frame,
            new Rect(1 - side, bottom, side, 1 - top - bottom));
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    public void ShowTip(string message)
    {
        tip = message;
        tipTimer = 2f;
    }

    public void ShowWin(float distance, int deaths)
    {
        won = true;
        winDistance = distance;
        winDeaths = deaths;
    }
}
