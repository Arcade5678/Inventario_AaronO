using UnityEngine;
using UnityEngine.UI;

public class si : MonoBehaviour
{
    [Header("Botón de salida")]
    public Button boton; 

    void Start()
    {
        if (boton != null)
        {
            boton.onClick.AddListener(Salir); 
        }
    }

    public void Salir()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
