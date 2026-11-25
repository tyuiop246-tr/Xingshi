using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 醒狮·高桩采青 —— 白盒关卡一键生成器
// 菜单：Tools > 醒狮白盒 > 生成关卡（一键重建）
// 设计：起(教学) → 承(静态桩) → 转(移动鼓) → 合(采青高台)，难度平滑上升
public static class WhiteboxBuilder
{
    const string Root = "Assets/Whitebox";
    const string MatDir = Root + "/Materials";
    const string PrefabDir = Root + "/Prefabs";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static Material gridMat, pileMat, drumMat, goalMat, playerMat, accentMat;
    static Material cpInactiveMat, cpActiveMat;
    static GameObject pilePrefab, drumPrefab;

    [MenuItem("Tools/醒狮白盒/生成关卡（一键重建）")]
    public static void Build()
    {
        EnsureFolders();
        CreateMaterials();

        // 新建空场景，避免重复叠加
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject env = new GameObject("=== 环境 ENV ===");
        GameObject start = new GameObject("=== 起·教学区 START ===");
        GameObject build = new GameObject("=== 承·静态梅花桩 BUILD ===");
        GameObject turn = new GameObject("=== 转·移动鼓阵 TURN ===");
        GameObject finish = new GameObject("=== 合·采青高台 FINISH ===");

        FixFrameImport(); // 先把醒狮图导入为 Sprite，否则 BuildPlayer 加载不到
        BuildLighting(env.transform);
        BuildGround(start.transform);
        BuildPiles(build.transform);
        BuildDrums(turn.transform);
        BuildCheckpoints(env.transform);
        BuildFinish(finish.transform);
        BuildPlayer(env.transform);
        BuildHUD(env.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[醒狮白盒] 关卡生成完成，已保存到 " + ScenePath);
    }

    // 边框 PNG 依赖 alpha 通道；醒狮贴图需导入为 Sprite 才能被 SpriteRenderer 使用
    static void FixFrameImport()
    {
        string frame = "Assets/Resources/GameFrame.png";
        TextureImporter ti = AssetImporter.GetAtPath(frame) as TextureImporter;
        if (ti != null)
        {
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }

        // 三面醒狮贴图都需导入为 Sprite
        foreach (string lion in new[] {
            "Assets/Resources/LionPixel.png",
            "Assets/Resources/LionPixelSide.png",
            "Assets/Resources/LionPixelBack.png" })
        {
            TextureImporter li = AssetImporter.GetAtPath(lion) as TextureImporter;
            if (li == null) continue;
            li.textureType = TextureImporterType.Sprite;
            li.spriteImportMode = SpriteImportMode.Single;
            li.filterMode = FilterMode.Point; // 像素风硬边
            li.mipmapEnabled = false;
            li.alphaIsTransparency = true;
            li.SaveAndReimport();
        }
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "Whitebox");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder(Root, "Materials");
        if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder(Root, "Prefabs");
    }

    static Texture2D MakeGridTexture()
    {
        string path = MatDir + "/GridPixel.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) return tex;

        const int size = 64;
        tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];
        // 像素风：暗红底 + 金色网格线，呼应醒狮的红金配色
        Color gold = new Color(0.95f, 0.78f, 0.28f);
        Color red = new Color(0.62f, 0.13f, 0.12f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool gx = x % 16 == 0 || x % 16 == 1;
                bool gy = y % 16 == 0 || y % 16 == 1;
                pixels[y * size + x] = (gx || gy) ? gold : red;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point; // 像素风：硬边不模糊
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();
        return tex;
    }

