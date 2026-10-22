namespace TXTReader.UITests;

/// <summary>
/// E2E de la vista de Markdown (General 8.6/8.7): abrir un .md desde otra app, verlo renderizado,
/// pasar al texto tal cual y volver, buscar, seguir un enlace (se abre fuera) y el tema oscuro. Los
/// textos del documento se buscan dentro del WebView por su accesibilidad.
/// </summary>
[Collection(UiCollection.Name)]
[TestCaseOrderer("TXTReader.UITests.ByNameOrderer", "TXTReader.UITests")]
public sealed class MarkdownUiTests
{
    private readonly UiSession _s;

    public MarkdownUiTests(UiSession session) => _s = session;

    /// <summary>Abre la muestra si el lector no esta ya a la vista con ella.</summary>
    private void EnsureSampleOpen()
    {
        if (_s.WaitFor(UiSession.Id("MarkdownToggle"), 1) is not null)
        {
            return;
        }

        _s.Push("logo.png");
        var md = _s.Push("prueba.md");
        _s.OpenWithApp(md, "text/markdown");
        _s.Require(UiSession.Id("MarkdownToggle"), "el boton de la vista de Markdown", 30);
    }

    [Fact]
    public void T01_Un_md_se_abre_renderizado_y_el_boton_alterna_con_el_texto()
    {
        EnsureSampleOpen();

        // Renderizado: el titulo sale sin «#».
        _s.Require(UiSession.Text("Prueba de Markdown"), "el titulo renderizado", 15);
        Assert.Null(_s.WaitFor(UiSession.TextStartsWith("# Prueba"), 1));
        _s.Shot("01-renderizado");

        // Texto tal cual.
        _s.Require(UiSession.Id("MarkdownToggle"), "el boton").Click();
        _s.Require(UiSession.TextStartsWith("# Prueba de Markdown"), "el texto tal cual", 15);
        _s.Shot("01-texto");

        // Y de vuelta.
        _s.Require(UiSession.Id("MarkdownToggle"), "el boton").Click();
        _s.Require(UiSession.Text("Prueba de Markdown"), "el titulo renderizado otra vez", 15);
    }

    [Fact]
    public void T02_Buscar_marca_en_la_vista_renderizada_y_baja_hasta_la_coincidencia()
    {
        EnsureSampleOpen();
        var search = _s.Require(UiSession.Id("SearchEntry"), "el buscador");
        search.SendKeys("nota");
        Thread.Sleep(1500);
        _s.Shot("02-busqueda");
        // La primera coincidencia («Nota al pie», al final) queda a la vista y el titulo no.
        _s.Require(UiSession.Text("Nota"), "la coincidencia marcada", 10);
        Assert.Null(_s.WaitFor(UiSession.Text("Prueba de Markdown"), 1));
        search.Clear();
        Thread.Sleep(1000);
    }

    [Fact]
    public void T03_Un_enlace_se_abre_fuera_de_la_app()
    {
        EnsureSampleOpen();
        // El enlace ocupa dos lineas: su rectangulo junta las dos y el centro puede caer fuera del
        // texto. Se toca al final de la primera linea («...a la»), arriba a la derecha.
        var link = _s.Require(UiSession.Text("enlace a la web"), "el enlace del documento", 15);
        _s.Driver.ExecuteScript("mobile: clickGesture", new Dictionary<string, object>
        {
            ["x"] = link.Location.X + link.Size.Width - 20,
            ["y"] = link.Location.Y + 15,
        });
        var until = DateTime.UtcNow.AddSeconds(10);
        while (_s.ForegroundPackage() == UiSession.Package && DateTime.UtcNow < until)
        {
            Thread.Sleep(300);
        }

        _s.Shot("03-enlace");
        Assert.NotEqual(UiSession.Package, _s.ForegroundPackage());
        _s.Driver.ActivateApp(UiSession.Package);
        _s.Require(UiSession.Id("MarkdownToggle"), "el lector al volver", 10);
    }

    [Fact]
    public void T04_Sigue_el_tema_oscuro_del_sistema()
    {
        EnsureSampleOpen();
        var viewer = _s.Require(UiSession.Id("ContentViewer"), "el visor");
        // Esquina de arriba a la derecha del documento: fondo, sin texto.
        var x = viewer.Location.X + viewer.Size.Width - 30;
        var y = viewer.Location.Y + 30;
        Assert.True(_s.Luma(x, y) > 200, "En claro, el fondo del documento deberia ser claro.");

        _s.SetNightMode(true);
        Thread.Sleep(2500);
        _s.Require(UiSession.Text("Prueba de Markdown"), "el documento en oscuro", 10);
        _s.Shot("04-oscuro");
        var dark = _s.Luma(x, y);
        _s.SetNightMode(false);
        Thread.Sleep(1500);
        Assert.True(dark < 80, $"En oscuro, el fondo del documento no es oscuro (luminancia {dark}).");
    }
}
