// Node 25+ define un `localStorage` global que queda en undefined si no se arranca con
// --localstorage-file, y tapa el de jsdom. Las pruebas usan uno en memoria.
class MemoriaStorage implements Storage {
  private datos = new Map<string, string>();

  get length(): number {
    return this.datos.size;
  }
  clear(): void {
    this.datos.clear();
  }
  getItem(key: string): string | null {
    return this.datos.get(key) ?? null;
  }
  key(index: number): string | null {
    return [...this.datos.keys()][index] ?? null;
  }
  removeItem(key: string): void {
    this.datos.delete(key);
  }
  setItem(key: string, value: string): void {
    this.datos.set(key, String(value));
  }
}

for (const nombre of ['localStorage', 'sessionStorage'] as const) {
  if (!globalThis[nombre]) {
    Object.defineProperty(globalThis, nombre, { value: new MemoriaStorage(), configurable: true });
  }
}
