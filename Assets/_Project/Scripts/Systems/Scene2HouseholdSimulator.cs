using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>World-space work surfaces. Story order and rewards remain owned by HouseholdChoreFlow.</summary>
[DisallowMultipleComponent]
public sealed partial class Scene2HouseholdSimulator : MonoBehaviour
{
    [Serializable]
    public sealed class Station
    {
        public HouseholdChore chore;
        public Transform surface;
        public Transform view;
        public Renderer existingFixture;
        public Transform idleDressing;
    }
    [SerializeField] private Station[] stations = new Station[0];
    [SerializeField] private DishwashingAssets dishwashingAssets;
    public DishwashingAssets DishwashingArt => dishwashingAssets;
    public void ConfigureDishwashing(DishwashingAssets assets) => dishwashingAssets = assets;
    [SerializeField] private WaterRiceAssets waterRiceAssets;
    public WaterRiceAssets WaterRiceArt => waterRiceAssets;
    public void ConfigureWaterRice(WaterRiceAssets assets) => waterRiceAssets = assets;
    private readonly Dictionary<HouseholdChore, HouseholdSimulatorState> work = new Dictionary<HouseholdChore, HouseholdSimulatorState>();
    private readonly List<UnityEngine.Object> ownedAssets = new List<UnityEngine.Object>();
    private readonly List<Collider> pickers = new List<Collider>();
    private readonly RaycastHit[] occlusionHits = new RaycastHit[64];
    private readonly List<GameObject> finishedWorlds = new List<GameObject>();
    private HouseholdChoreFlow flow;
    private HouseholdWorkSession session;
    private HouseholdSimulatorState state;
    private Station station;
    private Transform world;
    private Action completed, cancelled;
    private Camera view;
    private GameManager manager;
    private CanvasGroup captions;
    private TextMeshProUGUI title, instructions, hint;
    private float cameraTime, saveTime, idleTime, messageTime;
    private string message;
    private bool dirty;
    private int held = -1;
    private Vector3 lastPointerPoint;
    private bool pointerValid;
    private float tilt;
    private AudioSource loop;
    private AudioClip waterClip;
    public bool IsWorking { get; private set; }
    public HouseholdSimulatorState State => state;

    public void Configure(Station[] authored) => stations = authored ?? new Station[0];

    public bool Handles(HouseholdChore chore)
    {
        if (!isActiveAndEnabled) return false;
        foreach (var item in stations) if (item != null && item.chore == chore && item.surface && item.view) return true;
        return false;
    }

    public bool Begin(HouseholdChore chore, Action onComplete, Action onCancel)
    {
        if (IsWorking || !Handles(chore) || GameManager.Instance?.InputBlocked == true) return false;
        flow = GetComponent<HouseholdChoreFlow>();
        view = Camera.main;
        if (!flow || !flow.IsCurrent(chore) || !view) return false;
        foreach (var item in stations) if (item.chore == chore) { station = item; break; }
        if (!work.TryGetValue(chore, out state))
        {
            state = HouseholdSimulatorState.Restore(chore, Scene2Checkpoint.LoadWork(chore));
            work[chore] = state;
        }
        completed = onComplete; cancelled = onCancel;
        try
        {
            // Never capture another activity's camera or leave two input owners alive.
            session = new HouseholdWorkSession(this, view);
            IsWorking = true;
            manager = GameManager.Instance;
            if (manager) manager.OnPauseChanged += Pause;
            flow.GetComponent<Scene2KitchenHandsOn>()?.SilenceMother();
            flow.Voice?.Stop();
            BuildWorld();
            BuildCaptions();
            cameraTime = saveTime = idleTime = messageTime = 0;
            message = ""; dirty = false;
            ReleasePointer();
            RefreshVisuals();
            return true;
        }
        catch (Exception error)
        {
            Debug.LogException(error, this);
            End();
            return false;
        }
    }

