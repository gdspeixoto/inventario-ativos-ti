import type { AppSettings } from '@core/app-settings';

/**
 * Start here when creating a new system from this template.
 *
 * Everything below is branding and optional UI — no behaviour depends on the
 * exact values, so a new project is renamed in one commit.
 */
export const appSettings: AppSettings = {
  name: 'Controle de Ativos',
  shortName: 'Ativos TI',
  description: 'Gestao de licencas, servidores, contratos, custos, reajustes e timelines do setor',
  version: '1.0.0',
  logoUrl: 'logo.svg',
  titleSeparator: ' · ',
  supportEmail: 'suporte.ti@example.com',
  documentationUrl: '',
  features: {
    globalSearch: true,
    languageSwitcher: true,
    breadcrumbs: true,
    collapsibleSidebar: true,
  },
};
