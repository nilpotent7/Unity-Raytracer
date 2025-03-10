using UnityEngine;

public class RayTracerObject : MonoBehaviour
{
    [SerializeField] Transform thisTransform;
    [SerializeField] float Radius;
    [SerializeField] float Smoothness;
    [SerializeField] Color Albedo;
    [SerializeField] Color Specular;
    [SerializeField] Color Emission;

    public RayTracerMaster.Sphere currentSphere = new(Vector3.zero, 0, 0, Vector3.zero, Vector3.zero, Vector3.zero);

    bool _hasChanged = true;
    public bool hasChanged { 
        get { 
            return _hasChanged || transform.hasChanged || Radius != currentSphere.radius ||
            Smoothness != currentSphere.smoothness ||
            (Vector3)(Vector4)Emission != currentSphere.emission ||
            (Vector3)(Vector4)Albedo != currentSphere.albedo ||
            (Vector3)(Vector4)Specular != currentSphere.specular;
        }
        set { 
            // if(!value) transform.hasChanged = false;
            _hasChanged = value;
        }
    }

    private void OnValidate()
    {
        hasChanged = true;
        currentSphere.position = transform.position;
        currentSphere.radius = Radius;
        currentSphere.smoothness = Smoothness;
        currentSphere.albedo = (Vector4)Albedo;
        currentSphere.specular = (Vector4)Specular;
        currentSphere.emission = (Vector4)Emission;
    }

    void Start()
    {
        OnValidate();
        hasChanged = false;
        RayTracerMaster.Instance.RegisterSphere(this);
    }

    void Update()
    {
        if(thisTransform.hasChanged)
        {
            OnValidate();
            hasChanged = false;
        }
    }

    void OnDisable()
    {
        RayTracerMaster.Instance.UnregisterSphere(this);
    }
}
