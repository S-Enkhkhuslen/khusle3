#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Authoring tool only. The finished scene contains ordinary editable prefabs,
// with shared meshes/materials and no scenery Update methods at runtime.
public static class WorldSceneryBuilder
{
    public const string RootName = "Planet Wreckage";
    private const string Folder = "Assets/Scenery";
    private static readonly System.Random random = new System.Random(731904);
    private static readonly List<Vector4> clearAreas = new List<Vector4>();
    private static readonly List<Vector3[]> routes = new List<Vector3[]>();
    private static readonly List<Vector4> placed = new List<Vector4>();
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private static Terrain terrain;
    private static Mesh stoneA, stoneB, shell, wing, dish;
    public static string LastReport { get; private set; }

    [MenuItem("Tools/World/Add Planet Wreckage")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene in Edit mode first.");
        if (scene.isDirty)
            throw new InvalidOperationException("Scene has unsaved edits. Save your changes before adding scenery.");
        if (GameObject.Find(RootName) != null)
            throw new InvalidOperationException("Planet Wreckage already exists. Move or edit its prefab instances in the Hierarchy.");

        terrain = Terrain.activeTerrains.FirstOrDefault(t => t.terrainData != null
            && t.transform.position.x == 0f && t.transform.position.z == 0f);
        if (terrain == null) throw new InvalidOperationException("Central terrain tile was not found.");
        clearAreas.Clear(); routes.Clear(); placed.Clear(); materials.Clear();
        PrepareClearAreas();
        EnsureFolder(Folder); EnsureFolder(Folder + "/Materials");
        EnsureFolder(Folder + "/Meshes"); EnsureFolder(Folder + "/Prefabs");
        Palette();
        stoneA = SaveMesh(Rock(17), "Faceted Rock A");
        stoneB = SaveMesh(Rock(81), "Faceted Rock B");
        shell = SaveMesh(Tube(), "Torn Hull Shell");
        wing = SaveMesh(Wing(), "Broken Wing");
        dish = SaveMesh(Dish(), "Damaged Dish");

        GameObject shuttle = Prefab("Shattered Shuttle", Shuttle);
        GameObject engine = Prefab("Detached Engine", p => Engine(p, Vector3.up * 1.3f, Quaternion.Euler(0, 28, 13)));
        GameObject fragment = Prefab("Hull Fragments", Fragments);
        GameObject detachedWing = Prefab("Torn Wing", p => {
            MeshPart(p, "Torn Wing", wing, new Vector3(-2f, 0.5f, 2f), Quaternion.Euler(5, 22, -8), Vector3.one, "Hull", true);
            Box(p, "Burnt Edge", new Vector3(0f, 0.75f, -1f), new Vector3(4.5f, 0.12f, 0.6f), "Char", new Vector3(0, 20, 0));
        });
        GameObject habitat = Prefab("Ruined Station", Station);
        GameObject comms = Prefab("Collapsed Communications", Communications);
        GameObject[] rocks = new GameObject[3];
        for (int i = 0; i < rocks.Length; i++) { int variant = i; rocks[i] = Prefab("Rock Cluster " + (i + 1), p => Rocks(p, variant)); }

        var root = new GameObject(RootName);
        try
        {
            Transform wrecks = Group(root, "Crashed Ships");
            Transform stations = Group(root, "Station Ruins");
            Transform stones = Group(root, "Rock Fields");
            Transform debris = Group(root, "Scattered Debris");
            int shipCount = Scatter(shuttle, wrecks, 9, 18f, 1.3f, 22f);
            int stationCount = Scatter(habitat, stations, 4, 22f, 1.5f, 35f)
                + Scatter(comms, stations, 3, 15f, 1.8f, 30f);
            int rockCount = 0;
            foreach (GameObject rock in rocks) rockCount += Scatter(rock, stones, 35, 6f, 2.2f, 9f);
            int debrisCount = Scatter(engine, debris, 12, 5f, 1.5f, 12f)
                + Scatter(fragment, debris, 20, 7f, 1.7f, 10f)
                + Scatter(detachedWing, debris, 12, 8f, 1.5f, 12f);
            Physics.SyncTransforms();
            if (shipCount < 5 || stationCount < 4 || rockCount < 60)
                throw new InvalidOperationException("Not enough safe scenery positions found; scene was not saved.");
            Undo.RegisterCreatedObjectUndo(root, "Add planet wreckage");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save scenery scene.");
            AssetDatabase.SaveAssets();
            LastReport = "Ships=" + shipCount + "\nStations=" + stationCount + "\nRockClusters=" + rockCount
                + "\nDebris=" + debrisCount + "\nRenderers=" + root.GetComponentsInChildren<Renderer>().Length
                + "\nColliders=" + root.GetComponentsInChildren<Collider>().Length
                + "\nObstacles=" + root.GetComponentsInChildren<NavMeshObstacle>().Length
                + "\nProtectedRoutes=" + routes.Count;
            Debug.Log("Planet scenery added.\n" + LastReport);
        }
        catch { Object.DestroyImmediate(root); throw; }
    }

