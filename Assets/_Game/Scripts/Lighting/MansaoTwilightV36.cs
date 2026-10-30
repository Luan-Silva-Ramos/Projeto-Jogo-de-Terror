using System;
using UnityEngine;
using UnityEngine.Rendering;

// Dados de restauracao e oscilacao leve das quatro velas; sem execucao no Editor.
[DisallowMultipleComponent]
public class MansaoTwilightV36 : MonoBehaviour
{
    [Serializable] public class Candle
    {
        public Light light;public Behaviour previousFlicker;public bool previousFlickerEnabled;
        public Transform flame;public Vector3 flameScale;public float seed;
        public float intensity,range,temperature,bias,normalBias,nearPlane;public Color color;public bool useTemperature;
        public LightUnit unit;public LightShadows shadows;public int resolution;
    }
    public Candle[] candles;
    public Light oldDirectional,previousRenderSun;public bool oldDirectionalEnabled;
    public Material previousSky;public AmbientMode previousAmbientMode;public Color previousSkyColor,previousEquator,previousGround;
    public float previousAmbientIntensity,previousReflectionIntensity;public bool previousFog;public FogMode previousFogMode;
    public Color previousFogColor;public float previousFogDensity,previousFogStart,previousFogEnd;
    public bool hadSceneView,previousSceneLighting,previousSceneSkybox,previousSceneEffects;
    public Camera playerCamera;
    [Min(0)] public float candleLumens=14;
    [Range(0,.15f)] public float variation=.035f;
    [Min(1)] public float updatesPerSecond=20;
    float nextUpdate;
    void Update()
    {
        if(Time.timeScale==0 || Time.unscaledTime<nextUpdate)return;
        nextUpdate=Time.unscaledTime+1/Mathf.Max(1,updatesPerSecond);
        if(candles==null)return;
        foreach(Candle candle in candles)
        {
            if(candle.light==null)continue;
            float n=Mathf.PerlinNoise(candle.seed,Time.time*2.3f)*2-1;
            // Point light: fluxo em lumens dividido por 4*pi = intensidade em candelas.
            candle.light.intensity=candleLumens/(4*Mathf.PI)*(1+variation*n);
            if(candle.flame!=null)candle.flame.localScale=new Vector3(candle.flameScale.x*(1-.02f*n),candle.flameScale.y*(1+.04f*n),candle.flameScale.z);
            // Histerese: sombras locais sao dispensadas longe da camera e voltam ao aproximar.
            if(playerCamera!=null){float distance=Vector3.Distance(playerCamera.transform.position,candle.light.transform.position);
                if(distance>20 && candle.light.shadows!=LightShadows.None)candle.light.shadows=LightShadows.None;
                else if(distance<16 && candle.light.shadows==LightShadows.None)candle.light.shadows=LightShadows.Hard;}
        }
    }
}
