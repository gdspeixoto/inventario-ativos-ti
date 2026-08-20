import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '@core/services/api.service';
import type { DocumentItem, TimelineEntityType } from '@app/features/models';

/**
 * Anexos de contratos, ativos e fornecedores.
 *
 * O conteúdo nunca trafega em JSON: sobe como multipart e desce como blob.
 * Serializá-lo em base64 inflaria a carga em um terço e obrigaria o navegador
 * a manter o arquivo inteiro em memória duas vezes.
 */
@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly api = inject(ApiService);

  list(entityType: TimelineEntityType, entityId: string): Promise<DocumentItem[]> {
    return this.api.get<DocumentItem[]>('/documents', {
      params: { entityType, entityId },
    });
  }

  /**
   * Envia o arquivo e resolve quando o servidor confirma.
   *
   * O progresso do upload é ignorado aqui de propósito: os limites são de
   * 25 MB, e uma barra que pisca por um instante distrai mais do que informa.
   */
  async upload(
    entityType: TimelineEntityType,
    entityId: string,
    file: File,
    description?: string | null,
  ): Promise<void> {
    const extra: Record<string, string> = { entityType, entityId };

    if (description) {
      extra['description'] = description;
    }

    await firstValueFrom(this.api.upload('/documents', file, { extra }));
  }

  /** Baixa o anexo pelo diálogo do navegador, preservando o nome original. */
  download(id: string, fileName: string): Promise<void> {
    return this.api.download(`/documents/${id}`, fileName);
  }

  delete(id: string): Promise<void> {
    return this.api.delete<void>(`/documents/${id}`);
  }
}
