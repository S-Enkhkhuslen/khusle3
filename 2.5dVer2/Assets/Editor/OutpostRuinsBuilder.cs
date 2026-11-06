#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Creates ordinary editable scene prefabs. No scenery scripts run during gameplay.
public static class OutpostRuinsBuilder
{
    public const string RootName = "Mountain Outpost Ruins";
    private const string Folder = "Assets/Scenery/MountainRuins";
    public static string LastReport { get; private set; }
    private static Terrain terrain;
    private static Vector3 origin;
    private static Quaternion heading;
    private static Material concrete, pale, dark, rust, orange;
    private static Mesh[] wallMeshes;

    public static void Audit()
    {
        GameplayScene(scene => {
            var report = new List<string>();
            foreach (var gen in Generators(scene))
            {
                var t = TerrainAt(gen.transform.position);
                float lo = float.MaxValue, hi = float.MinValue;
                for (int i = 0; i < 32; i++) {
                    float a = i * Mathf.PI / 16f;
                    float h = t.SampleHeight(gen.transform.position + new Vector3(Mathf.Cos(a) * 28f, 0f, Mathf.Sin(a) * 28f)) + t.transform.position.y;
                    lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                }
                report.Add(gen.name + " position=" + gen.transform.position + " ground=" + t.SampleHeight(gen.transform.position) + " perimeter heights=" + lo + ".." + hi);
            }
            LastReport = string.Join("\n", report);
        });
    }

