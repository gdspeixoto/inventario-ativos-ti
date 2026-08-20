import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/**
 * PrimeNG theme preset.
 *
 * Rather than restating a palette, the preset *points PrimeNG at our design
 * tokens*: every value below is a `var(--app-…)` reference. One token change in
 * `src/styles/tokens` repaints PrimeNG, Tailwind utilities and hand-written SCSS
 * at the same time, in both light and dark mode.
 *
 * PrimeNG resolves `colorScheme.light` / `colorScheme.dark` against the
 * `darkModeSelector` configured in `app.config.ts` (`[data-theme="dark"]`), which
 * is exactly the selector our token files use.
 */

/** Brand ramp — the only place PrimeNG needs discrete steps. */
const brand = {
  50: 'var(--app-palette-brand-50)',
  100: 'var(--app-palette-brand-100)',
  200: 'var(--app-palette-brand-200)',
  300: 'var(--app-palette-brand-300)',
  400: 'var(--app-palette-brand-400)',
  500: 'var(--app-palette-brand-500)',
  600: 'var(--app-palette-brand-600)',
  700: 'var(--app-palette-brand-700)',
  800: 'var(--app-palette-brand-800)',
  900: 'var(--app-palette-brand-900)',
  950: 'var(--app-palette-brand-950)',
};

const neutral = {
  0: 'var(--app-palette-neutral-0)',
  50: 'var(--app-palette-neutral-50)',
  100: 'var(--app-palette-neutral-100)',
  200: 'var(--app-palette-neutral-200)',
  300: 'var(--app-palette-neutral-300)',
  400: 'var(--app-palette-neutral-400)',
  500: 'var(--app-palette-neutral-500)',
  600: 'var(--app-palette-neutral-600)',
  700: 'var(--app-palette-neutral-700)',
  800: 'var(--app-palette-neutral-800)',
  900: 'var(--app-palette-neutral-900)',
  950: 'var(--app-palette-neutral-950)',
};

const semanticSurface = {
  primary: {
    color: 'var(--app-color-primary)',
    contrastColor: 'var(--app-color-text-on-primary)',
    hoverColor: 'var(--app-color-primary-hover)',
    activeColor: 'var(--app-color-primary-active)',
  },
  highlight: {
    background: 'var(--app-color-primary-subtle)',
    focusBackground: 'var(--app-color-primary-subtle)',
    color: 'var(--app-color-primary)',
    focusColor: 'var(--app-color-primary-hover)',
  },
  formField: {
    background: 'var(--app-color-surface)',
    disabledBackground: 'var(--app-color-surface-sunken)',
    filledBackground: 'var(--app-color-surface-sunken)',
    filledHoverBackground: 'var(--app-color-surface-hover)',
    filledFocusBackground: 'var(--app-color-surface)',
    borderColor: 'var(--app-color-border-strong)',
    hoverBorderColor: 'var(--app-color-text-muted)',
    focusBorderColor: 'var(--app-color-primary)',
    invalidBorderColor: 'var(--app-color-danger)',
    color: 'var(--app-color-text)',
    disabledColor: 'var(--app-color-text-muted)',
    placeholderColor: 'var(--app-color-text-muted)',
    invalidPlaceholderColor: 'var(--app-color-danger)',
    floatLabelColor: 'var(--app-color-text-secondary)',
    floatLabelFocusColor: 'var(--app-color-primary)',
    floatLabelActiveColor: 'var(--app-color-text-secondary)',
    floatLabelInvalidColor: 'var(--app-color-danger)',
    iconColor: 'var(--app-color-text-muted)',
    shadow: 'none',
  },
  text: {
    color: 'var(--app-color-text)',
    hoverColor: 'var(--app-color-text)',
    mutedColor: 'var(--app-color-text-secondary)',
    hoverMutedColor: 'var(--app-color-text)',
  },
  content: {
    background: 'var(--app-color-surface)',
    hoverBackground: 'var(--app-color-surface-hover)',
    borderColor: 'var(--app-color-border)',
    color: 'var(--app-color-text)',
    hoverColor: 'var(--app-color-text)',
  },
  overlay: {
    select: {
      background: 'var(--app-color-surface)',
      borderColor: 'var(--app-color-border)',
      color: 'var(--app-color-text)',
      shadow: 'var(--app-shadow-md)',
    },
    popover: {
      background: 'var(--app-color-surface)',
      borderColor: 'var(--app-color-border)',
      color: 'var(--app-color-text)',
      shadow: 'var(--app-shadow-md)',
    },
    modal: {
      background: 'var(--app-color-surface)',
      borderColor: 'var(--app-color-border)',
      color: 'var(--app-color-text)',
      shadow: 'var(--app-shadow-lg)',
    },
  },
  list: {
    option: {
      focusBackground: 'var(--app-color-surface-hover)',
      selectedBackground: 'var(--app-color-primary-subtle)',
      selectedFocusBackground: 'var(--app-color-primary-subtle)',
      color: 'var(--app-color-text)',
      focusColor: 'var(--app-color-text)',
      selectedColor: 'var(--app-color-primary)',
      selectedFocusColor: 'var(--app-color-primary-hover)',
      icon: {
        color: 'var(--app-color-text-muted)',
        focusColor: 'var(--app-color-text-secondary)',
      },
    },
    optionGroup: {
      background: 'transparent',
      color: 'var(--app-color-text-muted)',
    },
  },
  navigation: {
    item: {
      focusBackground: 'var(--app-color-surface-hover)',
      activeBackground: 'var(--app-color-surface-active)',
      color: 'var(--app-color-text)',
      focusColor: 'var(--app-color-text)',
      activeColor: 'var(--app-color-primary)',
      icon: {
        color: 'var(--app-color-text-muted)',
        focusColor: 'var(--app-color-text-secondary)',
        activeColor: 'var(--app-color-primary)',
      },
    },
    submenuLabel: {
      background: 'transparent',
      color: 'var(--app-color-text-muted)',
    },
    submenuIcon: {
      color: 'var(--app-color-text-muted)',
      focusColor: 'var(--app-color-text-secondary)',
      activeColor: 'var(--app-color-primary)',
    },
  },
  mask: {
    background: 'var(--app-color-backdrop)',
    color: 'var(--app-color-text-inverse)',
  },
};

