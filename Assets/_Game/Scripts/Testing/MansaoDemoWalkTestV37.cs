using UnityEngine;
using UnityEngine.InputSystem;

// Instrumento temporario para medir deslocamento; nao e um sistema de perseguicao.
public class MansaoDemoWalkTestV37 : MonoBehaviour
{
    public Camera playerCamera;
    public Bounds startBounds=new Bounds(new Vector3(7.25f,1.2f,-4.7f),new Vector3(5.6f,2.4f,7f));
    public Bounds finishBounds=new Bounds(new Vector3(4.65f,1.2f,2f),new Vector3(.65f,2.4f,1f));
    public string status="Entre no escritorio e pressione F6 para medir a fuga.";
    public float lastTime;
    bool timing;float start;
    void Update()
    {
        if(Time.timeScale==0 || playerCamera==null)return;
        Keyboard keyboard=Keyboard.current;
        if(keyboard!=null && keyboard.f7Key.wasPressedThisFrame){timing=false;lastTime=0;status="Cronometro zerado. F6 inicia dentro do escritorio.";}
        if(keyboard!=null && keyboard.f6Key.wasPressedThisFrame)
        {
            if(!startBounds.Contains(playerCamera.transform.position)){status="Para iniciar, entre no escritorio (ala frontal direita).";return;}
            timing=true;start=Time.time;status="Vá pelo hall e entre na biblioteca, seguindo a linha laranja.";
        }
        if(timing && finishBounds.Contains(playerCamera.transform.position))
        {
            timing=false;lastTime=Time.time-start;status="Percurso concluido: "+lastTime.ToString("F2")+" s. F7 zera.";
            Debug.Log("Teste V37: escritorio -> biblioteca pelo hall em "+lastTime.ToString("F2")+" segundos. Deslocamento sem inimigo; nao e duracao de perseguicao validada.",this);
        }
    }
    void OnGUI()
    {
        GUI.Box(new Rect(12,12,585,105),GUIContent.none);
        GUIStyle title=new GUIStyle(GUI.skin.label);title.fontSize=18;title.normal.textColor=Color.white;
        GUI.Label(new Rect(25,20,560,25),"PROTOTIPO TEMPORARIO V37 — F6 inicia / F7 zera",title);
        GUIStyle text=new GUIStyle(GUI.skin.label);text.fontSize=16;text.wordWrap=true;text.normal.textColor=Color.white;
        GUI.Label(new Rect(25,49,560,62),timing?status+"  "+(Time.time-start).ToString("F1")+" s":status,text);
    }
}
