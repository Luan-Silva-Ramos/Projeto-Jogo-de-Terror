#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Diagnostico apenas da visualizacao Scene. Nao altera materiais nem a cena.
public static class MansaoDoorGlareDiagnosticV25
{
    private static SceneView savedView; private static bool saved, lighting, effects, flares;
    [MenuItem("Tools/Mansao/Diagnosticar clarao V25/Desligar efeitos somente na Scene")]
    private static void Test()
    {
        SceneView view=SceneView.lastActiveSceneView;
        if(view==null){EditorUtility.DisplayDialog("Diagnostico V25","Abra a aba Scene primeiro.","OK");return;}
        if(!saved)
        {
            lighting=view.sceneLighting;
            effects=view.sceneViewState.showImageEffects;
            flares=view.sceneViewState.showFlares;
            savedView=view;
            saved=true;
        }
        view.sceneLighting=true;
        view.sceneViewState.showImageEffects=false;
        view.sceneViewState.showFlares=false;
        view.Repaint();SceneView.RepaintAll();
        Debug.Log("V25: visualizacao Scene com luzes da cena e sem pos-processamento/flares. Materiais, luzes reais e Game nao foram alterados. Use o menu Restaurar visualizacao para voltar.");
    }
    [MenuItem("Tools/Mansao/Diagnosticar clarao V25/Restaurar visualizacao")]
    private static void Restore()
    {
        if(!saved)return;
        SceneView view=savedView;
        if(view==null){EditorUtility.DisplayDialog("Diagnostico V25","A janela Scene original foi fechada. O registro sera mantido nesta sessao.","OK");return;}
        view.sceneLighting=lighting;
        view.sceneViewState.showImageEffects=effects;
        view.sceneViewState.showFlares=flares;
        saved=false;view.Repaint();SceneView.RepaintAll();
    }
}
#endif

