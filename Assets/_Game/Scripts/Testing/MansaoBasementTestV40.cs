using UnityEngine;
using UnityEngine.InputSystem;

// Porta e dados de restauracao exclusivos do prototipo de acesso ao porao.
public class MansaoBasementTestV40 : MonoBehaviour
{
    public GameObject originalKitchenFloor;
    public bool originalFloorActive;
    public Camera playerCamera;
    public Transform hinge,interactionPoint;
    public float interactionDistance=1.9f;
    public float openingSpeed=90;
    Quaternion closed,opened;bool isOpen;
    void Start(){if(hinge!=null){closed=hinge.localRotation;opened=closed*Quaternion.Euler(0,90,0);}}
    void Update()
    {
        if(Time.timeScale==0 || hinge==null || playerCamera==null || interactionPoint==null)return;
        Keyboard keyboard=Keyboard.current;
        if(Vector3.Distance(playerCamera.transform.position,interactionPoint.position)<=interactionDistance && keyboard!=null && keyboard.eKey.wasPressedThisFrame)isOpen=!isOpen;
        hinge.localRotation=Quaternion.RotateTowards(hinge.localRotation,isOpen?opened:closed,openingSpeed*Time.deltaTime);
    }
    void OnGUI()
    {
        string message="TESTE V40 — siga a linha ambar ate a cozinha.\nAproxime-se da porta para abrir com E. Descida temporaria, sem evento narrativo.";
        if(playerCamera!=null && interactionPoint!=null && Vector3.Distance(playerCamera.transform.position,interactionPoint.position)<=interactionDistance)message="TESTE V40 — E: "+(isOpen?"fechar":"abrir")+" a porta do porao.\nConfira entrada, descida e retorno. A porta abre para a cozinha.";
        GUI.Box(new Rect(12,128,585,82),GUIContent.none);GUIStyle style=new GUIStyle(GUI.skin.label);style.fontSize=16;style.wordWrap=true;style.normal.textColor=Color.white;GUI.Label(new Rect(25,138,555,67),message,style);
    }
}
