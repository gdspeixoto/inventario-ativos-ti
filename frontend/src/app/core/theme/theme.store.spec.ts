import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { ThemeStore } from './theme.store';
import { THEME_CONFIG } from './theme.tokens';
import { themeConfig } from '@config/theme.config';

describe('ThemeStore', () => {
  let store: ThemeStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: THEME_CONFIG, useValue: { ...themeConfig, defaultMode: 'system' } }],
    });
    store = TestBed.inject(ThemeStore);
  });

  it('resolves `system` against the OS preference', () => {
    expect(store.resolved()).toBe('light');

    store.setSystemPrefersDark(true);
    expect(store.resolved()).toBe('dark');
    expect(store.isDark()).toBe(true);
  });

  it('an explicit mode overrides the OS preference', () => {
    store.setSystemPrefersDark(true);
    store.setMode('light');

    expect(store.resolved()).toBe('light');
    expect(store.isFollowingSystem()).toBe(false);
  });

  it('toggling from `system` pins the opposite of what is showing', () => {
    store.setSystemPrefersDark(true);
    store.toggle();

    expect(store.mode()).toBe('light');
    expect(store.isFollowingSystem()).toBe(false);
  });
});
