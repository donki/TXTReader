namespace TXTReader.UITests;

/// <summary>
/// E2E de las vistas de datos (General 8.6/8.7): abrir un CSV, un JSON y un XML desde otra app, verlos
/// con formato (tabla y coloreado) y pasar al texto tal cual con el boton. Los textos se buscan dentro
/// del WebView por su accesibilidad.
/// </summary>
[Collection(UiCollection.Name)]
[TestCaseOrderer("TXTReader.UITests.ByNameOrderer", "TXTReader.UITests")]
public sealed class DataUiTests
{
    private readonly UiSession _s;

    public DataUiTests(UiSession session) => _s = session;

    private void Open(string sample, string mime)
    {
        var path = _s.Push(sample);
        _s.OpenWithApp(path, mime);
        _s.Require(UiSession.Id("MarkdownToggle"), "el boton de la vista con formato", 30);
    }

    [Fact]
    public void T01_Un_csv_se_abre_como_tabla_y_el_boton_alterna()
    {
        Open("datos.csv", "text/csv");
        // Con formato: las cabeceras y los valores salen como celdas.
        _s.Require(UiSession.Text("poblacion"), "la cabecera de la tabla", 15);
        _s.Require(UiSession.Text("Barcelona"), "una celda", 10);
        _s.Shot("01-csv-tabla");

        _s.Require(UiSession.Id("MarkdownToggle"), "el boton").Click();
        _s.Require(UiSession.TextStartsWith("ciudad,poblacion"), "el texto tal cual", 15);
        _s.Shot("01-csv-texto");
    }

    [Fact]
    public void T02_Un_json_se_abre_con_formato()
    {
        Open("datos.json", "application/json");
        _s.Require(UiSession.Text("Barcelona"), "un valor del JSON", 15);
        _s.Shot("02-json");
        _s.Require(UiSession.Id("MarkdownToggle"), "el boton").Click();
        _s.Require(UiSession.Text("Barcelona"), "el valor en el texto tal cual", 10);
    }

    [Fact]
    public void T03_Un_xml_se_abre_con_formato()
    {
        Open("datos.xml", "text/xml");
        _s.Require(UiSession.Text("poblacion"), "una etiqueta del XML", 15);
        _s.Shot("03-xml");
        _s.Require(UiSession.Id("MarkdownToggle"), "el boton").Click();
        _s.Require(UiSession.TextStartsWith("<?xml"), "el texto tal cual", 15);
    }
}
