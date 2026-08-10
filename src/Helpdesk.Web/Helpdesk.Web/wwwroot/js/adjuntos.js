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
