//Exporto la funcion para crear la url
export async function createURL(stream, contentType) {
    const buffer = await stream.arrayBuffer(); //guardo el array del buffer 
    const blob = new Blob([buffer], { type: contentType }); //armo el blob con el buffer y content type
    return URL.createObjectURL(blob); //devuelvo la url creada con el blob
}

//Exporto la funcion para liberar la url
export function revokeURL(url) {
    URL.revokeObjectURL(url);
}

//Exporto la funcion para descargar los documentos
export async function documentURL(stream, contentType, nombreArchivo) {
    const url = await createURL(stream, contentType);

    const enlace = document.createElement("a");
    enlace.href = url;
    enlace.download = nombreArchivo;

    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();

    setTimeout(() => revokeURL(url), 1000)

}