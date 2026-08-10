
//Exporto la funcion para descarga de reportes
export async function descargarArchivo(stream, nombreArchivo){
    //Creo la url con el blob que me dio el buffer
    const buffer = await stream.arrayBuffer();
    const blob = new Blob([buffer], { type: "application/pdf"});
    const url = URL.createObjectURL(blob);

    //Creo un <a> temporal, click por codigo y lo saco
    const a = document.createElement("a");
    a.href = url;
    a.download = nombreArchivo;
    document.body.appendChild(a);
    a.click();
    a.remove();

    //Revoco la url
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}