export const CorporatePreset = definePreset(Aura, {
  primitive: {
    borderRadius: {
      none: '0',
      xs: 'var(--app-radius-sm)',
      sm: 'var(--app-radius-sm)',
      md: 'var(--app-radius-md)',
      lg: 'var(--app-radius-md)',
      xl: 'var(--app-radius-lg)',
    },
  },
  semantic: {
    primary: brand,
    transitionDuration: 'var(--app-duration-normal)',
    focusRing: {
      width: 'var(--app-focus-ring-width)',
      style: 'solid',
      color: 'var(--app-color-focus-ring)',
      offset: 'var(--app-focus-ring-offset)',
      shadow: 'none',
    },
    formField: {
      paddingX: 'var(--app-spacing-sm)',
      paddingY: 'var(--app-spacing-xs)',
      borderRadius: 'var(--app-radius-md)',
      focusRing: {
        width: 'var(--app-focus-ring-width)',
        style: 'solid',
        color: 'var(--app-color-focus-ring)',
        offset: '0',
        shadow: 'none',
      },
    },
    content: {
      borderRadius: 'var(--app-radius-md)',
    },
    colorScheme: {
      light: { surface: neutral, ...semanticSurface },
      dark: { surface: neutral, ...semanticSurface },
    },
  },
  components: {
    button: {
      root: {
        borderRadius: 'var(--app-radius-md)',
        paddingX: 'var(--app-spacing-md)',
        paddingY: 'var(--app-spacing-xs)',
        gap: 'var(--app-spacing-xs)',
        label: { fontWeight: 'var(--app-font-weight-medium)' },
      },
    },
    card: {
      root: {
        background: 'var(--app-color-card)',
        borderRadius: 'var(--app-radius-lg)',
        color: 'var(--app-color-text)',
        shadow: 'var(--app-shadow-xs)',
      },
      body: { padding: 'var(--app-spacing-lg)', gap: 'var(--app-spacing-sm)' },
    },
    toast: {
      root: { borderRadius: 'var(--app-radius-md)' },
    },
    tooltip: {
      root: {
        background: 'var(--app-palette-neutral-800)',
        color: 'var(--app-palette-neutral-0)',
        borderRadius: 'var(--app-radius-sm)',
      },
    },
  },
});
