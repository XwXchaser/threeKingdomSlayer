using UnityEngine;

/// <summary>Isolated visual lab. Never drives battle objects, route state or shared assets.</summary>
public sealed class CurvedScrollLab : MonoBehaviour
{
    public enum ViewMode { Flat, Curved, CurvedWithBackdrop }
    [Header("Explicit resources (no battle prefab instances)")]
    public Camera viewCamera;
    public Material groundMaterial;
    public Material spriteMaterial;
    public Sprite[] displaySprites;
    [Tooltip("Normalized foot anchor in the full Sprite rectangle, independently calibrated per sprite.")]
    public Vector2[] footAnchors;
    [Header("Presentation")]
    public ViewMode mode = ViewMode.CurvedWithBackdrop;
    [Min(0)] public float speed = 7;
    [Min(0)] public float bendStart = 12;
    [Range(0, 0.04f)] public float curvature = 0.012f;
    [Min(0.1f)] public float transitionLength = 6;
    [Range(3, 16)] public float roadWidth = 7;
    [Range(20, 120)] public float viewDistance = 80;
    [Range(0.05f, 0.3f)] public float spriteScale = 0.13f;
    [Range(0, 5)] public float roadsideGap = 1;
    [Range(0, 1)] public float paperTilt;
    public int seed = 71;
    public bool paused;
    public bool showRoadStripes = true;
    public bool showControls = true;
    [Header("Travel experiment")]
    [Min(0.1f)] public float tripLength = 60;
    [Min(0.1f)] public float acceleration = 5;
    [Min(0.1f)] public float brakingDistance = 12;

    public double Distance { get; private set; }
    public bool TripActive { get; private set; }
    public int ArrivalCount { get; private set; }
    public int DisplayCount => cards == null ? 0 : cards.Length;
    public float CurrentSpeed { get; private set; }
    public Transform GeneratedRoot => generated;

    const int Rows = 240;
    const int Bands = 5;
    const int CardCount = 40;
    const float Near = -14;
    Mesh groundMesh;
    Mesh backdropMesh;
    Vector3[] vertices;
    Color[] colors;
    Transform generated;
    GameObject backdrop;
    SpriteRenderer[] cards;
    float[] offsets;
    float[] sizes;
    float[] footX;
    float[] footY;
    double tripOrigin;
    bool arrived;
    bool initialized;
    GUIStyle labelStyle;

    void Start() { Initialize(); }

    public void Initialize()
    {
        if (initialized) return;
        if (viewCamera == null || groundMaterial == null || spriteMaterial == null || displaySprites == null || displaySprites.Length == 0)
        {
            Debug.LogError("[CurvedScrollLab] Assign camera, materials and display sprites in Inspector.", this);
            enabled = false;
            return;
        }
        initialized = true;
        generated = new GameObject("Runtime visual objects (not saved)").transform;
        generated.SetParent(transform, false);
        vertices = new Vector3[Rows * Bands * 4];
        colors = new Color[vertices.Length];
        var triangles = new int[Rows * Bands * 6];
        for (int i = 0; i < Rows * Bands; i++)
        {
            int v = i * 4, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }
        groundMesh = new Mesh { name = "Lab ground runtime mesh" };
        groundMesh.MarkDynamic();
        groundMesh.vertices = vertices;
        groundMesh.triangles = triangles;
        var ground = new GameObject("Subdivided road");
        ground.transform.SetParent(generated, false);
        ground.AddComponent<MeshFilter>().sharedMesh = groundMesh;
        var renderer = ground.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = groundMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        CreateBackdrop();
        cards = new SpriteRenderer[CardCount];
        offsets = new float[CardCount]; sizes = new float[CardCount];
        footX = new float[CardCount]; footY = new float[CardCount];
        var random = new System.Random(seed);
        for (int i = 0; i < CardCount; i++)
        {
            int index = i % displaySprites.Length;
            var card = new GameObject("Display paper " + i);
            card.transform.SetParent(generated, false);
            var sr = card.AddComponent<SpriteRenderer>();
            sr.sprite = displaySprites[index]; sr.sharedMaterial = spriteMaterial;
            sr.sortingOrder = 0;
            cards[i] = sr;
            offsets[i] = (float)random.NextDouble() * 3;
            sizes[i] = Mathf.Lerp(0.85f, 1.15f, (float)random.NextDouble());
            var anchor = footAnchors != null && index < footAnchors.Length ? footAnchors[index] : new Vector2(0.5f, 0);
            if (sr.sprite != null)
            {
                footX[i] = Mathf.Lerp(sr.sprite.bounds.min.x, sr.sprite.bounds.max.x, anchor.x);
                footY[i] = Mathf.Lerp(sr.sprite.bounds.min.y, sr.sprite.bounds.max.y, anchor.y);
            }
        }
        RefreshPresentation();
    }