    static Material MakeMat(string name, Color color, Texture2D texture = null)
    {
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        if (texture != null) { m.mainTexture = texture; m.mainTextureScale = new Vector2(2, 2); }
        m.SetFloat("_Glossiness", 0.15f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CreateMaterials()
    {
        Texture2D grid = MakeGridTexture();
        // 平台：红底金线像素网格；梅花桩：醒狮红 + 金线；鼓：青绿；青：发光金
        gridMat = MakeMat("GridMat", Color.white, grid);
        pileMat = MakeMat("PileMat", new Color(0.85f, 0.30f, 0.25f), grid);
        drumMat = MakeMat("DrumMat", new Color(0.16f, 0.62f, 0.48f));
        goalMat = MakeMat("QingMat", new Color(1f, 0.88f, 0.25f));
        goalMat.EnableKeyword("_EMISSION");
        goalMat.SetColor("_EmissionColor", new Color(1.4f, 1.0f, 0.12f));
        playerMat = MakeMat("PlayerMat", new Color(0.92f, 0.35f, 0.12f));
        accentMat = MakeMat("AccentMat", new Color(0.13f, 0.72f, 0.42f));
        // 复活点：半透明青蓝光柱，不遮挡视线
        cpInactiveMat = MakeGlowMat("CP_InactiveMat", new Color(0.15f, 0.35f, 0.6f, 0.25f), 0.4f);
        cpActiveMat = MakeGlowMat("CP_ActiveMat", new Color(0.35f, 0.85f, 1f, 0.45f), 1.6f);
    }

    // 半透明发光材质：Standard 渲染模式设为 Transparent，关闭 ZWrite 让光柱不遮挡后面的物体
    static Material MakeGlowMat(string name, Color color, float emission)
    {
        Material m = MakeMat(name, color);
        m.SetFloat("_Mode", 3); // Transparent
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = 3000;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * emission);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Cylinder(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void BuildLighting(Transform parent)
    {
        GameObject sun = new GameObject("Directional Light");
        sun.transform.SetParent(parent);
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        Light l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.15f;
        l.shadows = LightShadows.Soft;

        GameObject spot = new GameObject("Spotlight - Goal");
        spot.transform.SetParent(parent);
        spot.transform.position = new Vector3(0f, 18f, 92f);
        spot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Light s = spot.AddComponent<Light>();
        s.type = LightType.Spot;
        s.intensity = 3.5f;
        s.range = 40f;
        s.spotAngle = 45f;
        s.color = new Color(1f, 0.9f, 0.55f);
        s.shadows = LightShadows.Soft;
    }

    // 起：宽阔平地 + 低矮台阶 + 视觉锚点
    static void BuildGround(Transform parent)
    {
        Cube("Start_Plaza", new Vector3(0f, -0.5f, 4f), new Vector3(14f, 1f, 16f), gridMat, parent);
        Cube("Step_01", new Vector3(0f, 0.2f, 10f), new Vector3(5f, 0.4f, 2.5f), pileMat, parent);
        Cube("Step_02", new Vector3(1f, 0.5f, 13f), new Vector3(4f, 1f, 2.5f), pileMat, parent);
        // 远处高台作为视觉锚点，建立目标感
        Cube("Visual_Anchor", new Vector3(0f, 5f, 92f), new Vector3(1f, 10f, 1f), accentMat, parent, false);
    }

    // 承：静态梅花桩（教学难度）。桩子更粗、横向偏移更小、高度差更缓，先让玩家熟悉连跳
    static void BuildPiles(Transform parent)
    {
        // 创建 Prefab（JD 核心要求）：先做一个标准桩，保存为 Prefab 再批量实例化
        GameObject template = Cylinder("Pile_Template", Vector3.zero, new Vector3(1.6f, 1f, 1.6f), pileMat, null);
        pilePrefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabDir + "/MeihuaPile.prefab");
        Object.DestroyImmediate(template);

        // ID, X, Y(顶面高度), Z, 直径 —— 教学难度：加密桩子（间距约2.7m）、桩更粗、落差更缓
        float[][] data = {
            new float[]{1,  0f,    1.0f, 17f, 2.4f},
            new float[]{2, -0.6f,  1.2f, 19.7f, 2.4f},
            new float[]{3,  0.6f,  1.4f, 22.4f, 2.4f},
            new float[]{4, -0.5f,  1.6f, 25.1f, 2.3f},
            new float[]{5,  0.7f,  1.8f, 27.8f, 2.3f},
            new float[]{6, -0.7f,  2.0f, 30.5f, 2.2f},
            new float[]{7,  0.6f,  2.2f, 33.2f, 2.2f},
            new float[]{8, -0.5f,  2.4f, 35.9f, 2.1f},
            new float[]{9,  0.7f,  2.6f, 38.6f, 2.1f},
            new float[]{10,-0.6f,  2.8f, 41.3f, 2.0f},
            new float[]{11, 0.5f,  3.0f, 44f, 2.0f},
            new float[]{12,-0.5f,  3.2f, 46.7f, 1.9f},
            new float[]{13, 0.6f,  3.4f, 49.4f, 1.9f},
            new float[]{14, 0f,    3.6f, 52f, 1.9f},
        };

        foreach (float[] d in data)
        {
            GameObject pile = (GameObject)PrefabUtility.InstantiatePrefab(pilePrefab, parent);
            pile.name = "Pile_" + d[0].ToString("00");
            // Cylinder 默认高 2，缩放 Y = 顶面高度/2，位置 Y = 顶面高度/2
            float top = d[2];
            float diameter = d[4];
            pile.transform.position = new Vector3(d[1], top * 0.5f, d[3]);
            pile.transform.localScale = new Vector3(diameter, top * 0.5f, diameter);
        }

        // 隐藏捷径：高风险高收益，从桩14 斜跳到终点高台，跳过移动鼓阵
        GameObject shortcut = Cylinder("Hidden_Shortcut_Pile", new Vector3(4.5f, 2.75f, 60f),
            new Vector3(0.9f, 2.75f, 0.9f), accentMat, parent);
        shortcut.name = "[捷径] Hidden_Shortcut";
    }

    // 转：移动鼓阵。Prefab + Mathf.PingPong 脚本，面试时演示"代码驱动机关"
    static void BuildDrums(Transform parent)
    {
        GameObject template = Cylinder("Drum_Template", Vector3.zero, new Vector3(3f, 0.5f, 3f), drumMat, null);
        MovingObstacle mo = template.AddComponent<MovingObstacle>();
        mo.axis = MovingObstacle.MoveAxis.X;
        mo.amplitude = 3.5f;
        mo.speed = 2.2f;
        drumPrefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabDir + "/MovingDrum.prefab");
        Object.DestroyImmediate(template);

        float[][] data = {
            new float[]{1, 0f, 4.5f, 60f, 3.0f, 2.0f, 0f},
            new float[]{2, 0f, 5.0f, 68f, 3.0f, 2.6f, 1.7f},
            new float[]{3, 0f, 5.8f, 76f, 3.0f, 3.0f, 3.4f},
        };
        foreach (float[] d in data)
        {
            GameObject drum = (GameObject)PrefabUtility.InstantiatePrefab(drumPrefab, parent);
            drum.name = "Drum_" + d[0].ToString("00");
            drum.transform.position = new Vector3(d[1], d[2], d[3]);
            MovingObstacle m = drum.GetComponent<MovingObstacle>();
            m.amplitude = d[4];
            m.speed = d[5];
            m.phase = d[6];
        }

        // 鼓阵之间的固定落脚台，给玩家喘息点
        Cube("Rest_Pad_01", new Vector3(0f, 4f, 64f), new Vector3(3f, 0.6f, 2f), pileMat, parent);
        Cube("Rest_Pad_02", new Vector3(0f, 4.8f, 72f), new Vector3(3f, 0.6f, 2f), pileMat, parent);
        Cube("Rest_Pad_03", new Vector3(0f, 5.6f, 80f), new Vector3(3f, 0.6f, 2f), pileMat, parent);
    }

    // 合：最高处高台 + 发光的「青」+ 聚光灯引导
    // 复活点：每过一段（约 10 个桩/物件）放一个，浅蓝光圈 + 光柱，走过激活
    static void BuildCheckpoints(Transform parent)
    {
        // index, X, 平台顶面 Y, Z
        float[][] data = {
            new float[]{0, 0f, 0.6f, 11f},   // 教学广场末端
            new float[]{1, 0f, 4.2f, 56f},   // 第14桩之后（承→转衔接，高度就近衔接）
            new float[]{2, 0f, 6.2f, 81f},   // 鼓阵末端喘息台之后
        };

        GameObject root = new GameObject("=== 复活点 CHECKPOINTS ===");
        root.transform.SetParent(parent);

        for (int i = 0; i < data.Length; i++)
        {
            int idx = (int)data[i][0];
            float x = data[i][1], top = data[i][2], z = data[i][3];

            GameObject cp = new GameObject("Checkpoint_" + idx.ToString("00"));
            cp.transform.SetParent(root.transform);
            cp.transform.position = new Vector3(x, top, z);

            // 复活点小平台（保证站得住）
            Cube("CP_Pad", new Vector3(x, top - 0.3f, z), new Vector3(4f, 0.6f, 4f), gridMat, cp.transform);

            // 光圈：扁平圆柱铺在平台上
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "CP_Ring";
            ring.transform.SetParent(cp.transform);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.transform.localScale = new Vector3(1.8f, 0.04f, 1.8f);
            ring.GetComponent<Renderer>().sharedMaterial = cpInactiveMat;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            // 光柱：细长圆柱向上发光
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "CP_Beam";
            beam.transform.SetParent(cp.transform);
            beam.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            beam.transform.localScale = new Vector3(0.22f, 1.2f, 0.22f);
            beam.GetComponent<Renderer>().sharedMaterial = cpInactiveMat;
            Object.DestroyImmediate(beam.GetComponent<Collider>());

            // 触发区
            SphereCollider tc = cp.AddComponent<SphereCollider>();
            tc.radius = 2f;
            tc.isTrigger = true;

            Checkpoint script = cp.AddComponent<Checkpoint>();
            script.index = idx;
            script.ring = ring;
            script.beam = beam;
            script.inactiveMat = cpInactiveMat;
            script.activeMat = cpActiveMat;
        }
    }

    static void BuildFinish(Transform parent)
    {
        Cube("Finish_Platform", new Vector3(0f, 6f, 90f), new Vector3(10f, 1f, 10f), gridMat, parent);
        // 高台边缘装饰，强化终点识别
        Cube("Finish_Edge_L", new Vector3(-4.7f, 6.6f, 90f), new Vector3(0.4f, 1.2f, 10f), accentMat, parent, false);
        Cube("Finish_Edge_R", new Vector3(4.7f, 6.6f, 90f), new Vector3(0.4f, 1.2f, 10f), accentMat, parent, false);

        GameObject qing = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        qing.name = "Qing_Goal";
        qing.transform.SetParent(parent);
        qing.transform.position = new Vector3(0f, 8.2f, 92f);
        qing.transform.localScale = Vector3.one * 1.4f;
        qing.GetComponent<Renderer>().sharedMaterial = goalMat;
        SphereCollider sc = qing.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        // 触发区放大，避免白盒阶段很难碰到
        GameObject trigger = new GameObject("Qing_Trigger");
        trigger.transform.SetParent(parent);
        trigger.transform.position = qing.transform.position;
        SphereCollider tc = trigger.AddComponent<SphereCollider>();
        tc.radius = 2.2f;
        tc.isTrigger = true;
        GoalQing gq = trigger.AddComponent<GoalQing>();
        gq.qingTransform = qing.transform;
    }

    static void BuildPlayer(Transform parent)
    {
        // 胶囊只做碰撞，隐藏渲染，避免盖住醒狮面片
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        p.name = "Player_Lion";
        p.transform.SetParent(parent);
        p.transform.position = new Vector3(0f, 1.15f, 0f);
        p.GetComponent<Renderer>().enabled = false;
        CharacterController cc = p.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0f, 0f);
        p.AddComponent<PlayerController3D>();

        // 醒狮像素面片：空物体 + SpriteRenderer（标准 2D Sprite 方案，避免 MeshRenderer 冲突）
        GameObject sprite = new GameObject("Lion_Sprite");
        sprite.transform.SetParent(p.transform);
        sprite.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        sprite.transform.localRotation = Quaternion.identity;
        sprite.transform.localScale = new Vector3(1.4f, 1.4f, 1f); // 缩小醒狮
        SpriteRenderer sr = sprite.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/LionPixel.png");
        sr.sortingOrder = 10;
        BillboardSprite bs = sprite.AddComponent<BillboardSprite>();
        bs.spriteFront = sr.sprite;
        bs.spriteSide = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/LionPixelSide.png");
        bs.spriteBack = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/LionPixelBack.png");

        GameObject cam = new GameObject("Main Camera");
        cam.transform.SetParent(parent);
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 5f, -9f);
        Camera camera = cam.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cam.AddComponent<AudioListener>();
        FollowCamera fc = cam.AddComponent<FollowCamera>();
        fc.target = p.transform;
    }

    static void BuildHUD(Transform parent)
    {
        GameObject hud = new GameObject("HUD");
        hud.transform.SetParent(parent);
        hud.AddComponent<HUDController>();
        GameObject title = new GameObject("TitleScreen");
        title.transform.SetParent(parent);
        title.AddComponent<TitleScreen>();
    }
}
