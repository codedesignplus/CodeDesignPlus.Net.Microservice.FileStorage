using Microsoft.Net.Http.Headers;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Controllers;

/// <summary>
/// La cabecera <c>Content-Disposition</c> de una descarga, con el nombre del archivo tal como se subió.
/// </summary>
/// <remarks>
/// Kestrel solo escribe cabeceras ASCII: con «Cotización (1).pdf» escrito tal cual en <c>filename</c>, la descarga
/// respondía 500 (Kappali pendings/526). El nombre va en <c>filename*=UTF-8''…</c> (RFC 6266 y 5987), que los navegadores
/// prefieren, y <c>filename</c> lleva un respaldo ASCII para los clientes que no lo entienden.
/// </remarks>
public static class DownloadContentDisposition
{
    /// <summary>
    /// Arma el valor de la cabecera.
    /// </summary>
    /// <param name="viewInBrowser"><see langword="true"/> para <c>inline</c>; si no, <c>attachment</c>.</param>
    /// <param name="fileName">El nombre del archivo, con su extensión.</param>
    /// <returns>El valor de la cabecera, solo con caracteres ASCII.</returns>
    public static string For(bool viewInBrowser, string fileName)
    {
        var header = new ContentDispositionHeaderValue(viewInBrowser ? "inline" : "attachment");

        header.SetHttpFileName(fileName);

        return header.ToString();
    }
}