    void CreateBackdrop()
    {
        backdrop = new GameObject("Static distant silhouettes (placeholder)");
        backdrop.transform.SetParent(generated, false);
        var v = new Vector3[42]; var c = new Color[42]; var t = new int[120];
        for (int i = 0; i <= 20; i++)
        {
            float x = -160 + i * 16;
            float height = 15 + Mathf.Sin(i * 0.7f) * 7 + Mathf.Sin(i * 1.9f) * 3;
            v[i * 2] = new Vector3(x, -80, 160);
            v[i * 2 + 1] = new Vector3(x, height, 160);
            c[i * 2] = c[i * 2 + 1] = new Color(0.35f, 0.55f, 0.55f);
            if (i == 20) continue;
            int k = i * 6, a = i * 2;
            t[k] = a; t[k + 1] = a + 1; t[k + 2] = a + 3;
            t[k + 3] = a; t[k + 4] = a + 3; t[k + 5] = a + 2;
        }
        backdropMesh = new Mesh { name = "Lab backdrop runtime mesh" };
        backdropMesh.vertices = v; backdropMesh.colors = c; backdropMesh.triangles = t;
        backdropMesh.RecalculateBounds();
        backdrop.AddComponent<MeshFilter>().sharedMesh = backdropMesh;
        var r = backdrop.AddComponent<MeshRenderer>(); r.sharedMaterial = groundMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void Update()
    {
        if (!initialized) return;
        if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
        if (Input.GetKeyDown(KeyCode.R)) ResetTravel();
        if (Input.GetKeyDown(KeyCode.N)) Step();
        if (!paused && Time.timeScale > 0) Advance(Time.deltaTime);
        RefreshPresentation();
    }

    public void Advance(float dt)
    {
        if (!initialized || dt <= 0 || arrived) return;
        float targetSpeed = Mathf.Max(0, speed);
        double remaining = Mathf.Max(0.1f, tripLength) - (Distance - tripOrigin);
        if (TripActive)
            targetSpeed *= Mathf.Sqrt(Mathf.Clamp01((float)remaining / Mathf.Max(0.1f, brakingDistance)));
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, Mathf.Max(0.1f, acceleration) * dt);
        double step = CurrentSpeed * dt;
        if (TripActive && (step >= remaining || remaining <= 0.001))
        {
            Distance = tripOrigin + Mathf.Max(0.1f, tripLength);
            CurrentSpeed = 0; TripActive = false; arrived = true; ArrivalCount++;
        }
        else Distance += step;
    }

    public void ResetTravel()
    {
        Distance = 0; CurrentSpeed = 0; TripActive = false; arrived = false; ArrivalCount = 0;
        if (initialized) RefreshPresentation();
    }
    public void StartTrip() { tripOrigin = Distance; arrived = false; TripActive = true; paused = false; }
    public void ResumeLoop() { TripActive = false; arrived = false; paused = false; }
    public void Step() { paused = true; Advance(1f / 30f); RefreshPresentation(); }
    public void Seek(float normalized)
    {
        Distance = Mathf.Clamp01(normalized) * (Mathf.Max(20, viewDistance) - Near);
        CurrentSpeed = 0; TripActive = false; arrived = false; paused = true;
        RefreshPresentation();
    }

    public void SetPresentationDistance(double distance)
    {
        Distance = Mathf.Max(0f, (float)distance);
        CurrentSpeed = 0f;
        TripActive = false;
        arrived = false;
        RefreshPresentation();
    }

    public float Drop(float forwardDistance)
    {
        if (mode == ViewMode.Flat) return 0;
        float x = Mathf.Max(0, forwardDistance - Mathf.Max(0, bendStart));
        float blend = Mathf.SmoothStep(0, 1, x / Mathf.Max(0.1f, transitionLength));
        return Mathf.Max(0, curvature) * x * x * blend;
    }

