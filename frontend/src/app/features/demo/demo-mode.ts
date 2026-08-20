import { InjectionToken, inject } from '@angular/core';
import { environment } from '@env/environment';

/**
 * Modo demonstração.
 *
 * Quando ligado, as telas exibem dados fictícios sem chamar a API — útil para
 * apresentar o produto antes de o Keycloak e o backend estarem conectados.
 *
 * A distinção importa: um fallback silencioso, que troca a resposta da API por
 * dados fictícios quando a requisição falha, esconde indisponibilidade e erro
 * de configuração. O usuário veria números plausíveis e concluiria que o
 * sistema está funcionando. Aqui a escolha é explícita e sinalizada na tela.
 */
export const DEMO_MODE = new InjectionToken<boolean>('DEMO_MODE', {
  providedIn: 'root',
  factory: () => environment.features.demoData,
});

export function isDemoMode(): boolean {
  return inject(DEMO_MODE);
}
