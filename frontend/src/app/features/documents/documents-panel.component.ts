import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { DocumentService } from '@app/features/services/document.service';
import { DeleteDialogComponent } from '@app/features/forms/delete-dialog.component';
import {
  DOCUMENT_ALLOWED_TYPES,
  DOCUMENT_MAX_SIZE_BYTES,
  type DocumentItem,
  type TimelineEntityType,
} from '@app/features/models';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { ToastService } from '@core/services/toast.service';

/**
 * Anexos de uma entidade.
 *
 * Tamanho e tipo são verificados aqui antes do envio. Não substitui a
 * validação do servidor — que é a que vale —, mas evita que o usuário espere
 * o upload de um arquivo de 40 MB para só então descobrir que ele não seria
 * aceito.
 */
@Component({
  selector: 'app-documents-panel',
  imports: [DatePipe, Button, Message, AppCardComponent, DeleteDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-card title="Anexos" icon="pi pi-paperclip">
      <div class="toolbar">
        <input
          #fileInput
          type="file"
          class="toolbar__input"
          [accept]="acceptAttribute"
          [disabled]="uploading()"
          (change)="onFileSelected($event)"
        />

        <p-button
          label="Anexar arquivo"
          icon="pi pi-upload"
          size="small"
          [loading]="uploading()"
          [disabled]="disabled() || uploading()"
          (onClick)="fileInput.click()"
        />

        <span class="toolbar__hint">PDF, imagem, Word, Excel ou CSV — até 25 MB.</span>
      </div>

      @if (error(); as message) {
        <p-message severity="error" styleClass="documents__error">{{ message }}</p-message>
      }

      @if (loading()) {
        <p class="empty">Carregando anexos…</p>
      } @else if (items().length === 0) {
        <p class="empty">Nenhum arquivo anexado.</p>
      } @else {
        <ul class="list">
          @for (item of items(); track item.id) {
            <li class="list__item">
              <i class="pi" [class]="iconFor(item.contentType)" aria-hidden="true"></i>

              <div class="list__info">
                <button type="button" class="list__name" (click)="download(item)">
                  {{ item.fileName }}
                </button>

                <small>
                  {{ formatSize(item.sizeInBytes) }}
                  @if (item.uploadedByName) {
                    · {{ item.uploadedByName }}
                  }
                  · {{ item.createdAt | date: 'dd/MM/yyyy HH:mm' }}
                </small>

                @if (item.description) {
                  <small class="list__description">{{ item.description }}</small>
                }
              </div>

              <p-button
                icon="pi pi-download"
                [text]="true"
                [rounded]="true"
                size="small"
                ariaLabel="Baixar anexo"
                (onClick)="download(item)"
              />

              <p-button
                icon="pi pi-trash"
                [text]="true"
                [rounded]="true"
                size="small"
                severity="danger"
                ariaLabel="Excluir anexo"
                [disabled]="disabled()"
                (onClick)="removing.set(item)"
              />
            </li>
          }
        </ul>
      }
    </app-card>

    @if (removing(); as item) {
      <!--
        Anexo não tem alternativa a excluir: ou o arquivo fica, ou some. Por
        isso o diálogo não oferece desativação.
      -->
      <app-delete-dialog
        [visible]="true"
        [itemName]="item.fileName"
        [action]="deleteAction"
        [canDeactivate]="false"
        (deleted)="afterDelete()"
        (cancelled)="removing.set(null)"
      />
    }
  `,
  styles: `
    .toolbar {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
      margin-bottom: 0.75rem;
    }

    .toolbar__input {
      display: none;
    }

    .toolbar__hint {
      color: var(--app-color-text-muted);
      font-size: 0.75rem;
    }

    .list {
      margin: 0;
      padding: 0;
      list-style: none;
      display: grid;
      gap: 0.25rem;
    }

    .list__item {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      padding: 0.5rem 0.25rem;
      border-radius: 6px;
    }

    .list__item:hover {
      background: var(--surface-100);
    }

    .list__item > .pi {
      color: var(--app-color-text-muted);
    }

    .list__info {
      flex: 1 1 auto;
      min-width: 0;
      display: grid;
    }

    .list__name {
      justify-self: start;
      padding: 0;
      border: 0;
      background: transparent;
      color: inherit;
      font: inherit;
      font-weight: 600;
      text-align: left;
      cursor: pointer;
      word-break: break-word;
    }

    .list__name:hover {
      text-decoration: underline;
    }

    .list__info small {
      color: var(--app-color-text-muted);
      font-size: 0.75rem;
    }

    .list__description {
      margin-top: 0.15rem;
    }

    .empty {
      margin: 0;
      color: var(--app-color-text-muted);
      font-size: 0.875rem;
    }

    :host ::ng-deep .documents__error {
      display: block;
      margin-bottom: 0.75rem;
    }
  `,
})
export class DocumentsPanelComponent {
  private readonly api = inject(DocumentService);
  private readonly toast = inject(ToastService);

  readonly entityType = input.required<TimelineEntityType>();
  readonly entityId = input.required<string>();
  readonly disabled = input(false);

  protected readonly items = signal<DocumentItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly uploading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly removing = signal<DocumentItem | null>(null);

  protected readonly acceptAttribute = DOCUMENT_ALLOWED_TYPES.join(',');

  protected readonly deleteAction = (): Promise<void> => {
    const id = this.removing()?.id;

    return id ? this.api.delete(id) : Promise.resolve();
  };

  constructor() {
    // `effect` em vez de carregar no construtor: os inputs de signal ainda não
    // têm valor quando o construtor roda, e a lista viria vazia.
    effect(() => {
      const id = this.entityId();
      const type = this.entityType();

      if (id) {
        void this.fetch(type, id);
      }
    });
  }

  /** Recarrega a lista com a entidade corrente. */
  reload(): Promise<void> {
    return this.fetch(this.entityType(), this.entityId());
  }

  private async fetch(entityType: TimelineEntityType, entityId: string): Promise<void> {
    if (!entityId) {
      return;
    }

    this.loading.set(true);

    try {
      this.items.set(await this.api.list(entityType, entityId));
      this.error.set(null);
    } catch {
      // O interceptor já notificou; a lista vazia comunica o resto.
      this.items.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    // Limpa já: sem isso, escolher o mesmo arquivo de novo não dispara evento.
    input.value = '';

    if (!file) {
      return;
    }

    const rejection = this.validate(file);

    if (rejection) {
      this.error.set(rejection);

      return;
    }

    this.error.set(null);
    this.uploading.set(true);

    try {
      await this.api.upload(this.entityType(), this.entityId(), file);
      this.toast.success('Arquivo anexado.', 'common.success', { raw: true });
      await this.reload();
    } catch {
      this.error.set('Não foi possível anexar o arquivo.');
    } finally {
      this.uploading.set(false);
    }
  }

  protected async download(item: DocumentItem): Promise<void> {
    try {
      await this.api.download(item.id, item.fileName);
    } catch {
      this.error.set('Não foi possível baixar o arquivo.');
    }
  }

  protected async afterDelete(): Promise<void> {
    this.removing.set(null);
    await this.reload();
  }

  /**
   * Recusa antes do envio o que o servidor recusaria depois — poupando ao
   * usuário a espera de um upload que terminaria em erro.
   */
  private validate(file: File): string | null {
    if (file.size === 0) {
      return 'O arquivo está vazio.';
    }

    if (file.size > DOCUMENT_MAX_SIZE_BYTES) {
      return `O arquivo tem ${this.formatSize(file.size)} e o limite é 25 MB.`;
    }

    if (!DOCUMENT_ALLOWED_TYPES.includes(file.type)) {
      return 'Tipo de arquivo não permitido. Envie PDF, imagem, Word, Excel ou CSV.';
    }

    return null;
  }

  protected formatSize(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(0)} KB`;
    }

    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  protected iconFor(contentType: string): string {
    if (contentType === 'application/pdf') {
      return 'pi-file-pdf';
    }

    if (contentType.startsWith('image/')) {
      return 'pi-image';
    }

    if (contentType.includes('spreadsheetml') || contentType === 'text/csv') {
      return 'pi-file-excel';
    }

    if (contentType.includes('wordprocessingml')) {
      return 'pi-file-word';
    }

    return 'pi-file';
  }
}