    public void RefreshPresentation()
    {
        if (!initialized) return;
        float far = Mathf.Max(20, viewDistance);
        float half = Mathf.Max(1.5f, roadWidth * 0.5f);
        float span = far - Near;
        // Quads follow the moving strip phase: colors stay attached to physical strips, not screen rows.
        float cell = span / (Rows - 1);
        float phase = (float)(Distance % cell);
        long firstCell = (long)System.Math.Floor(Distance / cell);
        for (int row = 0; row < Rows; row++)
        {
            float z0 = Near + row * cell - phase;
            float z1 = z0 + cell;
            bool stripe = ((firstCell + row) / 5 % 2) == 0;
            for (int band = 0; band < Bands; band++)
            {
                float x0, x1;
                if (band == 0) { x0 = -100; x1 = -half - 0.18f; }
                else if (band == 1) { x0 = -half - 0.18f; x1 = -half; }
                else if (band == 2) { x0 = -half; x1 = half; }
                else if (band == 3) { x0 = half; x1 = half + 0.18f; }
                else { x0 = half + 0.18f; x1 = 100; }
                int k = (row * Bands + band) * 4;
                vertices[k] = new Vector3(x0, -Drop(z0), z0);
                vertices[k + 1] = new Vector3(x1, -Drop(z0), z0);
                vertices[k + 2] = new Vector3(x1, -Drop(z1), z1);
                vertices[k + 3] = new Vector3(x0, -Drop(z1), z1);
                Color color = band == 2 ? new Color(0.48f, 0.40f, 0.28f) :
                    (band == 1 || band == 3 ? new Color(0.79f, 0.72f, 0.46f) : new Color(0.26f, 0.38f, 0.23f));
                if (stripe && showRoadStripes) color *= 0.92f;
                color.a = 1;
                for (int j = 0; j < 4; j++) colors[k + j] = color;
            }
        }
        groundMesh.vertices = vertices; groundMesh.colors = colors; groundMesh.RecalculateBounds();
        backdrop.SetActive(mode == ViewMode.CurvedWithBackdrop);
        float cycle = (float)(Distance % span);
        for (int i = 0; i < cards.Length; i++)
        {
            float z = Near + Mathf.Repeat(i * span / CardCount - cycle, span);
            float x = (half + Mathf.Max(0, roadsideGap) + offsets[i]) * (i % 2 == 0 ? -1 : 1);
            float scale = Mathf.Max(0.01f, spriteScale) * sizes[i];
            var rotation = Quaternion.Euler(-Mathf.Atan((Drop(z + 0.05f) - Drop(z - 0.05f)) / 0.1f) * Mathf.Rad2Deg * paperTilt, 0, 0);
            cards[i].transform.localRotation = rotation;
            cards[i].transform.localScale = Vector3.one * scale;
            cards[i].transform.localPosition = new Vector3(x, -Drop(z), z) - rotation * new Vector3(footX[i] * scale, footY[i] * scale, 0);
            float alpha = Mathf.Clamp01((far - z) / 10) * Mathf.Clamp01((z - Near) / 2);
            cards[i].color = new Color(1, 1, 1, alpha);
        }
    }

    void OnGUI()
    {
        if (!showControls || !initialized) return;
        if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        float scale = Mathf.Clamp(Screen.width / 480f, 0.65f, 1.5f);
        var old = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUILayout.BeginArea(new Rect(10, 10, 300, 392), GUI.skin.box);
        GUILayout.Label("CURVED SCROLL / VISUAL LAB", labelStyle);
        GUILayout.Label("Display sprites only - no combat or saves");
        mode = (ViewMode)GUILayout.Toolbar((int)mode, new[] { "Flat", "Curve", "+ Backdrop" });
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(paused ? "Play" : "Pause")) paused = !paused;
        if (GUILayout.Button("Step")) Step();
        if (GUILayout.Button("Reset")) ResetTravel();
        GUILayout.EndHorizontal();
        GUILayout.Label("Speed " + speed.ToString("F1")); speed = GUILayout.HorizontalSlider(speed, 0, 20);
        GUILayout.Label("Curvature " + curvature.ToString("F4")); curvature = GUILayout.HorizontalSlider(curvature, 0, 0.04f);
        GUILayout.Label("Flat zone " + bendStart.ToString("F1")); bendStart = GUILayout.HorizontalSlider(bendStart, 0, 40);
        GUILayout.Label("Road width " + roadWidth.ToString("F1")); roadWidth = GUILayout.HorizontalSlider(roadWidth, 3, 16);
        GUILayout.Label("Distance " + Distance.ToString("F2") + " / arrivals " + ArrivalCount);
        float progress = (float)(Distance % (Mathf.Max(20, viewDistance) - Near)) / (Mathf.Max(20, viewDistance) - Near);
        float seek = GUILayout.HorizontalSlider(progress, 0, 1);
        if (Mathf.Abs(seek - progress) > 0.0001f) Seek(seek);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Travel + stop")) StartTrip();
        if (GUILayout.Button("Loop")) ResumeLoop();
        GUILayout.EndHorizontal();
        GUILayout.Label("Space: pause   N: step   R: reset\nInspector: width, range, anchor, tilt, trip");
        GUILayout.EndArea();
        GUI.matrix = old;
    }

    void OnDestroy()
    {
        if (groundMesh != null) Destroy(groundMesh);
        if (backdropMesh != null) Destroy(backdropMesh);
        if (generated != null) Destroy(generated.gameObject);
    }
}