    private void Update()
    {
        if (!IsWorking || !view || state == null) return;
        if (manager && manager.IsPaused) return;
        if (PauseUI.LastResumeFrame == Time.frameCount) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { End(); return; }
        if (!flow || !flow.IsCurrent(state.chore)) { End(); return; }
        cameraTime += Time.deltaTime;
        if (importedDishes)
        {
            if (Application.isFocused && Input.GetKeyDown(KeyCode.F))
                ToggleDishCloseView();
            // Carrying a bowl needs the whole station; scrubbing benefits from a close view.
            dishZoomBlend = Mathf.MoveTowards(dishZoomBlend, dishCloseView && held != 0 ? 1 : 0, Time.deltaTime * 4);
        }
        session.MoveTo(importedDishes ? DishCameraDestination : importedTransfer ? dishCameraPosition : station.view.position,
            importedDishes || importedTransfer ? dishCameraRotation : station.view.rotation, cameraTime / .4f);
        idleTime += Time.deltaTime;
        messageTime = Mathf.Max(0, messageTime - Time.deltaTime);
        if (cameraTime >= .4f && Application.isFocused)
            ProcessPointer(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButton(0),
                Input.GetMouseButtonUp(0), Input.GetMouseButtonDown(1), Input.mouseScrollDelta.y, Time.deltaTime);
        else ReleasePointer();
        RefreshVisuals();
        UpdateCaption();
        saveTime += Time.unscaledDeltaTime;
        if (dirty && saveTime >= 1f) Save();
        if (state.Complete) Complete();
    }

    /// <summary>The same ray/gesture path is used by live input and Play Mode regression tests.</summary>
    public void ProcessPointer(Vector2 screen, bool down, bool pressed, bool up, bool cancel, float scroll, float dt)
    {
        if (!IsWorking || !view || !world || dt <= 0 || manager && manager.IsPaused) return;
        if (cancel) { ReleasePointer(); Tell("Đã đặt vật lại an toàn. Bạn có thể thử tiếp."); return; }
        Ray ray = view.ScreenPointToRay(screen);
        if (down) Physics.SyncTransforms();
        int pick = Pick(ray);
        // Project the sponge onto the exposed bowl face, not an unrelated plane above it.
        float pointerHeight = importedDishes && state.chore == HouseholdChore.WashDishes && (held == 1 || down && pick == 1)
            ? DishContactHeight : .12f;
        var plane = new Plane(world.up, world.TransformPoint(new Vector3(0, pointerHeight, 0)));
        if (!plane.Raycast(ray, out float distance)) { if (up) ReleasePointer(); return; }
        Vector3 point = world.InverseTransformPoint(ray.GetPoint(distance));
        bool artBounds = importedDishes || importedTransfer;
        point.x = Mathf.Clamp(point.x, artBounds ? dishWorkBounds.min.x : -.57f, artBounds ? dishWorkBounds.max.x : .57f);
        point.z = Mathf.Clamp(point.z, artBounds ? dishWorkBounds.min.z : -.32f, artBounds ? dishWorkBounds.max.z : .32f);
        point.y = pointerHeight;
        if (state.chore == HouseholdChore.WashDishes) TickDishes(point, pick, down, pressed, up, scroll, dt);
        else if (state.chore == HouseholdChore.FetchWater || state.chore == HouseholdChore.MeasureRice)
            TickTransfer(point, pick, down, pressed, up, scroll, dt);
        else if (state.chore == HouseholdChore.ReadLetter) TickLetter(pick, down);
        else TickPlacement(point, pick, down, pressed, up, scroll);
        if (down || held >= 0 && pressed) idleTime = 0;
    }

    private int Pick(Ray ray)
    {
        float nearest = 5f;
        int picked = -1;
        for (int i = 0; i < pickers.Count; i++)
        {
            // Basin/rack are drop zones, not tools. Their broad volumes must not
            // steal the click from a visible bowl or sponge behind an open mouth.
            if (importedDishes && i >= 3) continue;
            if (importedTransfer && i >= 1) continue;
            Collider c = pickers[i];
            if (c && c.enabled && c.gameObject.activeInHierarchy && c.Raycast(ray, out var hit, nearest))
            { nearest = hit.distance; picked = i; }
        }
        if (picked >= 0)
        {
            if (importedDishes && fixtureSurfaceQuery && fixtureSurfaceQuery.Raycast(ray, out var exact, nearest) && exact.distance < nearest - .004f)
                return -1;
            int count = Physics.RaycastNonAlloc(ray, occlusionHits, nearest, ~0, QueryTriggerInteraction.Ignore);
            if (count == occlusionHits.Length) return -1;
            for (int i = 0; i < count; i++)
            {
                var hit = occlusionHits[i];
                if (!hit.collider || hit.collider.GetComponentInParent<FirstPersonController>() || hit.transform.IsChildOf(world)) continue;
                if (importedDishes && fixtureSurfaceQuery && hit.collider == fixtureBox) continue;
                if (hit.distance < nearest - .015f) return -1;
            }
        }
        return picked;
    }

