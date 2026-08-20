import { Injectable, inject } from '@angular/core';
import { PlatformService } from './platform.service';

/** Prefix applied to every key so several apps can share one origin safely. */
const NAMESPACE = 'app';

/**
 * Typed, namespaced and failure-tolerant wrapper over Web Storage.
 *
 * Never throws: a full quota, a blocked storage or a server render all degrade
 * to `null`, which callers treat as "no stored preference".
 */
@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly platform = inject(PlatformService);
  /** In-memory fallback so the app still behaves correctly without storage. */
  private readonly memory = new Map<string, string>();

  get<T>(key: string, kind: 'local' | 'session' = 'local'): T | null {
    const raw = this.readRaw(this.namespaced(key), kind);
    if (raw === null) {
      return null;
    }
    try {
      return JSON.parse(raw) as T;
    } catch {
      // A value written by an older version — drop it instead of crashing.
      this.remove(key, kind);
      return null;
    }
  }

  set<T>(key: string, value: T, kind: 'local' | 'session' = 'local'): void {
    const namespaced = this.namespaced(key);
    const raw = JSON.stringify(value);
    const storage = this.platform.storage(kind);
    if (storage) {
      try {
        storage.setItem(namespaced, raw);
        return;
      } catch {
        // Quota exceeded — fall through to the in-memory map.
      }
    }
    this.memory.set(namespaced, raw);
  }

  remove(key: string, kind: 'local' | 'session' = 'local'): void {
    const namespaced = this.namespaced(key);
    this.platform.storage(kind)?.removeItem(namespaced);
    this.memory.delete(namespaced);
  }

  /** Clears only this application's keys, leaving other apps on the origin intact. */
  clear(kind: 'local' | 'session' = 'local'): void {
    const storage = this.platform.storage(kind);
    if (storage) {
      const prefix = `${NAMESPACE}:`;
      Object.keys(storage)
        .filter((key) => key.startsWith(prefix))
        .forEach((key) => storage.removeItem(key));
    }
    this.memory.clear();
  }

  private readRaw(namespacedKey: string, kind: 'local' | 'session'): string | null {
    const storage = this.platform.storage(kind);
    if (storage) {
      const value = storage.getItem(namespacedKey);
      if (value !== null) {
        return value;
      }
    }
    return this.memory.get(namespacedKey) ?? null;
  }

  private namespaced(key: string): string {
    return `${NAMESPACE}:${key}`;
  }
}