    private static void PrepareClearAreas()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) throw new InvalidOperationException("Player was not found.");
        Protect(player.transform.position, 24f);
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>()) Protect(enemy.transform.position, 13f);
        foreach (TankGenerator tank in Object.FindObjectsByType<TankGenerator>())
            Protect(tank.transform.position, tank.spawnRadius + 5f);
        var generators = Object.FindObjectsByType<EnemyGenerator>();
        foreach (EnemyGenerator generator in generators) Protect(generator.transform.position, 30f);
        // Preserve a traversable corridor from the start to every objective,
        // plus corridors between generators, before placing any solid props.
        var destinations = new List<Vector3> { player.transform.position };
        destinations.AddRange(generators.Select(g => g.transform.position));
        for (int i = 0; i < destinations.Count; i++)
            for (int j = i + 1; j < destinations.Count; j++)
                if (NavMesh.SamplePosition(destinations[i], out var a, 10f, NavMesh.AllAreas)
                    && NavMesh.SamplePosition(destinations[j], out var b, 10f, NavMesh.AllAreas))
                {
                    var path = new NavMeshPath();
                    if (NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                        routes.Add(path.corners);
                }
    }

    private static void Protect(Vector3 point, float radius) => clearAreas.Add(new Vector4(point.x, point.y, point.z, radius));
    private static Transform Group(GameObject root, string name)
    {
        var group = new GameObject(name); group.transform.SetParent(root.transform, false); return group.transform;
    }
    private static float Range(float low, float high) => low + (float)random.NextDouble() * (high - low);
    private static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

    private static int Scatter(GameObject prefab, Transform parent, int count, float radius, float maxRelief, float gap)
    {
        int made = 0;
        Vector3 origin = terrain.transform.position, size = terrain.terrainData.size;
        for (int attempt = 0; attempt < count * 350 && made < count; attempt++)
        {
            Vector3 point;
            // A few pieces within sight of the start establish the setting immediately.
            if (made < 2 && attempt < 150)
            {
                Vector3 start = GameObject.FindGameObjectWithTag("Player").transform.position;
                float angle = Range(0f, Mathf.PI * 2f), distance = Range(42f + radius, 110f);
                point = start + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            }
            else point = origin + new Vector3(Range(25f + radius, size.x - 25f - radius), 0f, Range(25f + radius, size.z - 25f - radius));
            if (point.x < origin.x + radius + 20 || point.x > origin.x + size.x - radius - 20
                || point.z < origin.z + radius + 20 || point.z > origin.z + size.z - radius - 20) continue;
            point.y = Height(point);
            bool blocked = clearAreas.Any(c => Vector2.Distance(XZ(point), new Vector2(c.x, c.z)) < c.w + radius)
                || placed.Any(c => Vector2.Distance(XZ(point), new Vector2(c.x, c.z)) < c.w + radius + gap);
            if (blocked) continue;
            foreach (Vector3[] route in routes)
            {
                for (int k = 1; k < route.Length; k++)
                    if (SegmentDistance(XZ(point), XZ(route[k - 1]), XZ(route[k])) < radius + 6f) { blocked = true; break; }
                if (blocked) break;
            }
            if (blocked) continue;
            float min = point.y, max = point.y;
            for (int k = 0; k < 12; k++)
            {
                float angle = k * Mathf.PI * 2f / 12;
                float height = Height(point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
                min = Mathf.Min(min, height); max = Mathf.Max(max, height);
            }
            if (max - min > maxRelief) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = prefab.name + " " + (made + 1).ToString("00");
            instance.transform.position = new Vector3(point.x, (min + max) * 0.5f - 0.2f, point.z);
            instance.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            placed.Add(new Vector4(point.x, point.y, point.z, radius));
            made++;
        }
        return made;
    }

    private static float Height(Vector3 p) => terrain.transform.position.y + terrain.SampleHeight(p);
    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 delta = b - a;
        float t = delta.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, delta) / delta.sqrMagnitude) : 0f;
        return Vector2.Distance(p, a + delta * t);
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
    private static void Palette()
    {
        Mat("Hull", new Color(0.58f, 0.62f, 0.61f), 0.45f);
        Mat("Dark", new Color(0.10f, 0.13f, 0.15f), 0.6f);
        Mat("Char", new Color(0.045f, 0.042f, 0.04f), 0.1f);
        Mat("Rust", new Color(0.30f, 0.13f, 0.07f), 0.25f);
        Mat("Orange", new Color(0.85f, 0.31f, 0.07f), 0.35f);
        Mat("Glass", new Color(0.035f, 0.16f, 0.23f), 0.65f);
        Mat("Solar", new Color(0.055f, 0.12f, 0.23f), 0.6f);
        Mat("Blue", new Color(0.05f, 0.5f, 0.65f), 0.4f, true);
        Mat("Rock", new Color(0.25f, 0.22f, 0.20f), 0.0f);
        Mat("Rock Light", new Color(0.42f, 0.33f, 0.25f), 0.0f);
    }
    private static void Mat(string name, Color color, float metallic, bool glow = false)
    {
        string path = Folder + "/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", 0.22f);
        if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 0.7f); }
        mat.enableInstancing = true; EditorUtility.SetDirty(mat); materials[name] = mat;
    }
    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = Folder + "/Meshes/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        mesh.name = name;
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing;
    }
    private static GameObject Prefab(string name, Action<Transform> make)
    {
        var root = new GameObject(name);
        try
        {
            make(root.transform);
            MergeMeshes(root, name);
            Bounds bounds = root.GetComponent<MeshRenderer>().bounds;
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = bounds.center;
            obstacle.size = new Vector3(bounds.size.x + 0.2f, Mathf.Max(2f, bounds.size.y), bounds.size.z + 0.2f);
            obstacle.carving = true; obstacle.carveOnlyStationary = true;
            foreach (Transform child in root.GetComponentsInChildren<Transform>()) child.gameObject.isStatic = true;
            return PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Prefabs/" + name + ".prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }
    private static void MergeMeshes(GameObject root, string name)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();
        var groups = filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
        var combined = new List<Mesh>();
        foreach (var group in groups)
        {
            var mesh = new Mesh();
            mesh.CombineMeshes(group.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = f.transform.localToWorldMatrix }).ToArray(), true, true);
            combined.Add(mesh);
        }
        var result = new Mesh();
        result.CombineMeshes(combined.Select(m => new CombineInstance { mesh = m, transform = Matrix4x4.identity }).ToArray(), false, false);
        result.RecalculateBounds();
        root.AddComponent<MeshFilter>().sharedMesh = SaveMesh(result, name);
        root.AddComponent<MeshRenderer>().sharedMaterials = groups.Select(g => g.Key).ToArray();
        foreach (MeshFilter f in filters) { Object.DestroyImmediate(f.GetComponent<MeshRenderer>()); Object.DestroyImmediate(f); }
        foreach (Mesh mesh in combined) Object.DestroyImmediate(mesh);
    }
    private static Transform Primitive(Transform parent, string name, PrimitiveType type, Vector3 p, Vector3 scale, string mat, Vector3 angles, bool solid)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name; part.transform.SetParent(parent, false);
        part.transform.localPosition = p; part.transform.localEulerAngles = angles; part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = materials[mat];
        if (!solid) Object.DestroyImmediate(part.GetComponent<Collider>());
        return part.transform;
    }
    private static Transform Box(Transform p, string name, Vector3 pos, Vector3 scale, string mat, Vector3 angles = default, bool solid = false)
        => Primitive(p, name, PrimitiveType.Cube, pos, scale, mat, angles, solid);
    private static Transform Cylinder(Transform p, string name, Vector3 pos, Vector3 scale, string mat, Vector3 angles, bool solid = false)
        => Primitive(p, name, PrimitiveType.Cylinder, pos, scale, mat, angles, solid);
    private static Transform MeshPart(Transform parent, string name, Mesh mesh, Vector3 pos, Quaternion rotation, Vector3 scale, string mat, bool solid)
    {
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false); obj.transform.localPosition = pos; obj.transform.localRotation = rotation; obj.transform.localScale = scale;
        obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = materials[mat];
        if (solid) { var collider = obj.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = true; }
        return obj.transform;
    }

    private static void Engine(Transform p, Vector3 pos, Quaternion rotation)
    {
        var mount = new GameObject("Broken Engine").transform; mount.SetParent(p, false); mount.localPosition = pos; mount.localRotation = rotation;
        Cylinder(mount, "Engine Casing", Vector3.zero, new Vector3(2.5f, 2.2f, 2.5f), "Hull", new Vector3(90, 0, 0), true);
        Cylinder(mount, "Exhaust Rim", new Vector3(0, 0, -2.15f), new Vector3(2.75f, 0.18f, 2.75f), "Rust", new Vector3(90, 0, 0));
        Cylinder(mount, "Black Exhaust", new Vector3(0, 0, -2.35f), new Vector3(2.25f, 0.04f, 2.25f), "Char", new Vector3(90, 0, 0));
        Cylinder(mount, "Dead Core", new Vector3(0, 0, -2.4f), new Vector3(0.8f, 0.04f, 0.8f), "Orange", new Vector3(90, 0, 0));
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 0.5f;
            Box(mount, "Cooling Rib", new Vector3(Mathf.Cos(a) * 1.2f, Mathf.Sin(a) * 1.2f, 0), new Vector3(0.22f, 0.22f, 3.4f), "Dark");
        }
    }
    private static void Shuttle(Transform p)
    {
        var hull = new GameObject("Split Fuselage").transform; hull.SetParent(p, false); hull.localPosition = new Vector3(0, 2.1f, 0); hull.localEulerAngles = new Vector3(3, 0, -9);
        MeshPart(hull, "Torn Shell", shell, Vector3.zero, Quaternion.identity, Vector3.one, "Hull", false);
        Box(hull, "Main Body Collision", Vector3.zero, new Vector3(4.2f, 3.6f, 8.2f), "Char", default, true);
        Box(hull, "Upper Armor", new Vector3(0, 1.86f, 0.6f), new Vector3(3.3f, 0.18f, 5.8f), "Hull");
        Box(hull, "Broken Cockpit", new Vector3(0, 0.9f, 4f), new Vector3(3f, 1.4f, 2.1f), "Dark", new Vector3(18, 0, 0), true);
        Box(hull, "Cockpit Glass", new Vector3(0, 1.25f, 4.95f), new Vector3(2.55f, 0.7f, 0.08f), "Glass", new Vector3(18, 0, 0));
        for (int i = 0; i < 5; i++)
        {
            Box(hull, "Scorched Hull Patch", new Vector3(2.16f, -0.8f + i * 0.46f, Range(-2.6f, 2.2f)), new Vector3(0.08f, 0.35f, Range(1f, 3.6f)), i % 2 == 0 ? "Char" : "Rust", new Vector3(Range(-15, 15), 0, 0));
            Box(hull, "Fracture Rib", new Vector3(-1.8f + i * 0.9f, 0.8f, -4.5f), new Vector3(0.18f, 2f, 1.8f), "Rust", new Vector3(-35f + i * 9f, 0, 15));
        }
        MeshPart(p, "Attached Wing", wing, new Vector3(1.4f, 1.2f, 0), Quaternion.Euler(0, 0, 3), Vector3.one, "Hull", true);
        MeshPart(p, "Snapped Wing", wing, new Vector3(-6f, 0.5f, 3f), Quaternion.Euler(7, 170, -7), new Vector3(0.8f, 1, 0.8f), "Hull", true);
        Engine(p, new Vector3(2.8f, 1.3f, -4.8f), Quaternion.Euler(0, -10, 12));
        Engine(p, new Vector3(-7f, 1.2f, -5.3f), Quaternion.Euler(0, -48, -16));
        Box(p, "Broken Tail", new Vector3(-1.5f, 3.8f, -4), new Vector3(0.35f, 5.5f, 2.6f), "Orange", new Vector3(-20, 0, 42), true);
        for (int i = 0; i < 7; i++) Box(p, "Impact Debris", new Vector3(Range(-10, 10), 0.25f, Range(5, 10)), new Vector3(Range(0.5f, 2f), 0.25f, Range(0.8f, 2.8f)), i % 2 == 0 ? "Rust" : "Dark", new Vector3(0, Range(0, 360), Range(-12, 12)), true);
        Box(p, "Wing Identification", new Vector3(5.1f, 1.45f, -3f), new Vector3(0.7f, 0.08f, 3f), "Orange", new Vector3(0, -10, 0));
    }
    private static void Module(Transform p, Vector3 position, Vector3 angles)
    {
        var m = new GameObject("Damaged Habitat Module").transform; m.SetParent(p, false); m.localPosition = position; m.localEulerAngles = angles;
        Box(m, "Habitat", new Vector3(0, 2.6f, 0), new Vector3(8f, 5.2f, 5.8f), "Hull", default, true);
        Box(m, "Caved Roof", new Vector3(-0.5f, 5.22f, 0), new Vector3(5.6f, 0.2f, 4.7f), "Char", new Vector3(0, 0, -6));
        Box(m, "Black Doorway", new Vector3(0, 1.7f, -2.94f), new Vector3(1.8f, 3.4f, 0.08f), "Char");
        Box(m, "Door Lintel", new Vector3(0, 3.5f, -3f), new Vector3(2.2f, 0.25f, 0.25f), "Orange");
        for (int i = -1; i <= 1; i += 2)
        {
            Box(m, "Fractured Window", new Vector3(i * 2.6f, 3f, -2.97f), new Vector3(1.8f, 1.05f, 0.1f), "Glass");
            Box(m, "Window Crack", new Vector3(i * 2.6f, 3f, -3.04f), new Vector3(0.12f, 1.15f, 0.06f), "Char", new Vector3(0, 0, 28));
            Box(m, "Support Beam", new Vector3(i * 3.8f, 2.7f, -3f), new Vector3(0.25f, 5.4f, 0.35f), "Dark");
        }
        Box(m, "Rusted Side", new Vector3(4.03f, 1.1f, 0), new Vector3(0.08f, 1.8f, 4.8f), "Rust");
        Box(m, "Armor Stripe", new Vector3(0, 0.8f, -3f), new Vector3(7.5f, 0.25f, 0.08f), "Orange");
    }
    private static void Solar(Transform p, Vector3 pos, Vector3 angles)
    {
        var array = new GameObject("Broken Solar Array").transform; array.SetParent(p, false); array.localPosition = pos; array.localEulerAngles = angles;
        Box(array, "Panel Frame", Vector3.zero, new Vector3(9.8f, 0.3f, 5f), "Dark", default, true);
        for (int x = 0; x < 5; x++) for (int z = 0; z < 3; z++)
        {
            if ((x == 0 && z == 0) || (x == 4 && z == 2)) continue;
            Box(array, "Solar Cell", new Vector3(-3.8f + x * 1.9f, 0.19f, -1.55f + z * 1.55f), new Vector3(1.72f, 0.06f, 1.36f), "Solar");
        }
    }
    private static void Station(Transform p)
    {
        Module(p, new Vector3(-5f, 0f, 0f), new Vector3(0, 0, -3));
        Module(p, new Vector3(5f, -0.5f, 4f), new Vector3(0, 18, 13));
        Box(p, "Collapsed Walkway", new Vector3(0, 0.8f, -3f), new Vector3(10f, 0.5f, 2f), "Dark", new Vector3(0, 20, -12), true);
        Solar(p, new Vector3(-6f, 1.6f, -8f), new Vector3(9, 12, -8));
        Solar(p, new Vector3(7f, 0.8f, -7f), new Vector3(-5, -25, 4));
        Cylinder(p, "Fallen Oxygen Tank", new Vector3(11f, 1.2f, 1f), new Vector3(2.4f, 3.1f, 2.4f), "Hull", new Vector3(80, 30, 0), true);
        Box(p, "Snapped Mast", new Vector3(0, 3.2f, 8f), new Vector3(0.5f, 8f, 0.5f), "Rust", new Vector3(35, 0, -30), true);
        MeshPart(p, "Broken Dish", dish, new Vector3(0, 4.7f, 11f), Quaternion.Euler(36, 0, 20), Vector3.one, "Hull", true);
        for (int i = 0; i < 9; i++) Box(p, "Station Rubble", new Vector3(Range(-12, 12), 0.3f, Range(7, 13)), new Vector3(Range(0.6f, 3f), Range(0.3f, 0.8f), Range(0.6f, 2.5f)), i % 2 == 0 ? "Hull" : "Rust", new Vector3(0, Range(0, 360), Range(-10, 10)), true);
    }
    private static void Communications(Transform p)
    {
        Box(p, "Broken Equipment Block", new Vector3(-4, 1.5f, 0), new Vector3(5f, 3f, 5f), "Hull", new Vector3(0, 10, 5), true);
        Box(p, "Burnt Equipment Face", new Vector3(-4, 1.7f, -2.6f), new Vector3(3.8f, 1.9f, 0.1f), "Char", new Vector3(0, 10, 0));
        for (int i = 0; i < 3; i++) Box(p, "Faded Status Light", new Vector3(-5f + i, 2f, -2.7f), new Vector3(0.2f, 0.2f, 0.1f), "Blue");
        Cylinder(p, "Leaning Antenna", new Vector3(3, 3.2f, 0), new Vector3(0.55f, 4.5f, 0.55f), "Rust", new Vector3(32, 0, 38), true);
        MeshPart(p, "Fallen Receiver", dish, new Vector3(6, 2.2f, 2), Quaternion.Euler(35, 0, -30), new Vector3(1.5f, 1.5f, 1.5f), "Hull", true);
        Solar(p, new Vector3(-3, 0.9f, -7), new Vector3(8, 28, -5));
        Box(p, "Toppled Truss", new Vector3(1, 0.8f, 5), new Vector3(10f, 0.3f, 0.4f), "Dark", new Vector3(0, -15, 0), true);
        for (int i = 0; i < 5; i++) Box(p, "Truss Rib", new Vector3(-3 + i * 2, 1f, 5), new Vector3(0.18f, 0.2f, 2.8f), "Dark", new Vector3(0, 25, 0));
    }
    private static void Fragments(Transform p)
    {
        for (int i = 0; i < 5; i++)
            Box(p, "Torn Armor", new Vector3(Range(-3.5f, 3.5f), Range(0.15f, 0.4f), Range(-3f, 3f)), new Vector3(Range(1f, 3f), Range(0.2f, 0.45f), Range(1f, 3f)), i % 2 == 0 ? "Hull" : "Rust", new Vector3(Range(-9, 9), Range(0, 360), Range(-9, 9)), true);
        Cylinder(p, "Ruptured Pipe", new Vector3(1, 0.6f, 1), new Vector3(0.8f, 2.6f, 0.8f), "Dark", new Vector3(87, 38, 0), true);
    }
    private static void Rocks(Transform p, int variant)
    {
        int count = 3 + variant;
        for (int i = 0; i < count; i++)
        {
            float height = i == 0 ? 4f + variant * 0.5f : Range(0.8f, 2.4f);
            Vector3 position = i == 0 ? Vector3.zero : new Vector3(Range(-3, 3), 0, Range(-3, 3));
            MeshPart(p, "Boulder", (i + variant) % 2 == 0 ? stoneA : stoneB, position, Quaternion.Euler(0, Range(0, 360), 0), new Vector3(height * Range(0.65f, 0.95f), height, height * Range(0.65f, 1f)), i % 2 == 0 ? "Rock" : "Rock Light", true);
        }
    }

    // Flat normals keep rocks angular, rather than looking like stretched spheres.
    private static Mesh FlatMesh(List<Vector3> vertices, List<int> triangles)
    {
        var flat = new Vector3[triangles.Count]; var indices = new int[triangles.Count];
        for (int i = 0; i < triangles.Count; i++) { flat[i] = vertices[triangles[i]]; indices[i] = i; }
        var mesh = new Mesh { vertices = flat, triangles = indices }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    private static Mesh Rock(int seed)
    {
        var rng = new System.Random(seed); var v = new List<Vector3>(); var t = new List<int>();
        const int sides = 7;
        for (int ring = 0; ring < 3; ring++) for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides + ring * 0.19f;
            float r = (ring == 2 ? 0.42f : 0.7f) * (0.8f + (float)rng.NextDouble() * 0.35f);
            float y = ring == 0 ? -0.08f : ring == 1 ? 0.45f + (float)rng.NextDouble() * 0.13f : 0.85f + (float)rng.NextDouble() * 0.15f;
            v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
        }
        for (int ring = 0; ring < 2; ring++) for (int i = 0; i < sides; i++)
        {
            int a = ring * sides + i, b = ring * sides + (i + 1) % sides, c = a + sides, d = b + sides;
            t.AddRange(new[] { a, c, b, b, c, d });
        }
        int top = v.Count; v.Add(new Vector3(0.1f, 1f, 0)); int bottom = v.Count; v.Add(new Vector3(0, -0.08f, 0));
        for (int i = 0; i < sides; i++) { int next = (i + 1) % sides; t.AddRange(new[] { top, 14 + next, 14 + i, bottom, i, next }); }
        return FlatMesh(v, t);
    }
    private static Mesh Tube()
    {
        var v = new List<Vector3>(); var t = new List<int>(); const int sides = 10;
        for (int ring = 0; ring < 4; ring++) for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides; float radius = ring < 2 ? 2.45f : 2.1f;
            float z = ring % 2 == 0 ? -4f - (i % 3) * 0.35f : 3.5f;
            v.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * 0.8f, z));
        }
        for (int i = 0; i < sides; i++)
        {
            int n = (i + 1) % sides;
            t.AddRange(new[] { i, n, i + 10, n, n + 10, i + 10, i + 20, i + 30, n + 20, n + 20, i + 30, n + 30,
                i, i + 20, n, n, i + 20, n + 20, i + 10, n + 10, i + 30, n + 10, n + 30, i + 30 });
        }
        return FlatMesh(v, t);
    }
    private static Mesh Wing()
    {
        var v = new List<Vector3>();
        Vector3[] outline = { new Vector3(0, 0, 1), new Vector3(8, 0, -3), new Vector3(7, 0, -5), new Vector3(0, 0, -4) };
        foreach (Vector3 p in outline) v.Add(p + Vector3.up * 0.18f);
        foreach (Vector3 p in outline) v.Add(p - Vector3.up * 0.18f);
        return FlatMesh(v, new List<int> { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6, 0, 4, 1, 1, 4, 5, 1, 5, 2, 2, 5, 6, 2, 6, 3, 3, 6, 7, 3, 7, 0, 0, 7, 4 });
    }
    private static Mesh Dish()
    {
        var v = new List<Vector3>(); var t = new List<int>(); const int sides = 16;
        v.Add(Vector3.zero);
        for (int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2 / sides; v.Add(new Vector3(Mathf.Cos(a) * 3f, 1.3f, Mathf.Sin(a) * 3f)); }
        for (int i = 0; i < sides; i++) { int a = i + 1, b = (i + 1) % sides + 1; if (i == 3 || i == 4) continue; t.AddRange(new[] { 0, b, a, 0, a, b }); }
        return FlatMesh(v, t);
    }
}
#endif
