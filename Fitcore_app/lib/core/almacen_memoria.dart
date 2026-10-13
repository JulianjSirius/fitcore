final Map<String, String> _valores = {};

String? leer(String clave) => _valores[clave];

void escribir(String clave, String valor) => _valores[clave] = valor;

void borrar(String clave) => _valores.remove(clave);
