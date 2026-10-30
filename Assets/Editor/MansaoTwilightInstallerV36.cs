#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MansaoTwilightInstallerV36
{
    const string Root="Lighting_Twilight_V36",Folder="Assets/_Game/Lighting/TwilightV36/";
    [MenuItem("Tools/Mansao/Iluminacao/Aplicar crepusculo V36")]
    static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=SelectedMansion();if(mansion==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        if(mansion.Find(Root)!=null){Tell("O conjunto V36 ja existe. Use Restaurar iluminacao anterior V36 antes de montar outra vez.");return;}
        int group=-1;
        try
        {
            Transform lanterns=mansion.Find("GroundFloor_V2/Facade_Details/Exterior_CandleLanterns_V29");
            if(lanterns==null)throw new InvalidOperationException("Lanternas V29 nao encontradas.");
            Light[] lights=lanterns.GetComponentsInChildren<Light>();if(lights.Length!=4)throw new InvalidOperationException("Esperava quatro luzes das lanternas aprovadas.");
            Light oldSun=null;foreach(GameObject root in mansion.gameObject.scene.GetRootGameObjects())foreach(Light l in root.GetComponentsInChildren<Light>())if(l.enabled && l.type==LightType.Directional){if(oldSun!=null)throw new InvalidOperationException("Existe mais de uma luz direcional ativa. Nenhuma foi alterada.");oldSun=l;}
            if(oldSun==null)throw new InvalidOperationException("Luz direcional atual nao encontrada.");
            Camera camera=null;foreach(GameObject root in mansion.gameObject.scene.GetRootGameObjects())foreach(Camera c in root.GetComponentsInChildren<Camera>())if(c.enabled && c.CompareTag("MainCamera"))camera=c;
            if(camera==null)throw new InvalidOperationException("Main Camera ativa nao encontrada.");
            UniversalAdditionalCameraData cameraData=camera.GetComponent<UniversalAdditionalCameraData>();
            if(cameraData==null || !cameraData.renderPostProcessing || (cameraData.volumeLayerMask.value&1)==0)throw new InvalidOperationException("Main Camera precisa manter Post Processing ativo e Volume Mask incluindo Default, como na cena conferida.");
            var candles=new List<MansaoTwilightV36.Candle>();
            foreach(Light light in lights)
            {
                Behaviour flicker=null;foreach(Behaviour component in light.transform.parent.GetComponents<Behaviour>())if(component!=null && component.GetType().Name=="MansaoCandleFlickerV29")flicker=component;
                Transform flame=light.transform.parent.Find("Candle_Flame_V29");
                if(light.type!=LightType.Point || flicker==null || flame==null)throw new InvalidOperationException("Uma lanterna difere da estrutura aprovada.");
                var serial=new SerializedObject(flicker);float seed=serial.FindProperty("seed").floatValue;
                candles.Add(new MansaoTwilightV36.Candle{light=light,previousFlicker=flicker,previousFlickerEnabled=flicker.enabled,flame=flame,flameScale=flame.localScale,seed=seed,intensity=light.intensity,range=light.range,temperature=light.colorTemperature,bias=light.shadowBias,normalBias=light.shadowNormalBias,nearPlane=light.shadowNearPlane,color=light.color,useTemperature=light.useColorTemperature,unit=light.lightUnit,shadows=light.shadows,resolution=light.shadowCustomResolution});
            }
            EnsureFolder(Folder.TrimEnd('/'));VolumeProfile profile=Profile();Material sky=Sky();
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Aplicar crepusculo V36");Undo.RecordObject(Unsupported.GetRenderSettings(),"Guardar ambiente anterior");
            GameObject rootObject=new GameObject(Root);SceneManager.MoveGameObjectToScene(rootObject,mansion.gameObject.scene);rootObject.transform.SetParent(mansion,false);Undo.RegisterCreatedObjectUndo(rootObject,"Criar iluminacao de crepusculo");
            MansaoTwilightV36 state=Undo.AddComponent<MansaoTwilightV36>(rootObject);state.candles=candles.ToArray();state.playerCamera=camera;
            state.oldDirectional=oldSun;state.oldDirectionalEnabled=oldSun.enabled;state.previousRenderSun=RenderSettings.sun;
            state.previousSky=RenderSettings.skybox;state.previousAmbientMode=RenderSettings.ambientMode;state.previousSkyColor=RenderSettings.ambientSkyColor;state.previousEquator=RenderSettings.ambientEquatorColor;state.previousGround=RenderSettings.ambientGroundColor;state.previousAmbientIntensity=RenderSettings.ambientIntensity;state.previousReflectionIntensity=RenderSettings.reflectionIntensity;
            state.previousFog=RenderSettings.fog;state.previousFogMode=RenderSettings.fogMode;state.previousFogColor=RenderSettings.fogColor;state.previousFogDensity=RenderSettings.fogDensity;state.previousFogStart=RenderSettings.fogStartDistance;state.previousFogEnd=RenderSettings.fogEndDistance;
            Undo.RecordObject(oldSun,"Preservar luz diurna");oldSun.enabled=false;
            GameObject sunObject=new GameObject("Sun_LateAfternoon_V36");SceneManager.MoveGameObjectToScene(sunObject,mansion.gameObject.scene);sunObject.transform.SetParent(rootObject.transform,false);sunObject.transform.rotation=Quaternion.Euler(14,-32,0);Undo.RegisterCreatedObjectUndo(sunObject,"Criar sol de fim de tarde");
            Light sun=Undo.AddComponent<Light>(sunObject);sun.type=LightType.Directional;sun.lightUnit=LightUnit.Lux;sun.intensity=.38f;sun.useColorTemperature=true;sun.colorTemperature=4500;sun.color=Color.white;sun.shadows=LightShadows.Soft;sun.shadowStrength=.85f;sun.shadowBias=.035f;sun.shadowNormalBias=.15f;sun.cullingMask=oldSun.cullingMask;sun.renderingLayerMask=oldSun.renderingLayerMask;
            Undo.AddComponent<UniversalAdditionalLightData>(sunObject);RenderSettings.sun=sun;
            RenderSettings.skybox=sky;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.18f,.22f,.30f);RenderSettings.ambientEquatorColor=new Color(.11f,.13f,.18f);RenderSettings.ambientGroundColor=new Color(.055f,.047f,.042f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.40f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.24f,.27f,.34f);RenderSettings.fogDensity=.004f;
            foreach(var candle in state.candles){Undo.RecordObject(candle.previousFlicker,"Trocar controle das velas");candle.previousFlicker.enabled=false;Undo.RecordObject(candle.light,"Regular luz de vela");Light l=candle.light;l.lightUnit=LightUnit.Candela;l.intensity=14/(4*Mathf.PI);l.range=2.0f;l.useColorTemperature=true;l.colorTemperature=2200;l.color=Color.white;l.shadows=LightShadows.Hard;l.shadowCustomResolution=256;l.shadowBias=.02f;l.shadowNormalBias=.015f;l.shadowNearPlane=.05f;}
            GameObject volumeObject=new GameObject("Twilight_Volume_V36");SceneManager.MoveGameObjectToScene(volumeObject,mansion.gameObject.scene);volumeObject.transform.SetParent(rootObject.transform,false);Undo.RegisterCreatedObjectUndo(volumeObject,"Criar volume exclusivo");Volume volume=Undo.AddComponent<Volume>(volumeObject);volume.isGlobal=true;volume.priority=100;volume.weight=1;volume.sharedProfile=profile;
            SceneView view=SceneView.lastActiveSceneView;if(view!=null){state.hadSceneView=true;state.previousSceneLighting=view.sceneLighting;state.previousSceneSkybox=view.sceneViewState.showSkybox;state.previousSceneEffects=view.sceneViewState.showImageEffects;view.sceneLighting=true;view.sceneViewState.showSkybox=true;view.sceneViewState.showImageEffects=true;}
            EditorUtility.SetDirty(state);EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=rootObject;Refresh();
            Debug.Log("Crepusculo V36 aplicado: luz ambiente fria suave, sol baixo .38, velas 14 lm / 2200 K, Bloom zero e sombras locais com distancia. Luz diurna e valores anteriores preservados no conjunto V36. Conferir Scene e Play; nao houve medicao de FPS nem bake.",rootObject);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    [MenuItem("Tools/Mansao/Iluminacao/Restaurar iluminacao anterior V36")]
    static void Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=SelectedMansion(),root=mansion==null?null:mansion.Find(Root);MansaoTwilightV36 s=root==null?null:root.GetComponent<MansaoTwilightV36>();
        if(s==null){Tell("Selecione um objeto da Mansion que tenha o conjunto V36.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Restaurar iluminacao anterior V36");Undo.RecordObject(Unsupported.GetRenderSettings(),"Restaurar ambiente anterior");
        if(s.oldDirectional!=null){Undo.RecordObject(s.oldDirectional,"Restaurar luz diurna");s.oldDirectional.enabled=s.oldDirectionalEnabled;}
        RenderSettings.sun=s.previousRenderSun;RenderSettings.skybox=s.previousSky;RenderSettings.ambientMode=s.previousAmbientMode;RenderSettings.ambientSkyColor=s.previousSkyColor;RenderSettings.ambientEquatorColor=s.previousEquator;RenderSettings.ambientGroundColor=s.previousGround;RenderSettings.ambientIntensity=s.previousAmbientIntensity;RenderSettings.reflectionIntensity=s.previousReflectionIntensity;
        RenderSettings.fog=s.previousFog;RenderSettings.fogMode=s.previousFogMode;RenderSettings.fogColor=s.previousFogColor;RenderSettings.fogDensity=s.previousFogDensity;RenderSettings.fogStartDistance=s.previousFogStart;RenderSettings.fogEndDistance=s.previousFogEnd;
        foreach(var c in s.candles){if(c.light!=null){Undo.RecordObject(c.light,"Restaurar vela");c.light.lightUnit=c.unit;c.light.intensity=c.intensity;c.light.range=c.range;c.light.color=c.color;c.light.colorTemperature=c.temperature;c.light.useColorTemperature=c.useTemperature;c.light.shadows=c.shadows;c.light.shadowCustomResolution=c.resolution;c.light.shadowBias=c.bias;c.light.shadowNormalBias=c.normalBias;c.light.shadowNearPlane=c.nearPlane;}if(c.previousFlicker!=null){Undo.RecordObject(c.previousFlicker,"Restaurar oscilacao anterior");c.previousFlicker.enabled=c.previousFlickerEnabled;}if(c.flame!=null){Undo.RecordObject(c.flame,"Restaurar chama");c.flame.localScale=c.flameScale;}}
        SceneView view=SceneView.lastActiveSceneView;if(view!=null && s.hadSceneView){view.sceneLighting=s.previousSceneLighting;view.sceneViewState.showSkybox=s.previousSceneSkybox;view.sceneViewState.showImageEffects=s.previousSceneEffects;}
        Undo.DestroyObjectImmediate(root.gameObject);EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);Undo.CollapseUndoOperations(group);Refresh();
    }
    static VolumeProfile Profile()
    {
        string path=Folder+"Twilight_V36.asset";VolumeProfile p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);if(p!=null)return p;
        p=ScriptableObject.CreateInstance<VolumeProfile>();p.name="Twilight_V36";AssetDatabase.CreateAsset(p,path);
        Bloom bloom=p.Add<Bloom>(true);bloom.intensity.Override(0);
        Tonemapping tone=p.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
        ColorAdjustments colors=p.Add<ColorAdjustments>(true);colors.postExposure.Override(.15f);colors.contrast.Override(8);colors.saturation.Override(-6);
        WhiteBalance white=p.Add<WhiteBalance>(true);white.temperature.Override(-3);white.tint.Override(0);
        Vignette vignette=p.Add<Vignette>(true);vignette.intensity.Override(.10f);vignette.smoothness.Override(.60f);
        foreach(VolumeComponent c in p.components)AssetDatabase.AddObjectToAsset(c,p);EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();return p;
    }
    static Material Sky()
    {
        string path=Folder+"Sky_Twilight_V36.mat";Material sky=AssetDatabase.LoadAssetAtPath<Material>(path);if(sky!=null)return sky;Shader shader=Shader.Find("Skybox/Procedural");if(shader==null)throw new InvalidOperationException("Shader Skybox/Procedural indisponivel.");
        sky=new Material(shader);sky.name="Sky_Twilight_V36";sky.SetColor("_SkyTint",new Color(.34f,.38f,.48f));sky.SetColor("_GroundColor",new Color(.10f,.10f,.12f));sky.SetFloat("_Exposure",.65f);sky.SetFloat("_AtmosphereThickness",1.1f);sky.SetFloat("_SunSize",.025f);sky.SetFloat("_SunDisk",1);AssetDatabase.CreateAsset(sky,path);AssetDatabase.SaveAssets();return sky;
    }
    static Transform SelectedMansion(){Transform t=Selection.activeTransform;while(t!=null && t.name!="Mansion")t=t.parent;return t;}
    static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');EnsureFolder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    static void Refresh(){DynamicGI.UpdateEnvironment();SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();}
    static void Tell(string message){EditorUtility.DisplayDialog("Crepusculo V36",message,"OK");}
}
#endif

