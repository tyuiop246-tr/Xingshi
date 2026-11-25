using UnityEngine;

// 封面 → 关卡选择 → 游戏：参考《蛋仔派对》关卡选择页，使用 OnGUI 绘制蓝色关卡按钮与星级
public class TitleScreen : MonoBehaviour
{
    enum Page { Cover, LevelSelect }

    public static TitleScreen Instance { get; private set; }
    public static bool GameStarted { get; private set; }
    public static bool Paused { get; private set; }
    public static int SelectedLevel { get; private set; } = 1;

    Font font;
    Texture2D dimTex, frame;
    Texture2D btnNormal, btnHover, btnActive, btnLocked;
    GUIStyle title, sub, hint, ops;
    GUIStyle selectTitle, starStyle, numberStyle, tagStyle, lockedStyle;
    GUIStyle buttonStyle;
    Page page;
    bool show;

    void Awake()
    {
        Instance = this;
        GameStarted = false;
        SelectedLevel = 1;
        page = Page.Cover;
        show = true;
        font = Resources.Load<Font>("Fonts/DaoFont");
        frame = Resources.Load<Texture2D>("GameFrame");
        dimTex = MakeTex(new Color(0.05f, 0.03f, 0.02f, 0.58f));
        btnNormal = MakeButtonTexture(new Color(0.32f, 0.72f, 1f), new Color(0.08f, 0.28f, 0.68f), new Color(0.04f, 0.15f, 0.45f));
        btnHover = MakeButtonTexture(new Color(0.48f, 0.84f, 1f), new Color(0.12f, 0.38f, 0.78f), new Color(1f, 0.92f, 0.55f));
        btnActive = MakeButtonTexture(new Color(0.18f, 0.52f, 0.92f), new Color(0.05f, 0.2f, 0.58f), new Color(0.03f, 0.12f, 0.38f));
        btnLocked = MakeButtonTexture(new Color(0.62f, 0.64f, 0.68f), new Color(0.32f, 0.34f, 0.38f), new Color(0.2f, 0.21f, 0.24f));
    }

