using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventarioUI : MonoBehaviour
{
    [Header("Inventario - Panel principal")]
    public TextMeshProUGUI lblBienvenida;
    public Transform       contenedorItems;
    public GameObject      prefabItemFila;

    [Header("Panel - Anadir objeto")]
    public GameObject      panelAnadir;
    public TMP_Dropdown    dropdownObjetos;
    public TMP_InputField  inputCantidadAnadir;
    public Button          btnConfirmarAnadir;
    public Button          btnCancelarAnadir;
    public TextMeshProUGUI lblMensajeAnadir;

    [Header("Panel - Editar cantidad")]
    public GameObject      panelEditar;
    public TextMeshProUGUI lblEditarNombre;
    public TMP_InputField  inputNuevaCantidad;
    public Button          btnConfirmarEditar;
    public Button          btnCancelarEditar;
    public TextMeshProUGUI lblMensajeEditar;

    [Header("Panel - Historial (Ampliacion)")]
    public GameObject      panelHistorial;
    public Transform       contenedorHistorial;
    public Button          btnCerrarHistorial;

    [Header("Botones globales")]
    public Button          btnAbrirAnadir;
    public Button          btnVerHistorial;
    public Button          btnCerrarSesion;

    [Header("Canvas")]
    public GameObject canvasInventario;
    public GameObject canvasLogin;

    private List<EntradaInventario> _inventario = new List<EntradaInventario>();
    private List<ObjetoRPG>         _catalogo   = new List<ObjetoRPG>();
    private EntradaInventario       _entradaActual;

    void Start()
    {
        if (btnAbrirAnadir   != null) btnAbrirAnadir.onClick.AddListener(AbrirPanelAnadir);
        if (btnVerHistorial  != null) btnVerHistorial.onClick.AddListener(AbrirPanelHistorial);
        if (btnCerrarSesion  != null) btnCerrarSesion.onClick.AddListener(CerrarSesion);

        if (btnConfirmarAnadir != null) btnConfirmarAnadir.onClick.AddListener(ConfirmarAnadir);
        if (btnCancelarAnadir  != null) btnCancelarAnadir.onClick.AddListener(() => panelAnadir.SetActive(false));

        if (btnConfirmarEditar != null) btnConfirmarEditar.onClick.AddListener(ConfirmarEditar);
        if (btnCancelarEditar  != null) btnCancelarEditar.onClick.AddListener(() => panelEditar.SetActive(false));

        if (btnCerrarHistorial != null) btnCerrarHistorial.onClick.AddListener(() => panelHistorial.SetActive(false));
    }

    void OnEnable()
    {
        if (panelAnadir    != null) panelAnadir.SetActive(false);
        if (panelEditar    != null) panelEditar.SetActive(false);
        if (panelHistorial != null) panelHistorial.SetActive(false);

        if (lblBienvenida != null)
            lblBienvenida.text = "Inventario de  " + SesionUsuario.NombreUsuario;

        if (SesionUsuario.HayUsuario)
            CargarInventario();
    }

    public void CargarInventario()
    {
        _inventario = InventarioDAL.ObtenerInventario(SesionUsuario.UsuarioId);
        RefrescarUI();
    }

    private void RefrescarUI()
    {
        foreach (Transform hijo in contenedorItems)
            Destroy(hijo.gameObject);

        if (_inventario.Count == 0)
        {
            CrearTextoInfo(contenedorItems, "El inventario esta vacio.\nUsa el boton + para anadir objetos.");
            return;
        }

        foreach (var entrada in _inventario)
        {
            GameObject fila = Instantiate(prefabItemFila, contenedorItems);

            SetTextoEnHijo(fila, "NombreObjeto",   entrada.NombreObjeto);
            SetTextoEnHijo(fila, "TipoObjeto",     entrada.Tipo);
            SetTextoEnHijo(fila, "CantidadObjeto", entrada.Acumulable ? "x" + entrada.Cantidad : "-");
            SetTextoEnHijo(fila, "DescObjeto",     entrada.Descripcion);

            TextMeshProUGUI[] textos = fila.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (textos.Length > 0 && string.IsNullOrEmpty(textos[0].text)) textos[0].text = entrada.NombreObjeto;
            if (textos.Length > 1 && string.IsNullOrEmpty(textos[1].text)) textos[1].text = entrada.Tipo;
            if (textos.Length > 2 && string.IsNullOrEmpty(textos[2].text)) textos[2].text = entrada.Acumulable ? "x" + entrada.Cantidad : "-";
            if (textos.Length > 3 && string.IsNullOrEmpty(textos[3].text)) textos[3].text = entrada.Descripcion;

            EntradaInventario captura = entrada;

            AsignarBotonFila(fila, "BtnEditar",   () => AbrirPanelEditar(captura));
            AsignarBotonFila(fila, "BtnEliminar", () => EliminarObjeto(captura));

            Button[] botones = fila.GetComponentsInChildren<Button>(true);
            if (botones.Length > 0) { EntradaInventario c = captura; botones[0].onClick.RemoveAllListeners(); botones[0].onClick.AddListener(() => AbrirPanelEditar(c)); }
            if (botones.Length > 1) { EntradaInventario c = captura; botones[1].onClick.RemoveAllListeners(); botones[1].onClick.AddListener(() => EliminarObjeto(c)); }
        }
    }

    private void AbrirPanelAnadir()
    {
        panelAnadir.SetActive(true);
        lblMensajeAnadir.text    = "";
        inputCantidadAnadir.text = "1";

        _catalogo = InventarioDAL.ObtenerCatalogoObjetos();
        dropdownObjetos.ClearOptions();
        var opciones = new List<string>();
        foreach (var obj in _catalogo)
            opciones.Add("[" + obj.Tipo + "] " + obj.Nombre);
        dropdownObjetos.AddOptions(opciones);
    }

    private void ConfirmarAnadir()
    {
        if (_catalogo.Count == 0) return;

        var obj = _catalogo[dropdownObjetos.value];

        int cantidad = 1;
        if (obj.Acumulable)
        {
            if (!int.TryParse(inputCantidadAnadir.text, out cantidad) || cantidad < 1)
            {
                lblMensajeAnadir.text = "Introduce una cantidad valida (>= 1).";
                return;
            }
        }

        bool ok = InventarioDAL.AnadirObjeto(SesionUsuario.UsuarioId, obj.Id, cantidad);

        if (ok)
        {
            var inventario = InventarioDAL.ObtenerInventario(SesionUsuario.UsuarioId);
            foreach (var e in inventario)
            {
                if (e.ObjetoId == obj.Id)
                {
                    InventarioDAL.RegistrarUso(e.Id, "Objeto anadido al inventario: " + obj.Nombre);
                    break;
                }
            }

            lblMensajeAnadir.text = obj.Nombre + " anadido correctamente.";
            panelAnadir.SetActive(false);
            CargarInventario();
        }
        else
        {
            lblMensajeAnadir.text = "El objeto ya esta en el inventario y no es acumulable.";
        }
    }
    private void AbrirPanelEditar(EntradaInventario entrada)
    {
        _entradaActual          = entrada;
        panelEditar.SetActive(true);
        lblEditarNombre.text    = "Editando: " + entrada.NombreObjeto;
        inputNuevaCantidad.text = entrada.Cantidad.ToString();
        lblMensajeEditar.text   = "";
        inputNuevaCantidad.interactable = entrada.Acumulable;
        if (!entrada.Acumulable)
            lblMensajeEditar.text = "Este objeto no es acumulable (cantidad fija = 1).";
    }

    private void ConfirmarEditar()
    {
        if (_entradaActual == null) return;

        if (!_entradaActual.Acumulable)
        {
            panelEditar.SetActive(false);
            return;
        }

        if (!int.TryParse(inputNuevaCantidad.text, out int nuevaCantidad) || nuevaCantidad < 1)
        {
            lblMensajeEditar.text = "Introduce una cantidad valida (>= 1).";
            return;
        }

        bool ok = InventarioDAL.ActualizarCantidad(
            SesionUsuario.UsuarioId, _entradaActual.ObjetoId, nuevaCantidad);

        if (ok)
        {
            InventarioDAL.RegistrarUso(_entradaActual.Id,
                "Cantidad modificada a " + nuevaCantidad + ": " + _entradaActual.NombreObjeto);
            panelEditar.SetActive(false);
            CargarInventario();
        }
        else
        {
            lblMensajeEditar.text = "No se pudo actualizar la cantidad.";
        }
    }

    private void EliminarObjeto(EntradaInventario entrada)
    {
        InventarioDAL.RegistrarUso(entrada.Id, "Objeto eliminado del inventario: " + entrada.NombreObjeto);

        bool ok = InventarioDAL.EliminarObjeto(SesionUsuario.UsuarioId, entrada.ObjetoId);
        if (ok)
            CargarInventario();
        else
            Debug.LogWarning("[InventarioUI] No se pudo eliminar el objeto.");
    }

    private void AbrirPanelHistorial()
    {
        panelHistorial.SetActive(true);

        foreach (Transform hijo in contenedorHistorial)
            Destroy(hijo.gameObject);

        var historial = InventarioDAL.ObtenerHistorial(SesionUsuario.UsuarioId);

        if (historial.Count == 0)
        {
            CrearTextoInfo(contenedorHistorial, "No hay registros en el historial todavia.\nAnade o modifica objetos para generar registros.");
            return;
        }

        foreach (var reg in historial)
        {
            GameObject fila = new GameObject("FilaHistorial");
            fila.transform.SetParent(contenedorHistorial, false);

            Image img   = fila.AddComponent<Image>();
            img.color   = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            RectTransform rt = fila.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 60);

            HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
            hlg.padding           = new RectOffset(10, 10, 8, 8);
            hlg.spacing           = 15;
            hlg.childAlignment    = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // Fecha
            CrearCelda(fila.transform, reg.FechaUso.ToString("dd/MM/yy HH:mm"), 130, 16);
            // Nombre objeto
            CrearCelda(fila.transform, reg.NombreObjeto, 180, 16);
            // Nota 
            CrearCelda(fila.transform, reg.Nota, 0, 14, true);
        }
    }

    private void CerrarSesion()
    {
        SesionUsuario.CerrarSesion();
        if (canvasInventario != null) canvasInventario.SetActive(false);
        if (canvasLogin      != null) canvasLogin.SetActive(true);
    }

    /// Crea un texto centrado como mensaje informativo dentro de un contenedor
    private void CrearTextoInfo(Transform padre, string mensaje)
    {
        var go      = new GameObject("Info");
        go.transform.SetParent(padre, false);
        var txt       = go.AddComponent<TextMeshProUGUI>();
        txt.text      = mensaje;
        txt.fontSize  = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(0.7f, 0.7f, 0.7f);
        var rt        = go.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(0, 80);
    }

    /// Crea una celda de texto dentro de una fila del historial
    private void CrearCelda(Transform padre, string texto, float ancho, float fontSize, bool expandir = false)
    {
        var go    = new GameObject("Celda");
        go.transform.SetParent(padre, false);

        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = texto;
        tmp.fontSize   = fontSize;
        tmp.color      = Color.white;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        var le         = go.AddComponent<LayoutElement>();
        le.preferredWidth  = ancho;
        le.flexibleWidth   = expandir ? 1 : 0;
        le.minHeight       = 40;
    }

    private void SetTextoEnHijo(GameObject fila, string nombreHijo, string texto)
    {
        Transform hijo = fila.transform.Find(nombreHijo);
        if (hijo == null) return;

        TextMeshProUGUI tmp = hijo.GetComponent<TextMeshProUGUI>();
        if (tmp != null) { tmp.text = texto; return; }

        Text txt = hijo.GetComponent<Text>();
        if (txt != null) txt.text = texto;
    }

    private void AsignarBotonFila(GameObject fila, string nombreHijo, UnityEngine.Events.UnityAction accion)
    {
        Transform hijo = fila.transform.Find(nombreHijo);
        if (hijo == null) return;

        Button btn = hijo.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(accion);
        }
    }
}
