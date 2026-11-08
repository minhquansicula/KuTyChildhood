using System.Linq;
using UnityEngine;

public sealed partial class Scene2HouseholdSimulator
{
    private bool importedTransfer;
    private Transform transferSource, transferReceiver, transferFillCenter, transferSpout;
    private WaterRiceFill scoopFill, sourceFill, receiverFill;
    private float transferSourceRadius, transferTargetRadius, transferTravelHeight;
    private bool transferPouring;
    private static readonly Vector2[] LadleProfile = { new Vector2(0, .006f), new Vector2(.008f, .028f), new Vector2(.017f, .038f), new Vector2(.028f, .045f), new Vector2(.040f, .049f), new Vector2(.046f, .050f) };
    private static readonly Vector2[] CupProfile = { new Vector2(0, .029f), new Vector2(.003f, .034f), new Vector2(.018f, .036f), new Vector2(.047f, .038f), new Vector2(.077f, .040f), new Vector2(.093f, .041f) };
    private static readonly Vector2[] BucketProfile = { new Vector2(0, .109f), new Vector2(.003f, .121f), new Vector2(.021f, .125f), new Vector2(.095f, .131f), new Vector2(.185f, .138f), new Vector2(.275f, .146f), new Vector2(.34f, .151f) };
    private static readonly Vector2[] BasketProfile = { new Vector2(0, .089f), new Vector2(.003f, .108f), new Vector2(.01f, .127f), new Vector2(.026f, .142f), new Vector2(.054f, .156f), new Vector2(.093f, .168f), new Vector2(.132f, .175f), new Vector2(.171f, .179f) };
    private static Transform TransferMarker(Transform root, string suffix) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name.EndsWith("_" + suffix));
    private WaterRiceFill CreateTransferFill(string name, Transform vessel, Transform marker, Vector2[] profile, float height, Material material)
    {
        var root = Group(name, world, Vector3.zero);
        var fill = root.gameObject.AddComponent<WaterRiceFill>(); fill.Configure(vessel, marker, profile, height, material); return fill;
    }
    private void BuildImportedTransfer()
    {
        importedTransfer = true; transferPouring = false;
        bool water = state.chore == HouseholdChore.FetchWater;
        sourceZone = new Vector3(-.34f, .005f, .06f); targetZone = new Vector3(.34f, .005f, .06f);
        transferSource = Art(water ? waterRiceAssets.bucket : waterRiceAssets.basket, water ? "Water bucket" : "Rice basket", world, sourceZone);
        transferReceiver = Art(waterRiceAssets.receiver, "Existing kitchen receiving pot", world, targetZone);
        movingHome = water ? new Vector3(.18f, .06f, -.22f) : new Vector3(.06f, .075f, -.22f);
        moving = Art(water ? waterRiceAssets.ladle : waterRiceAssets.cup, water ? "Coconut ladle" : "Aluminium measuring cup", world, movingHome);
        transferFillCenter = TransferMarker(moving, "FillCenter"); transferSpout = TransferMarker(moving, "PourSpout");
        Material material = water ? waterRiceAssets.waterMaterial : waterRiceAssets.riceMaterial;
        sourceFill = CreateTransferFill("Source contents · profile fitted", transferSource, TransferMarker(transferSource, "ContentsRoot"),
            water ? BucketProfile : BasketProfile, water ? .225f : .111f, material);
        scoopFill = CreateTransferFill("Carried contents · horizontal surface", moving, TransferMarker(moving, "ContentsRoot"),
            water ? LadleProfile : CupProfile, water ? .028f : .067f, material);
        var receiverFloor = Group("Receiver content floor", transferReceiver, waterRiceAssets.receiverFloor);
        receiverFill = CreateTransferFill("Received contents", transferReceiver, receiverFloor,
            new[] { new Vector2(0, waterRiceAssets.receiverRadius * .82f), new Vector2(waterRiceAssets.receiverHeight, waterRiceAssets.receiverRadius) },
            waterRiceAssets.receiverHeight, material);
        content = scoopFill.transform; receiverContent = receiverFill.transform;
        PickVisual(moving); PickVisual(transferSource); PickVisual(transferReceiver);
        transferSourceRadius = water ? .125f : .15f; transferTargetRadius = waterRiceAssets.receiverRadius * .8f;
        transferTravelHeight = Mathf.Max(water ? .56f : .36f, LocalVisualBounds(transferReceiver, world).max.y + .30f);
        pourStream = new GameObject(water ? "Water pour stream" : "Rice pour stream").AddComponent<LineRenderer>();
        pourStream.transform.SetParent(world, false); pourStream.positionCount = 3; pourStream.useWorldSpace = true;
        pourStream.sharedMaterial = material; pourStream.startWidth = water ? .006f : .012f; pourStream.endWidth = water ? .010f : .018f;
        pourStream.generateLightingData = true;
        pourStream.numCapVertices = 3; pourStream.enabled = false;
        dishWorkBounds = LocalVisualBounds(transferSource, world); dishWorkBounds.Encapsulate(LocalVisualBounds(transferReceiver, world));
        dishWorkBounds.Encapsulate(LocalVisualBounds(moving, world)); dishWorkBounds.Encapsulate(new Vector3(.45f, transferTravelHeight + .08f, -.22f));
        dishWorkBounds.Expand(new Vector3(.15f, .1f, .1f)); FrameDishLayout();
        sourceFill.SetLevel(1);
    }
    private void TickImportedTransfer(Vector3 point, int pick, bool down, bool pressed, bool up, float scroll, float dt)
    {
        if (down && pick == 0) { held = 0; tilt = 0; }
        transferPouring = false;
        if (held == 0 && pressed)
        {
            tilt = Mathf.Clamp01(tilt + scroll * .14f);
            // The exported root is the grip. Positive Z tilt lowers its -X pouring lip.
            moving.localRotation = Quaternion.Euler(0, 0, tilt * 100);
            moving.localPosition = new Vector3(point.x, transferTravelHeight, point.z);
            Vector3 mouth = world.InverseTransformPoint(transferFillCenter.position);
            if (InZone(mouth, sourceZone, transferSourceRadius) && tilt < .15f)
            {
                Vector3 position = moving.localPosition;
                position.y += sourceFill.SurfaceY - transferFillCenter.position.y - .012f;
                moving.localPosition = position;
                state.Scoop(dt); Touch(); PlayWater(state.chore == HouseholdChore.FetchWater);
            }
            else if (InZone(world.InverseTransformPoint(transferSpout.position), targetZone, transferTargetRadius) && tilt > .35f && state.fill > 0)
            {
                float before = state.fill; state.Pour(tilt, dt); transferPouring = state.fill < before;
                Touch(); PlayWater(state.chore == HouseholdChore.FetchWater && transferPouring);
            }
            else PlayWater(false);
        }
        if (up)
        {
            if (state.fill > .01f && !state.Complete) Tell("Đưa mép gáo/ca vào miệng nồi rồi lăn chuột để nghiêng đổ.");
            ReleasePointer(); transferPouring = false;
        }
        RefreshVisuals();
    }
    private void RefreshImportedTransfer()
    {
        sourceFill.SetLevel(1); scoopFill.SetLevel(state.fill);
        receiverFill.SetLevel(state.chore == HouseholdChore.FetchWater ? state.transferred : (state.delivered + state.transferred) / 3f);
        bool pouring = transferPouring && held == 0 && tilt > .35f;
        pourStream.enabled = pouring;
        if (pouring)
        {
            Vector3 from = transferSpout.position;
            Vector3 to = world.TransformPoint(targetZone + Vector3.up * .04f); to.y = Mathf.Max(to.y, receiverFill.SurfaceY);
            pourStream.SetPosition(0, from); pourStream.SetPosition(1, Vector3.Lerp(from, to, .5f) + Vector3.down * .02f); pourStream.SetPosition(2, to);
        }
    }
}
