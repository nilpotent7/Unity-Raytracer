using UnityEngine;
using System.Collections.Generic;

public class RayTracerMaster : MonoBehaviour
{
    public struct Sphere
    {
        public Vector3 position;
        public float radius;

        public float smoothness;
        public Vector3 albedo;
        public Vector3 specular;
        public Vector3 emission;

        public Sphere(Vector3 _pos, float _radius, float _smooth, Vector3 _albedo, Vector3 _spec, Vector3 _emission)
        {
            specular = _spec;
            albedo = _albedo;
            radius = _radius;
            position = _pos;
            smoothness = _smooth;
            emission = _emission;
        }
    }

    public static RayTracerMaster Instance { get; private set; }

    [SerializeField] ComputeShader RayTracingShader;
    [SerializeField] Texture Skybox;
    [SerializeField] Light DirectionalLight;
    [SerializeField] Vector2 TemporalOffsetRange;

    [Header("Ground Plane Material")]
    [SerializeField] float Smoothness;
    [SerializeField] Color Albedo;
    [SerializeField] Color Specular;
    [SerializeField] Color Emission;


    ComputeBuffer sphereBuffer;
    List<RayTracerObject> objects;
    List<Sphere> spheres;

    RenderTexture targetTexture;
    Camera cam;
    uint currentSample = 0;
    Material TemporalMaterial;

    RenderTexture convergedTexture;
    float lastFieldOfView;

    void Awake()
    {
        Instance = this;

        objects = new List<RayTracerObject>();
        spheres = new List<Sphere>();
        cam = GetComponent<Camera>();
        
        if(TemporalMaterial == null)
            TemporalMaterial = new Material(Shader.Find("Hidden/AddShader"));
    }

    public void RegisterSphere(RayTracerObject objectToAdd)
    {
        objects.Add(objectToAdd);
        spheres.Add(objectToAdd.currentSphere);
        sphereBuffer?.Release();
        sphereBuffer = new ComputeBuffer(spheres.Count, 56);
        sphereBuffer.SetData(spheres);
    }

    public void UnregisterSphere(RayTracerObject objectToRemove)
    {
        objects.Remove(objectToRemove);
        spheres.Remove(objectToRemove.currentSphere);
        sphereBuffer?.Release();
        if(spheres.Count != 0)
        {
            sphereBuffer = new ComputeBuffer(spheres.Count, 56);
            sphereBuffer.SetData(spheres);
        }
    }

    void OnValidate()
    {
        currentSample = 0;
        transform.hasChanged = false;
        DirectionalLight.transform.hasChanged = false;

        if(cam != null) lastFieldOfView = cam.fieldOfView;
    }

    void Update()
    {
        bool hasChanged = transform.hasChanged || DirectionalLight.transform.hasChanged || cam.fieldOfView != lastFieldOfView;
        for(int i = 0; i < objects.Count; i++)
        {
            hasChanged = hasChanged || objects[i].hasChanged;
            spheres[i] = objects[i].currentSphere;
            objects[i].hasChanged = false;
            objects[i].transform.hasChanged = false;
        }

        if(hasChanged) OnValidate();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        InitRenderTexture();
        SetShaderParameters();
        Render(destination);
    }

    void Render(RenderTexture destination)
    {
        RayTracingShader.SetTexture(0, "Result", targetTexture);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        TemporalMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(targetTexture, convergedTexture, TemporalMaterial);
        Graphics.Blit(convergedTexture, destination);
        currentSample++;
    }

    void InitRenderTexture()
    {
        if (targetTexture == null || targetTexture.width != Screen.width || targetTexture.height != Screen.height)
        {
            // Release render texture if we already have one
            if (targetTexture != null || convergedTexture != null)
            {
                targetTexture.Release();
                convergedTexture.Release();
            }

            // Get a render target for Ray Tracing
            targetTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            targetTexture.enableRandomWrite = true;
            targetTexture.Create();
            convergedTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            convergedTexture.enableRandomWrite = true;
            convergedTexture.Create();

            // Reset sampling
            currentSample = 0;
        }
    }

    void SetShaderParameters()
    {
        RayTracingShader.SetTexture(0, "_Skybox", Skybox);
        RayTracingShader.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.Range(TemporalOffsetRange.x, TemporalOffsetRange.y), Random.Range(TemporalOffsetRange.x, TemporalOffsetRange.y)));
        RayTracingShader.SetFloat("_Seed", Random.value);
        RayTracingShader.SetFloat("_PlaneSmoothness", Smoothness);
        RayTracingShader.SetVector("_PlaneAlbedo", Albedo);
        RayTracingShader.SetVector("_PlaneSpecular", Specular);
        RayTracingShader.SetVector("_PlaneEmission", Emission);

        sphereBuffer.SetData(spheres);
        RayTracingShader.SetBuffer(0, "_Spheres", sphereBuffer);

        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_SunLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
    }

    private void OnDisable()
    {
        sphereBuffer?.Release();
    }
}