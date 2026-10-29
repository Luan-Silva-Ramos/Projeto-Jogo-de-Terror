using UnityEngine;

[DisallowMultipleComponent]
public class MansaoCandleFlickerV29 : MonoBehaviour
{
    public Light candleLight;
    public Transform flame;
    public float intensityLumens=12f;
    public float seed;
    [Range(0f,.15f)] public float variation=.06f;
    private Vector3 initialScale;
    private void Start(){if(flame!=null)initialScale=flame.localScale;}
    private void Update()
    {
        if(Time.timeScale==0)return;
        float n=Mathf.PerlinNoise(seed,Time.time*2.3f)*2f-1f;
        if(candleLight!=null)candleLight.intensity=intensityLumens*(1+variation*n);
        if(flame!=null)flame.localScale=new Vector3(initialScale.x*(1-.025f*n),initialScale.y*(1+.05f*n),initialScale.z);
    }
}
