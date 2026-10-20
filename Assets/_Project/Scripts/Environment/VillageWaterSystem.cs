using System;
using UnityEngine;

/// <summary>One contact sampler and one pooled spray system for all village water.</summary>
public sealed class VillageWaterSystem : MonoBehaviour
{
    [Serializable]
    public struct Surface
    {
        public Renderer renderer;
        public Vector4 rectangle; // minX, minZ, maxX, maxZ
        public float height;
        public bool canal;
        public bool meandering;
        public bool shoreMask;
        public Vector2[] outline; // canonical X/Z; reject the dry rounded corners before sampling contacts
        public bool Contains(Vector3 feet)
        {
            if (meandering) feet = VillageLandscapeShape.Unwarp(feet);
            if(feet.x < rectangle.x || feet.x > rectangle.z || feet.z < rectangle.y || feet.z > rectangle.w ||
                feet.y > height + .06f || feet.y < height - .8f) return false;
            if(outline==null || outline.Length<3) return true;
            bool inside=false;
            for(int i=0,j=outline.Length-1;i<outline.Length;j=i++)
            {
                var a=outline[i];var b=outline[j];
                if((a.y>feet.z)!=(b.y>feet.z) && feet.x<(b.x-a.x)*(feet.z-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            return inside;
        }
    }
    public Surface[] surfaces = Array.Empty<Surface>();
    public FirstPersonController player;
    public ParticleSystem spray;
    public ReflectionProbe reflectionProbe;
    public const int RippleCapacity = 8;
    public int RippleEvents { get; private set; }
    public int SplashEvents { get; private set; }
    public int ActiveSurface { get; private set; } = -1;
    private readonly Vector4[] origins = new Vector4[RippleCapacity]; // x, surfaceY, z, startTime
    private readonly Vector4[] parameters = new Vector4[RippleCapacity]; // strength, surfaceIndex, lifetime, speed
    private readonly System.Random random = new System.Random(4217);
    private static readonly int OriginsId = Shader.PropertyToID("_VillageWaterRipples");
    private static readonly int ParametersId = Shader.PropertyToID("_VillageWaterRippleParams");
    private static readonly int CountId = Shader.PropertyToID("_VillageWaterRippleCount");
    private CharacterController character;
    private Vector3 lastFeet, lastStep;
    private float lastStepTime;
    private bool havePrevious;
    private int cursor, count;

    private void OnEnable()
    {
        // Property blocks are transient; rebuild them when the scene/domain loads.
        var block=new MaterialPropertyBlock();
        for(var i=0;i<surfaces.Length;i++)
        {
            if(!surfaces[i].renderer) continue;
            block.Clear();block.SetFloat("_SurfaceIndex",i);block.SetVector("_SurfaceBounds",surfaces[i].rectangle);
            block.SetFloat("_SurfaceMeander",surfaces[i].meandering ? 1 : 0);
            block.SetFloat("_UseShoreMask",surfaces[i].shoreMask ? 1 : 0);
            surfaces[i].renderer.SetPropertyBlock(block);
        }
    }

    private void Start()
    {
        if(player) character = player.GetComponent<CharacterController>();
        ClearInteraction();
        // Capture the static environment once, not a second camera every frame.
        if(reflectionProbe) reflectionProbe.RenderProbe();
    }
    private void LateUpdate()
    {
        if(!player || !character || !character.enabled) return;
        bool locked = player.IsMovementLocked || player.LockWalkingOnly ||
            (GameManager.Instance != null && (GameManager.Instance.InputBlocked || GameManager.Instance.IsPaused));
        ProcessContact(new Vector3(player.transform.position.x,character.bounds.min.y,player.transform.position.z),
            player.transform.right,player.VerticalSpeed,Time.time,!locked);
        ExpireRipples(Time.time);
    }
    public int FindSurface(Vector3 feet)
    {
        for(var i=0;i<surfaces.Length;i++) if(surfaces[i].Contains(feet)) return i;
        return -1;
    }
    // Public deterministic entry point used by the editor's contact/landing verification.
    public void ProcessContact(Vector3 feet,Vector3 right,float verticalSpeed,float now,bool allowInteraction=true)
    {
        int surface = allowInteraction ? FindSurface(feet) : -1;
        if(player) player.WaterSpeedMultiplier = surface < 0 ? 1 : surfaces[surface].canal ? .7f : .8f;
        if(surface >= 0)
        {
            var data = surfaces[surface];
            var point = new Vector3(feet.x,data.height,feet.z);
            bool entered = surface != ActiveSurface;
            // Teleports/reset do not cause a giant landing splash.
            bool continuous = havePrevious && (feet-lastFeet).sqrMagnitude < 9;
            if(entered)
            {
                bool landing = continuous && verticalSpeed < -2.8f && lastFeet.y > data.height + .035f;
                float strength = landing ? Mathf.Clamp(-verticalSpeed*.17f,.65f,1.5f) : .35f;
                AddRipple(point,surface,strength,now);
                if(landing) EmitSplash(point,strength,data.canal);
                lastStep = feet;lastStepTime = now;
            }
            else
            {
                var movement = feet-lastStep;movement.y=0;
                if(movement.sqrMagnitude >= .25f && now-lastStepTime >= .28f)
                {
                    var foot = point + right * ((RippleEvents & 1)==0 ? .13f : -.13f);
                    // Keep the foot ripple within the same rectangle at the bank.
                    if(data.meandering) foot = VillageLandscapeShape.Unwarp(foot);
                    foot.x = Mathf.Clamp(foot.x,data.rectangle.x+.02f,data.rectangle.z-.02f);
                    foot.z = Mathf.Clamp(foot.z,data.rectangle.y+.02f,data.rectangle.w-.02f);
                    if(data.meandering) foot = VillageLandscapeShape.Warp(foot);
                    if(!data.Contains(foot)) foot=point;
                    AddRipple(foot,surface,data.canal?.5f:.4f,now);
                    lastStep = feet;lastStepTime = now;
                }
            }
        }
        ActiveSurface=surface;lastFeet=feet;havePrevious=true;
    }
    public void AddRipple(Vector3 point,int surface,float strength,float now)
    {
        if(surface<0 || surface>=surfaces.Length) return;
        origins[cursor]=new Vector4(point.x,point.y,point.z,now);
        parameters[cursor]=new Vector4(strength,surface,2.2f,1.15f);
        cursor=(cursor+1)%RippleCapacity;count=Mathf.Min(count+1,RippleCapacity);RippleEvents++;
        Shader.SetGlobalVectorArray(OriginsId,origins);Shader.SetGlobalVectorArray(ParametersId,parameters);
        Shader.SetGlobalInt(CountId,count);
    }
    public void ExpireRipples(float now)
    {
        if(count==0) return;
        for(var i=0;i<count;i++)
            if(parameters[i].z>0 && now-origins[i].w<parameters[i].z) return;
        // Do not keep eight per-pixel ripple branches running after water goes quiet.
        count=cursor=0;Shader.SetGlobalInt(CountId,0);
    }
    private void EmitSplash(Vector3 point,float strength,bool canal)
    {
        if(!spray) return;
        if(!spray.isPlaying) spray.Play();
        SplashEvents++;
        int droplets = canal ? Mathf.RoundToInt(32*strength) : Mathf.RoundToInt(12*strength);
        for(var i=0;i<droplets;i++)
        {
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            float spread=(.4f+(float)random.NextDouble()*.65f)*strength;
            var emit=new ParticleSystem.EmitParams
            {
                position=point+Vector3.up*.025f,
                velocity=new Vector3(Mathf.Cos(angle)*spread,(1.3f+(float)random.NextDouble()*1.4f)*strength,Mathf.Sin(angle)*spread),
                startLifetime=.45f+(float)random.NextDouble()*.35f,
                startSize=.018f+(float)random.NextDouble()*.025f,
                startColor=new Color(.7f,.88f,.95f,.85f)
            };
            spray.Emit(emit,1);
        }
    }
    public void ClearInteraction()
    {
        Array.Clear(origins,0,origins.Length);Array.Clear(parameters,0,parameters.Length);
        cursor=count=0;RippleEvents=SplashEvents=0;ActiveSurface=-1;havePrevious=false;
        Shader.SetGlobalInt(CountId,0);
        if(spray) spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(player) player.WaterSpeedMultiplier=1;
    }
    private void OnDisable() {ClearInteraction();}
}