    void Update()
    {
        if (!show) return;

        if (page == Page.LevelSelect && Input.GetKeyDown(KeyCode.Escape))
        {
            page = Page.Cover;
            return;
        }

        if (page == Page.Cover)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                page = Page.LevelSelect;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) StartGame(1);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) StartGame(2);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) StartGame(3);
    }

    public static void SetPaused(bool paused)
    {
        Paused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    public static void ShowLevelSelect()
    {
        SetPaused(false);
        GameStarted = false;
        if (Instance == null) return;
        Instance.show = true;
        Instance.page = Page.LevelSelect;
    }

    public static void StartGame(int level)
    {
        if (Instance == null) return;

        SetPaused(false);
        SelectedLevel = Mathf.Clamp(level, 1, 3);
        LevelRuntimeTuner.Apply(SelectedLevel);
        if (PlayerController3D.Instance != null) PlayerController3D.Instance.ResetCharacter();
        GoalQing.ResetAll();
        if (HUDController.Instance != null) HUDController.Instance.ResetForNewLevel();

        Instance.show = false;
        GameStarted = true;
    }

    void OnGUI()
    {
        if (!show) return;
        float w = Screen.width, h = Screen.height;
        InitStyles(h);

        GUI.DrawTexture(new Rect(0, 0, w, h), dimTex);

        if (page == Page.Cover) DrawCover(w, h);
        else DrawLevelSelect(w, h);

        if (frame != null) GUI.DrawTexture(new Rect(0, 0, w, h), frame);
    }

    void DrawCover(float w, float h)
    {
        // 布局避开边框：顶部边框约占 8.5%，底部火焰约占 8.5%
        LabelOutline(new Rect(0, h * 0.13f, w, h * 0.13f), "醒狮：高桩采青", title);
        LabelOutline(new Rect(0, h * 0.26f, w, h * 0.07f), "非遗献礼 · 3D 平台跳跃", sub);
        LabelOutline(new Rect(0, h * 0.45f, w, h * 0.09f), "按 空格 进入关卡选择", hint);
        LabelOutline(new Rect(0, h * 0.80f, w, h * 0.06f),
            "WASD / 方向键  移动      空格  跳跃      R  重新开始      ESC  设置", ops);
    }

    void DrawLevelSelect(float w, float h)
    {
        LabelOutline(new Rect(0, h * 0.12f, w, h * 0.1f), "SELECT LEVEL", selectTitle);
        LabelOutline(new Rect(0, h * 0.21f, w, h * 0.055f), "选择关卡", sub);

        float size = Mathf.Min(w * 0.15f, h * 0.22f);
        float gap = Mathf.Min(w * 0.035f, 42f);
        float total = size * 4f + gap * 3f;
        float startX = (w - total) * 0.5f;
        float startY = h * 0.32f;

        DrawLevelButton(new Rect(startX, startY, size, size), 1);
        DrawLevelButton(new Rect(startX + (size + gap), startY, size, size), 2);
        DrawLevelButton(new Rect(startX + (size + gap) * 2f, startY, size, size), 3);
        DrawLevelButton(new Rect(startX + (size + gap) * 3f, startY, size, size), 4, true);

        LabelOutline(new Rect(w * 0.12f, h * 0.64f, w * 0.76f, h * 0.06f),
            "关卡 2 / 3：桩更窄、鼓更快、横向跨度更大", sub);
        LabelOutline(new Rect(w * 0.12f, h * 0.73f, w * 0.76f, h * 0.06f),
            "点击关卡或按 1 / 2 / 3 开始      ESC 返回封面", ops);
    }

    void DrawLevelButton(Rect rect, int level, bool locked = false)
    {
        if (locked)
        {
            GUI.DrawTexture(rect, btnLocked);
            LabelOutline(new Rect(rect.x, rect.y + rect.height * 0.28f, rect.width, rect.height * 0.32f), "4", numberStyle);
            LabelOutline(new Rect(rect.x, rect.y + rect.height * 0.64f, rect.width, rect.height * 0.2f), "开发中", lockedStyle);
            return;
        }

        if (GUI.Button(rect, GUIContent.none, buttonStyle)) StartGame(level);

        string stars = level == 1 ? "★" : (level == 2 ? "★★" : "★★★");
        LabelOutline(new Rect(rect.x, rect.y + rect.height * 0.04f, rect.width, rect.height * 0.18f), stars, starStyle);
        LabelOutline(new Rect(rect.x, rect.y + rect.height * 0.26f, rect.width, rect.height * 0.4f), level.ToString(), numberStyle);
        LabelOutline(new Rect(rect.x, rect.y + rect.height * 0.7f, rect.width, rect.height * 0.18f),
            LevelRuntimeTuner.GetName(level), tagStyle);
    }

    void InitStyles(float h)
    {
        title = Make(h * 0.075f, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.5f));
        sub = Make(h * 0.04f, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.7f));
        hint = Make(h * 0.055f, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.35f));
        ops = Make(h * 0.03f, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.82f));

        selectTitle = Make(h * 0.085f, TextAnchor.MiddleCenter, new Color(0.45f, 0.8f, 1f));
        starStyle = Make(h * 0.026f, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.25f));
        numberStyle = Make(h * 0.07f, TextAnchor.MiddleCenter, Color.white);
        tagStyle = Make(h * 0.028f, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.72f));
        lockedStyle = Make(h * 0.035f, TextAnchor.MiddleCenter, new Color(0.92f, 0.92f, 0.92f));

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = btnNormal;
            buttonStyle.hover.background = btnHover;
            buttonStyle.active.background = btnActive;
            buttonStyle.focused.background = btnNormal;
            buttonStyle.onNormal.background = btnNormal;
            buttonStyle.onHover.background = btnHover;
            buttonStyle.onActive.background = btnActive;
            buttonStyle.onFocused.background = btnNormal;
            buttonStyle.border = new RectOffset(12, 12, 12, 12);
            buttonStyle.padding = new RectOffset(0, 0, 0, 0);
            buttonStyle.margin = new RectOffset(0, 0, 0, 0);
        }
    }

    GUIStyle Make(float size, TextAnchor anchor, Color color)
    {
        var s = new GUIStyle(GUI.skin.label);
        if (font != null) s.font = font;
        s.fontSize = Mathf.Max(12, Mathf.RoundToInt(size));
        s.alignment = anchor;
        // 统一所有状态的颜色，去掉鼠标悬停时 skin 默认的白色
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

    void LabelOutline(Rect r, string text, GUIStyle style)
    {
        GUI.Label(r, text, style);
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    static Texture2D MakeButtonTexture(Color top, Color bottom, Color border)
    {
        const int size = 64;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size - 0.5f;
                float ny = (y + 0.5f) / size - 0.5f;
                float dx = Mathf.Max(Mathf.Abs(nx) - 0.4f, 0f);
                float dy = Mathf.Max(Mathf.Abs(ny) - 0.4f, 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > 0.1f)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                Color c = Color.Lerp(bottom, top, (y + 0.5f) / size);
                pixels[y * size + x] = dist > 0.065f ? border : c;
            }
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }
}
