using UnityEngine;
using UnityEngine.UI;

public class BotonReg : MonoBehaviour
{
    [Header("Objetos a controlar")]
    public GameObject objetoActivar;    
    public GameObject objetoDesactivar; 

    [Header("Botón de activación")]
    public Button boton;                

    void Start()
    {
        if (boton != null)
        {
            boton.onClick.AddListener(AlternarObjetos);
        }

    }

    public void AlternarObjetos()
    {
        if (objetoActivar != null)
            objetoActivar.SetActive(true);

        if (objetoDesactivar != null)
            objetoDesactivar.SetActive(false);
    }
}
