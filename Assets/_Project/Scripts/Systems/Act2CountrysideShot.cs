using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal static class Act2CountrysideShot
{
    private static readonly Vector3 Origin = new Vector3(0f, 2048f, 0f);

    internal static IEnumerator Play()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) yield break;
        var root = new GameObject("Countryside dream");
        root.transform.position = Origin;
        var materials = new Dictionary<Color, Material>();
        var target = new RenderTexture(960, 540, 24);
        try
        {
            var camera = new GameObject("Dream camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.66f, .79f, .76f);
            camera.farClipPlane = 85f;
            camera.fieldOfView = 50f;
            camera.targetTexture = target;
            camera.allowHDR = false;

            var canvas = new GameObject("Dream screen", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(root.transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;
            var image = new GameObject("Dream picture", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(canvas.transform, false);
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.texture = target;
            image.raycastTarget = false;
            var ambience = root.AddComponent<AudioSource>();
            ambience.clip = Resources.Load<AudioClip>("Cutscenes/CountrysideAmbience");
            ambience.volume = .6f;
            KuTyAudioMix.Route(ambience, KuTyAudioBus.Ambience);

            Color earth = new Color(.70f, .48f, .30f);
            Color green = new Color(.37f, .50f, .24f);
            Color gold = new Color(.65f, .66f, .28f);
            Color brown = new Color(.31f, .22f, .14f);
            Color leaf = new Color(.29f, .42f, .24f);
            Block(new Vector3(0f, -.22f, 22f), new Vector3(90f, .4f, 80f), green);
            for (int row = 0; row < 12; row++)
            {
                float z = row * 2.4f;
                float bend = Mathf.Sin(z * .12f) * 1.7f;
                Block(new Vector3(bend, .01f, z), new Vector3(2.15f, .045f, 2.65f), earth, -Mathf.Cos(z * .12f) * 10f);
                Block(new Vector3(bend - 9.2f, .01f, z), new Vector3(15.8f, .035f, 2.2f), row % 2 == 0 ? gold : green);
                Block(new Vector3(bend + 10f, .015f, z), new Vector3(17.1f, .035f, 2.2f), row % 2 == 0 ? green : gold);
            }
            var random = new System.Random(32);
            var rice = new List<Transform>();
            for (int i = 0; i < 125; i++)
            {
                float z = 1f + (float)random.NextDouble() * 28f;
                float x = (2f + (float)random.NextDouble() * 12f) * (i % 2 == 0 ? -1f : 1f);
                rice.Add(Block(new Vector3(x, .19f, z), new Vector3(.065f, .36f, .09f), i % 3 == 0 ? gold : leaf));
            }
            for (int i = 0; i < 8; i++)
            {
                float z = i * .77f + .7f;
                Block(new Vector3(-1.45f, .38f, z), new Vector3(.09f, .76f, .09f), brown, i * 3f);
                Block(new Vector3(-1.45f, .45f, z + .34f), new Vector3(.065f, .055f, .80f), earth);
                Block(new Vector3(-1.45f, .24f, z + .34f), new Vector3(.065f, .055f, .80f), brown);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = -12f + i * 7f;
                float z = 29f + (i % 2) * 4f;
                Block(new Vector3(x, .9f, z), new Vector3(3.7f, 1.8f, 2.8f), new Color(.73f, .62f, .43f));
                Color tile = new Color(.47f, .27f, .19f);
                Block(new Vector3(x, 1.96f, z - .76f), new Vector3(4.2f, .15f, 1.94f), tile, pitch: -24f);
                Block(new Vector3(x, 1.96f, z + .76f), new Vector3(4.2f, .15f, 1.94f), tile, pitch: 24f);
                Block(new Vector3(x, .58f, z - 1.42f), new Vector3(.65f, 1.16f, .05f), brown);
            }
            for (int i = 0; i < 16; i++)
            {
                float x = -29f + i * 3.8f;
                float z = 35f + Mathf.Sin(i * 3.1f) * 4f;
                Block(new Vector3(x, 1f, z), new Vector3(.28f, 2f, .28f), brown);
                Block(new Vector3(x, 2.8f, z), new Vector3(4f, 3.7f, 4f), i % 2 == 0 ? green : leaf, shape: PrimitiveType.Sphere);
            }
            for (int i = 0; i < 6; i++)
            {
                float x = 2.2f + i * .11f;
                Block(new Vector3(x, 1.5f, 3.3f), new Vector3(.06f, 1.5f + .12f * i, .06f), leaf,
                    shape: PrimitiveType.Cylinder).localRotation = Quaternion.Euler(0f, 0f, -5f + i * 2f);
                Block(new Vector3(x - .3f, 2.55f + .09f * i, 3.3f), new Vector3(.70f, .085f, .32f), green,
                    shape: PrimitiveType.Sphere).localRotation = Quaternion.Euler(0f, 0f, 20f);
            }

            if (ambience.clip != null) ambience.Play();
            for (float elapsed = 0f; elapsed < 5.5f; elapsed += Time.deltaTime)
            {
                if (Time.timeScale == 0f && ambience.isPlaying) ambience.Pause();
                else if (Time.timeScale > 0f && !ambience.isPlaying) ambience.UnPause();
                float t = elapsed / 5.5f;
                camera.transform.localPosition = new Vector3(Mathf.Lerp(-.28f, .28f, t), 1.24f, Mathf.Lerp(-2.7f, .65f, t));
                camera.transform.localRotation = Quaternion.Euler(1.5f, Mathf.Lerp(-3f, 4f, t), 0f);
                for (int i = 0; i < rice.Count; i++)
                    rice[i].localRotation = Quaternion.Euler(0f, 0f, 7f * Mathf.Sin(elapsed * 2f + i * .42f));
                float fade = Mathf.Min(Mathf.Clamp01(elapsed / .55f), Mathf.Clamp01((5.5f - elapsed) / .55f));
                image.color = new Color(fade, fade, fade);
                yield return null;
            }
        }
        finally
        {
            Object.Destroy(root);
            Object.Destroy(target);
            foreach (var material in materials.Values) Object.Destroy(material);
        }

        Transform Block(Vector3 position, Vector3 scale, Color color, float yaw = 0f, float pitch = 0f,
            PrimitiveType shape = PrimitiveType.Cube)
        {
            if (!materials.TryGetValue(color, out var material))
            {
                material = new Material(shader) { color = color };
                materials.Add(color, material);
            }
            var part = GameObject.CreatePrimitive(shape);
            part.GetComponent<Collider>().enabled = false;
            part.GetComponent<Renderer>().sharedMaterial = material;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            part.transform.localScale = scale;
            return part.transform;
        }
    }
}
