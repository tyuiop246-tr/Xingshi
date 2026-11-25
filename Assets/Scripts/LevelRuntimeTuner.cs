using System.Collections.Generic;
using UnityEngine;

// 关卡难度运行时微调：同一套白盒场景根据所选关卡还原/调整桩子与机关参数
public static class LevelRuntimeTuner
{
    struct PadState
    {
        public Transform transform;
        public Vector3 position;
        public Vector3 scale;
    }

    struct ObstacleState
    {
        public MovingObstacle obstacle;
        public float amplitude;
        public float speed;
    }

    static readonly List<PadState> pads = new List<PadState>();
    static readonly List<ObstacleState> obstacles = new List<ObstacleState>();
    static bool captured;

    public static string GetName(int level)
    {
        switch (level)
        {
            case 2: return "进阶";
            case 3: return "挑战";
            default: return "入门";
        }
    }

    public static void Apply(int level)
    {
        CaptureIfNeeded();
        RestoreAll();

        float padScale = level == 3 ? 0.72f : (level == 2 ? 0.86f : 1f);
        float lateral = level == 3 ? 1.55f : (level == 2 ? 1.25f : 1f);
        float obstacleMul = level == 3 ? 1.45f : (level == 2 ? 1.2f : 1f);

        foreach (PadState pad in pads)
        {
            Vector3 scale = pad.scale;
            scale.x *= padScale;
            scale.z *= padScale;
            pad.transform.localScale = scale;

            Vector3 position = pad.position;
            position.x *= lateral;
            pad.transform.position = position;
        }

        foreach (ObstacleState state in obstacles)
        {
            state.obstacle.amplitude = state.amplitude * obstacleMul;
            state.obstacle.speed = state.speed * obstacleMul;
        }
    }

    static void CaptureIfNeeded()
    {
        if (captured) return;
        captured = true;

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (!go.scene.IsValid()) continue;
            if (!go.name.StartsWith("Pile_") && !go.name.StartsWith("Rest_Pad_")) continue;

            pads.Add(new PadState
            {
                transform = go.transform,
                position = go.transform.position,
                scale = go.transform.localScale
            });
        }

        foreach (MovingObstacle obstacle in Object.FindObjectsOfType<MovingObstacle>())
        {
            obstacles.Add(new ObstacleState
            {
                obstacle = obstacle,
                amplitude = obstacle.amplitude,
                speed = obstacle.speed
            });
        }
    }

    static void RestoreAll()
    {
        foreach (PadState pad in pads)
        {
            pad.transform.position = pad.position;
            pad.transform.localScale = pad.scale;
        }

        foreach (ObstacleState state in obstacles)
        {
            state.obstacle.amplitude = state.amplitude;
            state.obstacle.speed = state.speed;
        }
    }
}