    [MenuItem("Tools/World/Add Mountain Outpost Ruins")]
    public static void Build()
    {
        GameplayScene(scene => {
            if (scene.GetRootGameObjects().Any(r => r.name == RootName)) throw new Exception("Mountain ruins already exist; edit their prefab instances in the Hierarchy.");
            var generators = Generators(scene);
            if (generators.Length == 0) throw new Exception("No enemy generators found.");
            EnsureFolder(Folder); EnsureFolder(Folder + "/Meshes"); EnsureFolder(Folder + "/Materials"); EnsureFolder(Folder + "/Prefabs");
            concrete = Material("Weathered Concrete", new Color(.33f, .30f, .25f));
            pale = Material("Exposed Concrete", new Color(.51f, .47f, .38f));
            dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenery/Materials/Dark.mat");
            rust = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenery/Materials/Rust.mat");
            orange = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenery/Materials/Orange.mat");
            wallMeshes = Enumerable.Range(0, 3).Select(i => SaveMesh(JaggedWall(i), "Broken Wall " + i)).ToArray();
            var group = new GameObject(RootName);
            var report = new List<string>();
            try
            {
                var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerHealth>()).First();
                for (int i = 0; i < generators.Length; i++)
                {
                    origin = generators[i].transform.position;
                    terrain = TerrainAt(origin);
                    origin.y = terrain.SampleHeight(origin) + terrain.transform.position.y;
                    Vector3 away = origin - player.transform.position; away.y = 0f;
                    heading = Quaternion.LookRotation(away.sqrMagnitude > .01f ? away : Vector3.forward);
                    GameObject prefab = MakeOutpost(i);
                    var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                    placed.name = "Outpost " + (i + 1).ToString("00") + " - " + generators[i].name;
                    placed.transform.SetPositionAndRotation(origin, heading);
                    report.Add(placed.name + " center=" + origin + " colliders=" + placed.GetComponentsInChildren<Collider>().Length + " obstacles=" + placed.GetComponentsInChildren<NavMeshObstacle>().Length);
                }
                Undo.RegisterCreatedObjectUndo(group, "Add ruined mountain outposts");
                Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save outposts.");
                AssetDatabase.SaveAssets();
                LastReport = string.Join("\n", report);
                Debug.Log("Added mountain outposts.\n" + LastReport);
            }
            catch { Object.DestroyImmediate(group); throw; }
        });
    }

    private static GameObject MakeOutpost(int variant)
    {
        string name = "Ruined Mountain Outpost " + (variant + 1);
        var root = new GameObject(name);
        try
        {
            Transform p = root.transform;
            Wall(p, "West facade A", -25, -15, 9, 11, 90, variant);
            Wall(p, "West facade B", -25, 1, 10, 15, 90, variant + 1);
            Wall(p, "West broken base", -25, 18, 6, 5, 90, variant + 2);
            Wall(p, "East facade A", 25, -16, 10, 6, 90, variant + 1);
            Wall(p, "East facade B", 25, 0, 9, 11, 90, variant + 2);
            Wall(p, "East facade C", 25, 17, 8, 13, 90, variant);
            Wall(p, "Rear broken wall", -18, 22, 11, 13, 0, variant + 2);
            Wall(p, "Rear stair remnant", -7, 22, 5, 6, 0, variant);
            Wall(p, "Front collapsed wall", -18, -22, 11, 5, 0, variant);
            Wall(p, "Front standing wall", 18, -22, 11, 10, 0, variant + 1);
            // A surviving window bay makes the skeleton read as a former building.
            Box(p, "Window sill", At(15, 22, 1.1f), new Vector3(13, 2.2f, .9f), concrete, true);
            Box(p, "Window left jamb", At(9, 22, 4.4f), new Vector3(1, 6.6f, 1), dark, true);
            Box(p, "Window right jamb", At(21, 22, 3.5f), new Vector3(1.1f, 4.8f, 1.1f), concrete, true);
            Beam(p, "Snapped window lintel", At(8.6f, 22, 8), At(17, 22, 7.5f), .65f, pale, true);

            Column(p, -25, -22, 11f + variant); Column(p, 25, -22, 15f - variant);
            Column(p, -25, 22, 20f); Column(p, 25, 22, 9f + variant);
            Column(p, -8, 22, 13f); Column(p, 25, 2, 17f); Column(p, -25, 3, 14f);
            Beam(p, "Ripped rear roof girder", At(-25, 22, 19.6f), At(-13, 22, 18.5f), .8f, dark, false);
            Beam(p, "Roof girder torn end", At(-13, 22, 18.5f), At(-8, 19, 16f), .4f, rust, false);
            Beam(p, "West roof rib", At(-25, 3, 14), At(-15, 3, 11.5f), .7f, dark, false);
            Beam(p, "East roof rib", At(25, 2, 16.6f), At(19, 2, 12.5f), .65f, rust, false);
            Box(p, "Broken upper floor", At(-18, 21f, 6f), new Vector3(10, .4f, 3.6f), pale, true, new Vector3(0, 0, -4));
            Box(p, "Remaining roof shard", At(-19, 21.5f, 12.4f), new Vector3(8, .3f, 3.2f), dark, true, new Vector3(12, 4, -10));
            // Large fallen pieces stay on the perimeter; the courtyard and front entrance stay open.
            Box(p, "Fallen reinforced slab", At(-29, -6, .7f), new Vector3(6, 1.2f, 3.2f), pale, true, new Vector3(9, 24, -13));
            Box(p, "Collapsed column", At(28, 12, 1.2f), new Vector3(1.8f, 8, 1.8f), concrete, true, new Vector3(0, -15, 78));
            Box(p, "Fallen roof panel", At(17, -27, .6f), new Vector3(9, .45f, 4), rust, true, new Vector3(7, -18, 9));
            for (int i = 0; i < 16; i++)
            {
                float a = (i + .35f * variant) * Mathf.PI * 2f / 16f;
                float x = Mathf.Cos(a) * (27f + i % 3), z = Mathf.Sin(a) * (25f + i % 2);
                if (Mathf.Abs(x) < 10f || Mathf.Abs(z) < 6f) continue;
                Box(p, "Rubble " + i, At(x, z, .3f), new Vector3(1.2f + i % 3 * .6f, .5f + i % 2 * .4f, 1.3f), i % 2 == 0 ? concrete : pale, true, new Vector3(i % 3 * 8, i * 37, i % 2 * 12));
            }
            Combine(root, name);
            foreach (Transform child in root.GetComponentsInChildren<Transform>()) child.gameObject.isStatic = true;
            return PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Prefabs/" + name + ".prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static Vector3 At(float x, float z, float height)
    {
        Vector3 world = origin + heading * new Vector3(x, 0f, z);
        var tile = TerrainAt(world);
        return new Vector3(x, tile.SampleHeight(world) + tile.transform.position.y - origin.y + height, z);
    }
    private static Terrain TerrainAt(Vector3 point) => Terrain.activeTerrains.First(t => t.terrainData != null
        && point.x >= t.transform.position.x && point.z >= t.transform.position.z
        && point.x <= t.transform.position.x + t.terrainData.size.x && point.z <= t.transform.position.z + t.terrainData.size.z);
    private static void Wall(Transform parent, string name, float x, float z, float width, float height, float yaw, int variant)
    {
        var wall = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); wall.transform.SetParent(parent, false);
        wall.transform.localPosition = At(x, z, -.35f); wall.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        wall.transform.localScale = new Vector3(width, height, .9f);
        Mesh mesh = wallMeshes[variant % 3]; wall.GetComponent<MeshFilter>().sharedMesh = mesh;
        wall.GetComponent<MeshRenderer>().sharedMaterial = variant % 2 == 0 ? concrete : pale;
        wall.AddComponent<MeshCollider>().sharedMesh = mesh;
        Obstacle(wall, mesh.bounds);
        for (int i = 0; i < 3; i++) {
            Box(wall.transform, "Scorched surface", new Vector3(-.32f + i * .28f, .13f + i * .08f, -.513f), new Vector3(.14f, .05f, .025f), rust, false);
        }
        Beam(parent, "Exposed rebar", At(x, z, height * .68f), At(x + .4f, z - .2f, height * .68f + 2.1f), .075f, rust, false);
    }
    private static void Column(Transform parent, float x, float z, float height)
    {
        Box(parent, "Broken structural column", At(x, z, height * .5f), new Vector3(1.4f, height, 1.4f), dark, true);
        Box(parent, "Foundation collar", At(x, z, .5f), new Vector3(2.1f, 1.1f, 2.1f), concrete, true);
        Box(parent, "Faded hazard band", At(x, z, 2.4f), new Vector3(1.45f, .22f, 1.45f), orange, false);
        Beam(parent, "Bent column reinforcement", At(x, z, height), At(x + .5f, z + .6f, height + 1.4f), .12f, rust, false);
    }
    private static GameObject Box(Transform parent, string name, Vector3 point, Vector3 size, Material material, bool solid, Vector3 angles = default)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name; part.transform.SetParent(parent, false);
        part.transform.localPosition = point; part.transform.localEulerAngles = angles; part.transform.localScale = size;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (solid) Obstacle(part, new Bounds(Vector3.zero, Vector3.one));
        else Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }
    private static void Beam(Transform parent, string name, Vector3 start, Vector3 end, float thickness, Material material, bool solid)
    {
        var beam = Box(parent, name, (start + end) * .5f, new Vector3(thickness, Vector3.Distance(start, end), thickness), material, solid);
        beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
    }
    private static void Obstacle(GameObject part, Bounds bounds)
    {
        var obstacle = part.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = bounds.center; obstacle.size = bounds.size + Vector3.one * .04f;
        obstacle.carving = true; obstacle.carveOnlyStationary = true;
    }
    private static Mesh JaggedWall(int variant)
    {
        float[][] patterns = { new[] { .9f, 1f, .72f, .84f, .48f, .58f, .3f }, new[] { .32f, .55f, .4f, .76f, .6f, 1f, .91f }, new[] { .6f, .74f, 1f, .68f, .83f, .43f, .5f } };
        float[] tops = patterns[variant]; var vertices = new List<Vector3>(); var triangles = new List<int>();
        Action<Vector3, Vector3, Vector3, Vector3> quad = (a, b, c, d) => { int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d }); triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 }); };
        for (int i = 0; i < tops.Length - 1; i++) {
            float x0 = i / 6f - .5f, x1 = (i + 1) / 6f - .5f;
            quad(new Vector3(x0, 0, -.5f), new Vector3(x0, tops[i], -.5f), new Vector3(x1, tops[i + 1], -.5f), new Vector3(x1, 0, -.5f));
            quad(new Vector3(x1, 0, .5f), new Vector3(x1, tops[i + 1], .5f), new Vector3(x0, tops[i], .5f), new Vector3(x0, 0, .5f));
            quad(new Vector3(x0, tops[i], -.5f), new Vector3(x0, tops[i], .5f), new Vector3(x1, tops[i + 1], .5f), new Vector3(x1, tops[i + 1], -.5f));
            quad(new Vector3(x0, 0, .5f), new Vector3(x0, 0, -.5f), new Vector3(x1, 0, -.5f), new Vector3(x1, 0, .5f));
        }
        quad(new Vector3(-.5f, 0, .5f), new Vector3(-.5f, tops[0], .5f), new Vector3(-.5f, tops[0], -.5f), new Vector3(-.5f, 0, -.5f));
        quad(new Vector3(.5f, 0, -.5f), new Vector3(.5f, tops[6], -.5f), new Vector3(.5f, tops[6], .5f), new Vector3(.5f, 0, .5f));
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    private static void Combine(GameObject root, string name)
    {
        var filters = root.GetComponentsInChildren<MeshFilter>();
        var groups = filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
        var meshes = new List<Mesh>();
        foreach (var group in groups) {
            var mesh = new Mesh(); mesh.CombineMeshes(group.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = root.transform.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray(), true, true); meshes.Add(mesh);
        }
        var result = new Mesh(); result.CombineMeshes(meshes.Select(m => new CombineInstance { mesh = m, transform = Matrix4x4.identity }).ToArray(), false, false); result.RecalculateBounds();
        root.AddComponent<MeshFilter>().sharedMesh = SaveMesh(result, name); root.AddComponent<MeshRenderer>().sharedMaterials = groups.Select(g => g.Key).ToArray();
        foreach (var filter in filters) { Object.DestroyImmediate(filter.GetComponent<MeshRenderer>()); Object.DestroyImmediate(filter); }
        foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
    }
    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = Folder + "/Meshes/" + name + ".asset"; mesh.name = name;
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing;
    }
    private static Material Material(string name, Color color)
    {
        string path = Folder + "/Materials/" + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true }; AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", .12f); EditorUtility.SetDirty(mat); return mat;
    }
    private static void EnsureFolder(string path) { if (!AssetDatabase.IsValidFolder(path)) { int i = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, i)); AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1)); } }
    private static EnemyGenerator[] Generators(Scene scene) => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyGenerator>()).OrderBy(g => g.transform.position.y).ToArray();
    private static void GameplayScene(Action<Scene> action)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before authoring.");
        Scene original = SceneManager.GetActiveScene(), scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity"); bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
        try { if (scene.isDirty) throw new Exception("SampleScene has unsaved user edits; save them before authoring."); SceneManager.SetActiveScene(scene); action(scene); }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
    }
}
#endif
