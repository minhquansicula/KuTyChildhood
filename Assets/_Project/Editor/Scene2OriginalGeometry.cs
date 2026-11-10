using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Copies complete original triangles/UVs only. Never synthesizes replacement prop geometry.</summary>
public static class Scene2OriginalGeometry
{
    [Serializable] class Survey { public Vector3[] vertices; public int[] triangles; public Vector2[] uv; }
    [MenuItem("KuTy/Existing assets/Export original topology for articulation survey")]
    public static void ExportTopology()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Saved Edit Mode required; survey never changes scene.");
        foreach (string name in new[] { "beplua_naucom", "StovePotVisual" })
        {
            var t = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(x => x.name == name);
            var mesh = t.GetComponent<MeshFilter>().sharedMesh;
            File.WriteAllText(Scene2ExistingAssetReview.Output + "/" + name + "-topology.json", JsonUtility.ToJson(new Survey { vertices = mesh.vertices.Select(t.TransformPoint).ToArray(), triangles = mesh.triangles, uv = mesh.uv }));
        }
        Debug.Log("[ExistingAssets] Original world-space topology exported; scene untouched.");
    }
    public static Mesh Triangles(Mesh source, Func<int, int, int, bool> keep)
    {
        var input = source.triangles;
        var map = new Dictionary<int, int>();
        var output = new List<int>();
        var order = new List<int>();
        for (int i = 0; i < input.Length; i += 3)
        {
            if (!keep(input[i], input[i + 1], input[i + 2])) continue;
            for (int j = 0; j < 3; j++)
            {
                int original = input[i + j];
                if (!map.TryGetValue(original, out int index)) { index = order.Count; map.Add(original, index); order.Add(original); }
                output.Add(index);
            }
        }
        var result = new Mesh { indexFormat = IndexFormat.UInt32 };
        var positions = source.vertices; var normals = source.normals; var uv = source.uv; var tangents = source.tangents;
        result.vertices = order.Select(i => positions[i]).ToArray();
        if (normals.Length == positions.Length) result.normals = order.Select(i => normals[i]).ToArray();
        if (uv.Length == positions.Length) result.uv = order.Select(i => uv[i]).ToArray();
        if (tangents.Length == positions.Length) result.tangents = order.Select(i => tangents[i]).ToArray();
        result.triangles = output.ToArray(); result.RecalculateBounds();
        return result;
    }
    [MenuItem("KuTy/Existing assets/Preview tilted source lid segmentation")]
    public static void PreviewTiltedLid()
    {
        var source = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "beplua_naucom");
        var mesh = source.GetComponent<MeshFilter>().sharedMesh;
        var normal = new Vector3(-.28181923f, .94260713f, .17908021f);
        var distances = mesh.vertices.Select(v => Vector3.Dot(normal, source.TransformPoint(v)) - 1.6015695f).ToArray();
        foreach (float threshold in new[] { -.060f, -.045f, -.035f, -.025f })
        {
            var selected = Triangles(mesh, (a,b,c) => (distances[a] + distances[b] + distances[c]) / 3f > threshold);
            var groups = Scene2ExistingAssetReview.ConnectedParts(selected.vertices, selected.triangles, source.localToWorldMatrix);
            var included = new HashSet<int>(groups[0]);
            var lid = Triangles(selected, (a,b,c) => included.Contains(a));
            string suffix = threshold.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            Render(source, lid, "tilted-lid-" + suffix + ".png");
            Render(source, lid, "tilted-lid-under-" + suffix + ".png", new Vector3(1,-1,1));
            UnityEngine.Object.DestroyImmediate(lid);
            UnityEngine.Object.DestroyImmediate(selected);
        }
        Debug.Log("[ExistingAssets] Tilted-plane source-only previews rendered. No asset or scene binding changed.");
    }
    [MenuItem("KuTy/Existing assets/Validate original kitchen articulation candidate")]
    public static void ValidateCandidate()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != Scene2MotherHomeSetup.Home)
            throw new InvalidOperationException("Saved Home Edit Mode required; no scene edits allowed.");
        var poses = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .ToDictionary(t => t, t => (t.localPosition,t.localRotation,t.localScale,t.parent));
        var source = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "beplua_naucom");
        var mesh = source.GetComponent<MeshFilter>().sharedMesh;
        if (AssetDatabase.GetAssetPath(mesh) != "Assets/_Project/Art/Scene2/Models/Kitchen/SM_RiceCookingStove.fbx")
            throw new InvalidOperationException("Candidate segmentation is reviewed only for the original rice stove; source changed.");
        // Renderer.bounds rotates the local AABB and is larger than actual geometry.
        // Match the same transformed vertex bounds used by the connected-part survey.
        var bounds = new Bounds(source.TransformPoint(mesh.vertices[0]),Vector3.zero);
        foreach(var vertex in mesh.vertices) bounds.Encapsulate(source.TransformPoint(vertex));
        if (mesh.vertexCount != 551517 || Vector3.Distance(bounds.center,new Vector3(-6.0745f,.5869f,-6.4259f)) > .001f ||
            Vector3.Distance(bounds.extents,new Vector3(.5492f,.5849f,.6031f)) > .001f)
            throw new InvalidOperationException("Source geometry/pose changed since this world-plane survey. Resurvey; never move user props to match it.");
        if (mesh.subMeshCount != 1) throw new InvalidOperationException("This reviewed source has one material slot; do not flatten a different source.");
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        var normal = new Vector3(-.28181923f, .94260713f, .17908021f);
        var distance = vertices.Select(v => Vector3.Dot(normal, source.TransformPoint(v)) - 1.6015695f).ToArray();
        bool Above(int a, int b, int c) => (distance[a]+distance[b]+distance[c])/3f > -.018f;
        var above = new List<int>();
        for (int i=0; i<triangles.Length; i+=3)
            if (Above(triangles[i],triangles[i+1],triangles[i+2])) { above.Add(triangles[i]); above.Add(triangles[i+1]); above.Add(triangles[i+2]); }
        var lidVertices = new HashSet<int>(Scene2ExistingAssetReview.ConnectedParts(vertices, above.ToArray(), source.localToWorldMatrix)[0]);
        var groups = Scene2ExistingAssetReview.ConnectedParts(vertices, triangles, source.localToWorldMatrix);
        var fuel = groups.Skip(2).Take(3).Select(g => new HashSet<int>(g)).ToArray();
        var labels = new int[triangles.Length/3];
        var count = new int[5];
        for(int i=0; i<triangles.Length; i+=3)
        {
            int a=triangles[i], b=triangles[i+1], c=triangles[i+2];
            int label = Above(a,b,c) && lidVertices.Contains(a) ? 1 : 0;
            for(int f=0; f<fuel.Length; f++) if(fuel[f].Contains(a)) label=f+2;
            labels[i/3]=label; count[label]++;
        }
        var log = new System.Text.StringBuilder();
        log.AppendLine("SOURCE " + AssetDatabase.GetAssetPath(mesh) + ":" + mesh.name);
        log.AppendLine("Candidate ONLY: no gameplay binding or source FBX writes.");
        log.AppendLine("All triangles assigned exactly once; no new vertices, caps, UVs or replacement props.");
        var pieces = new List<Mesh>();
        var sourceEdges = EdgeCounts(vertices,triangles,source.localToWorldMatrix);
        try
        {
            for(int label=0; label<5; label++)
            {
                int triangleIndex=0, wanted=label;
                var piece=Triangles(mesh,(a,b,c)=>labels[triangleIndex++]==wanted);
                piece.name = new[] { "Original body", "Original lid", "Original fuel 1", "Original fuel 2", "Original fuel 3" }[label];
                pieces.Add(piece);
                if(piece.triangles.Length != count[label]*3 || count[label]==0) throw new InvalidOperationException("Empty or incomplete partition.");
                log.AppendLine(piece.name + ": triangles=" + count[label] + ", vertices=" + piece.vertexCount);
                var pieceEdges = EdgeCounts(piece.vertices,piece.triangles,source.localToWorldMatrix);
                int opened = pieceEdges.Count(e => e.Value == 1 && sourceEdges.TryGetValue(e.Key,out int incidence) && incidence == 2);
                log.AppendLine("  newly exposed source-manifold edges=" + opened + (opened > 0 ? " (OPEN SEAM; do not bind as a finished articulated prop)" : ""));
                Render(source,piece,"candidate-part-"+label+".png");
            }
            if(count.Sum()*3 != triangles.Length) throw new InvalidOperationException("Triangle conservation failed.");
            foreach(var direction in new[] { new Vector3(1.3f,1.1f,1.5f), new Vector3(-1.3f,.7f,-1.5f), new Vector3(1,-1,1) })
            {
                string side=direction.y<0?"underside":direction.x<0?"rear":"front";
                Render(source,pieces[1],"candidate-lid-"+side+".png",direction);
                Render(source,pieces[0],"candidate-body-"+side+".png",direction);
            }
            log.AppendLine("PASS triangle conservation; source position/normal/tangent/UV copied by index.");
            log.AppendLine("FAIL VISUAL: underside has missing bands at lid/pot contact; lowering plane captures pot rim/handle. Upper-view silhouette alone is not acceptance.");
            log.AppendLine("NOT PASSED: closed lid pose, source-backed fire controls, pointer/runtime regressions. LightFire/CookRice still use previous BlenderKitchen bindings.");
            foreach(var pose in poses)
                if(!pose.Key || pose.Key.localPosition!=pose.Value.Item1 || pose.Key.localRotation!=pose.Value.Item2 || pose.Key.localScale!=pose.Value.Item3 || pose.Key.parent!=pose.Value.Item4)
                    throw new InvalidOperationException("Survey altered an existing pose: " + pose.Key);
            log.AppendLine("PASS preserved " + poses.Count + " existing transform local poses/parents; no source renderer bindings edited.");
            File.WriteAllText(Scene2ExistingAssetReview.Output+"/candidate-validation.txt",log.ToString());
        }
        finally { foreach(var piece in pieces) UnityEngine.Object.DestroyImmediate(piece); }
        if(scene.isDirty) throw new InvalidOperationException("Survey unexpectedly dirtied Home.");
        Debug.Log("[ExistingAssets] Candidate partition validated and multi-angle previews written. Home untouched.");
    }
    // Compare welded source edges, not the UV-split vertex indices. A source-manifold
    // edge with one surviving face is a new open seam, even when the top looks complete.
    static Dictionary<(Vector3Int,Vector3Int),int> EdgeCounts(Vector3[] vertices,int[] triangles,Matrix4x4 matrix)
    {
        var points=vertices.Select(v=>
        {
            var p=matrix.MultiplyPoint3x4(v);
            return new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
        }).ToArray();
        var edges=new Dictionary<(Vector3Int,Vector3Int),int>();
        int Compare(Vector3Int a,Vector3Int b) => a.x!=b.x?a.x.CompareTo(b.x):a.y!=b.y?a.y.CompareTo(b.y):a.z.CompareTo(b.z);
        void Edge(int i,int j)
        {
            var a=points[i]; var b=points[j];
            if(a==b) return;
            var key=Compare(a,b)<0?(a,b):(b,a);
            edges.TryGetValue(key,out int count); edges[key]=count+1;
        }
        for(int i=0;i<triangles.Length;i+=3)
        { Edge(triangles[i],triangles[i+1]); Edge(triangles[i+1],triangles[i+2]); Edge(triangles[i+2],triangles[i]); }
        return edges;
    }
    [MenuItem("KuTy/Existing assets/Preview original lid and connected fuel parts")]
    public static void PreviewParts()
    {
        var source = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "beplua_naucom");
        var mesh = source.GetComponent<MeshFilter>().sharedMesh;
        var positions = mesh.vertices;
        var groups = Scene2ExistingAssetReview.ConnectedParts(positions, mesh.triangles, source.localToWorldMatrix);
        for (int i = 1; i < groups.Length; i++)
        {
            var included = new HashSet<int>(groups[i]);
            var part = Triangles(mesh, (a,b,c) => included.Contains(a));
            Render(source, part, "original-connected-" + i + ".png");
            UnityEngine.Object.DestroyImmediate(part);
        }
        foreach (float threshold in new[] { .97f, 1.0f, 1.02f })
        {
            var heights = positions.Select(v => source.TransformPoint(v).y).ToArray();
            var part = Triangles(mesh, (a,b,c) => (heights[a] + heights[b] + heights[c]) / 3f >= threshold);
            Render(source, part, "original-lid-" + threshold.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ".png");
            UnityEngine.Object.DestroyImmediate(part);
        }
        var closed = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "StovePotVisual");
        var closedMesh = closed.GetComponent<MeshFilter>().sharedMesh;
        var y = closedMesh.vertices.Select(v => closed.TransformPoint(v).y).ToArray();
        foreach (float threshold in new[] { .965f, .985f, 1.005f })
        {
            var part = Triangles(closedMesh, (a,b,c) => (y[a]+y[b]+y[c])/3f >= threshold);
            Render(closed, part, "closed-lid-" + threshold.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + ".png");
            UnityEngine.Object.DestroyImmediate(part);
        }
    }
    public static void Render(Transform source, Mesh mesh, string file, Vector3? direction = null)
    {
        var go = new GameObject("Source geometry preview");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<Renderer>().sharedMaterials;
        go.transform.SetPositionAndRotation(source.position, source.rotation); go.transform.localScale = source.lossyScale;
        var preview = new PreviewRenderUtility();
        try
        {
            preview.AddSingleGO(go);
            var bounds = go.GetComponent<Renderer>().bounds;
            float extent = bounds.extents.magnitude;
            preview.camera.nearClipPlane = .001f; preview.camera.farClipPlane = 50; preview.camera.fieldOfView = 40;
            preview.camera.transform.position = bounds.center + (direction ?? new Vector3(1.3f, 1.1f, 1.5f)).normalized * extent * 3.2f;
            preview.camera.transform.LookAt(bounds.center);
            preview.lights[0].intensity = 1.5f; preview.lights[0].transform.rotation = Quaternion.Euler(45,-30,0);
            preview.lights[1].intensity = 1; preview.lights[1].transform.rotation = Quaternion.Euler(30,140,0);
            preview.BeginStaticPreview(new Rect(0,0,900,900)); preview.camera.Render();
            var image = preview.EndStaticPreview();
            File.WriteAllBytes(Scene2ExistingAssetReview.Output + "/" + file, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally { preview.Cleanup(); }
    }
}