    private void TickDishes(Vector3 point, int pick, bool down, bool pressed, bool up, float scroll, float dt)
    {
        // Pick IDs: bowl, sponge, soap, rinse basin, drying rack.
        if (down)
        {
            if (pick == 2)
            {
                state.soaped = state.wetted;
                if (state.wetted) state.rinse = 0;
                Tell(state.wetted ? "Miếng rửa đã có xà phòng." : "Làm ướt chén trong thau trước nhé."); Touch();
            }
            else if (pick == 0 || pick == 1) { held = pick; pointerValid = false; }
        }
        if (Mathf.Abs(scroll) > .01f && held == 0)
        {
            state.flipped = !state.flipped;
            Touch();
            Tell(state.flipped ? "Mặt ngoài của chén." : "Lòng chén.");
        }
        if (held == 0 && pressed)
        {
            moving.localPosition = new Vector3(point.x, importedDishes ? bowlLift : .17f, point.z);
            if (importedDishes ? DishInRinse(point) : InZone(point, rinseZone, .18f))
            {
                if (importedDishes) moving.localPosition = new Vector3(point.x, rinseBowlHeight, point.z);
                state.Rinse(dt); Touch();
                PlayWater(true);
            }
            else PlayWater(false);
        }
        if (held == 1 && pressed)
        {
            tool.localPosition = new Vector3(point.x, importedDishes ? DishContactHeight + .001f : moving.localPosition.y + (state.flipped ? .10f : .04f), point.z);
            float travel = pointerValid ? Vector3.Distance(point, lastPointerPoint) : 0;
            lastPointerPoint = point; pointerValid = true;
            Vector3 relative = point - moving.localPosition;
            if (new Vector2(relative.x, relative.z).magnitude < (importedDishes ? bowlRadius : .135f) && travel > .0002f && (!importedDishes || travel < .07f))
            {
                int patch = importedDishes ? ContactPatch(point) :
                    Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(relative.z, relative.x) / (Mathf.PI * 2), 1f) * 3) + (state.flipped ? 3 : 0);
                if (state.Scrub(patch, travel))
                {
                    Touch();
                    if (Time.time >= nextScrubSound) { AudioManager.Instance?.PlaySFX(SfxId.TaskProgress); nextScrubSound = Time.time + .35f; }
                }
                else if (!state.wetted || !state.soaped) Tell("Làm ướt chén, rồi bấm vào bánh xà phòng trước khi chà.");
            }
        }
        if (up && held == 0)
        {
            if (importedDishes ? DishInRack(point) : InZone(point, rackZone, .18f))
            {
                if (state.RackDish()) { AudioManager.Instance?.PlaySFX(SfxId.DishClean); Tell("Đã đặt chén sạch lên giá."); Touch(); }
                else Tell(state.Scrubbed ? "Tráng sạch bọt trước khi đặt lên giá nhé." : "Chén còn bẩn, nhớ chà cả hai mặt nhé.");
            }
        }
        if (up) ReleasePointer();
        // Keep geometry synchronized immediately, including a new dish after racking. This
        // also makes multiple gestures within a frame observe the same state as live input.
        RefreshVisuals();
    }

    private void TickTransfer(Vector3 point, int pick, bool down, bool pressed, bool up, float scroll, float dt)
    {
        if (importedTransfer) { TickImportedTransfer(point, pick, down, pressed, up, scroll, dt); return; }
        if (down && pick == 0) { held = 0; tilt = 0; }
        if (held == 0 && pressed)
        {
            moving.localPosition = point + Vector3.up * .04f;
            tilt = Mathf.Clamp01(tilt + scroll * .13f);
            if (InZone(point, sourceZone, .2f) && tilt < .15f) { state.Scoop(dt); PlayWater(state.chore == HouseholdChore.FetchWater); Touch(); }
            else if (InZone(point, targetZone, .2f) && tilt > .35f)
            { state.Pour(tilt, dt); PlayWater(state.chore == HouseholdChore.FetchWater && state.fill > 0); Touch(); }
            else PlayWater(false);
            moving.localRotation = Quaternion.Euler(0, 0, -tilt * 100f);
        }
        if (up)
        {
            if (state.fill > 0 && !state.Complete) Tell("Gáo còn đầy. Kéo sang nồi, lăn chuột để nghiêng đổ.");
            ReleasePointer();
        }
    }

    private void TickPlacement(Vector3 point, int pick, bool down, bool pressed, bool up, float scroll)
    {
        if (down && pick >= 0 && pick < 3 && (state.placedMask & (1 << pick)) == 0) held = pick;
        if (held >= 0 && pressed)
        {
            pieces[held].localPosition = point + Vector3.up * .03f;
            pieces[held].Rotate(Vector3.up, scroll * 15f, Space.Self);
        }
        if (up && held >= 0)
        {
            int item = held;
            if (state.Place(item, InZone(point, slots[item], .16f)))
            { AudioManager.Instance?.PlaySFX(SfxId.TaskProgress); Touch(); Tell("Đã đặt gọn vào chỗ."); }
            else Tell("Đặt vào dấu cùng loại; chuột phải để trả vật về.");
            ReleasePointer();
        }
    }

    private void TickLetter(int pick, bool down)
    {
        if (!down || pick != 0) return;
        // Reading is deliberate, not a timing challenge or click-spam reward.
        if (state.letterStep < 2) { state.letterStep++; letterReadSeconds = 0; Touch(); }
        else if (letterReadSeconds >= 4f) { state.letterStep = 3; Touch(); }
    }

    private bool InZone(Vector3 point, Vector3 zone, float radius) =>
        new Vector2(point.x - zone.x, point.z - zone.z).sqrMagnitude <= radius * radius;

    private void ReleasePointer()
    {
        held = -1; pointerValid = false; tilt = 0;
        if (moving) { moving.localPosition = movingHome; moving.localRotation = Quaternion.identity; }
        if (tool) { tool.localPosition = toolHome; tool.localRotation = Quaternion.identity; }
        for (int i = 0; i < pieces.Length; i++) if (pieces[i])
            pieces[i].localPosition = state != null && (state.placedMask & (1 << i)) != 0 ? slots[i] : homes[i];
        PlayWater(false);
    }

    private void Touch() { dirty = true; idleTime = 0; }
    private void Tell(string text) { if (message == text && messageTime > 0) return; message = text; messageTime = 3f; }
    private void Save()
    {
        if (state != null && flow && flow.CurrentChore == state.chore && !state.Complete)
            Scene2Checkpoint.SaveWork(state.chore, JsonUtility.ToJson(state));
        dirty = false; saveTime = 0;
    }

    private void Complete()
    {
        Action callback = completed;
        Close(false);
        callback?.Invoke();
    }

    public void End()
    {
        if (!IsWorking) return;
        Action callback = cancelled;
        Save();
        Close(true);
        callback?.Invoke();
    }

    public void ResetWork()
    {
        End();
        work.Clear(); state = null;
        foreach (var item in stations) if (item != null && item.idleDressing) item.idleDressing.gameObject.SetActive(true);
        foreach (var go in finishedWorlds) if (go) Destroy(go);
        finishedWorlds.Clear();
    }

    private void Close(bool cancel)
    {
        IsWorking = false;
        ReleasePointer();
        if (manager) manager.OnPauseChanged -= Pause;
        session?.Close(); session = null;
        if (captions) captions.alpha = 0;
        completed = cancelled = null;
        if (!cancel && world) finishedWorlds.Add(world.gameObject);
        else if (world) Destroy(world.gameObject);
        if (cancel && station != null && station.idleDressing) station.idleDressing.gameObject.SetActive(true);
        world = null;
    }

    private void Pause(bool paused)
    {
        if (!IsWorking) return;
        ReleasePointer();
        if (loop) loop.Stop();
    }
    private void OnApplicationFocus(bool focused) { if (!focused && IsWorking) { ReleasePointer(); Save(); } }
    private void OnDisable() => End();
    private void OnDestroy()
    {
        End();
        foreach (var asset in ownedAssets) if (asset) Destroy(asset);
        foreach (var go in finishedWorlds) if (go) Destroy(go);
        if (captions) Destroy(captions.gameObject);
    }
    private void PlayWater(bool playing)
    {
        if (!loop) return;
        if (playing && !loop.isPlaying && loop.clip) loop.Play();
        else if (!playing && loop.isPlaying) loop.Stop();
    }

    private void BuildCaptions()
    {
        if (captions) { captions.alpha = 1; return; }
        var go = new GameObject("Household simulator instructions", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        go.GetComponent<Canvas>().sortingOrder = 67;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        captions = go.GetComponent<CanvasGroup>(); captions.blocksRaycasts = captions.interactable = false;
        title = Label(go.transform, "Activity", new Vector2(0, -32), new Vector2(1100, 56), true, 25);
        instructions = Label(go.transform, "Gesture", new Vector2(0, 26), new Vector2(1300, 102), false, 22);
        hint = Label(go.transform, "Context", new Vector2(0, 134), new Vector2(1300, 52), false, 22);
    }
    private TextMeshProUGUI Label(Transform parent, string name, Vector2 pos, Vector2 size, bool top, float fontSize)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
        var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, top ? 1 : 0);
        rect.anchoredPosition = pos; rect.sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(.035f, .04f, .035f, .88f); panel.GetComponent<Image>().raycastTarget = false;
        var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(panel.transform, false);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(18, 8); label.rectTransform.offsetMax = new Vector2(-18, -8);
        if (GameUiTheme.Current) label.font = GameUiTheme.Current.font;
        label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.color = new Color(.98f, .93f, .8f);
        return label;
    }
    private void UpdateCaption()
    {
        if (!title) return;
        title.text = HouseholdChoreFlow.Titles[(int)state.chore] + " · Thao tác trực tiếp";
        string text;
        switch (state.chore)
        {
            case HouseholdChore.WashDishes:
                text = "Chén " + Mathf.Min(3, state.delivered + 1) + "/3 · " + (!state.wetted ? "Kéo chén vào thau để làm ướt" :
                    !state.soaped ? "Bấm bánh xà phòng" : !state.Scrubbed ? "Kéo miếng rửa qua các vết bẩn; cầm chén + lăn chuột để lật mặt" :
                    state.rinse < 1 ? "Kéo chén vào thau, giữ để tráng sạch bọt · " + Mathf.RoundToInt(state.rinse * 100) + "%" : "Kéo chén sạch lên giá rồi thả chuột");
                if (state.wetted && state.soaped && !state.Scrubbed)
                {
                    float remaining = 0; int offset = state.flipped ? 3 : 0;
                    for (int i = offset; i < offset + 3; i++) remaining += state.stains[i];
                    text += "\n" + (state.flipped ? "Mặt ngoài" : "Lòng chén") + " · Sạch " + Mathf.RoundToInt((1 - remaining / 3) * 100) + "%";
                    if (remaining <= .06f) text += " · Mặt này sạch rồi — cầm chén và lăn chuột để lật mặt";
                }
                break;
            case HouseholdChore.FetchWater:
            case HouseholdChore.MeasureRice:
                text = (importedTransfer ?
                    (state.chore == HouseholdChore.FetchWater ? "Giữ chuột trái cầm gáo dừa · Đưa lòng gáo vào thùng nước" : "Giữ chuột trái cầm ca nhôm · Đưa lòng ca vào thúng gạo") +
                    " · Đưa mép đổ vào miệng nồi rồi lăn chuột\n" :
                    "Giữ chuột trái cầm gáo · Đưa vào thau bên trái để múc · Kéo sang nồi bên phải và lăn chuột để nghiêng đổ\n") +
                    (state.chore == HouseholdChore.FetchWater ? "Nước trong nồi: " + Mathf.RoundToInt(state.transferred * 100) + "%" :
                    "Đã đong " + state.delivered + "/3 ca gạo") + " · Gáo: " + Mathf.RoundToInt(state.fill * 100) + "%"; break;
            case HouseholdChore.ReadLetter:
                if (state.letterStep == 2) letterReadSeconds += Time.deltaTime;
                text = state.letterStep == 0 ? "Bấm vào phong bì để mở." : state.letterStep == 1 ? "Bấm lá thư để mở nếp giấy." :
                    "“" + HouseholdChoreFlow.Memories[(int)HouseholdChore.ReadLetter] + "”\n" +
                    (letterReadSeconds < 4 ? "Đọc thong thả…" : "Bấm lá thư khi bạn đã đọc xong."); break;
            default: text = "Giữ chuột trái để nhấc đồ · Kéo vào dấu cùng loại rồi thả · Lăn chuột để xoay\n" +
                "Đã xếp " + (((state.placedMask & 1) != 0 ? 1 : 0) + ((state.placedMask & 2) != 0 ? 1 : 0) + ((state.placedMask & 4) != 0 ? 1 : 0)) + "/3 món"; break;
        }
        instructions.text = text;
        hint.text = messageTime > 0 ? message : idleTime > 9 ? "Chạm đúng vật để thao tác; chuột phải trả vật về. Tiến độ được giữ khi rời đi." :
            "CHUỘT PHẢI  Đặt lại an toàn     ·     ESC  Rời hoạt động, giữ tiến độ";
        if (importedDishes) hint.text += "\nF  Cầm miếng rửa và nhìn gần / trở về toàn cảnh · Giữ chuột trái để chà";
    }
}
