import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { ProgressBar } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';
import { LicenseService } from '@app/features/services/asset.service';
import { OrganizationApiService } from '@app/features/services/business.service';
import { licenses as demoLicenses } from '@app/features/demo/demo-data';
import { DEMO_MODE } from '@app/features/demo/demo-mode';
import type {
  LicenseListItem,
  OrganizationalUnitTree,
  PagedResult,
} from '@app/features/models';
import { LicenseFormComponent } from '@app/features/forms/license-form.component';
import { PriceAdjustmentFormComponent } from '@app/features/forms/price-adjustment-form.component';
import { QuantityFormComponent } from '@app/features/forms/asset-action-forms.component';
import { UsageFormComponent } from '@app/features/forms/organization-forms.component';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { DemoNoticeComponent } from '@shared/components/demo-notice/demo-notice.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { asyncSignal } from '@shared/models/async-state.model';

/** Qual modal está aberto. Um de cada vez, por construção. */
type OpenDialog = 'none' | 'create' | 'adjustment' | 'quantity' | 'usage';

@Component({
  selector: 'app-licenses',
  imports: [
    DatePipe,
    RouterLink,
    FormsModule,
    Button,
    InputText,
    TableModule,
    ProgressBar,
    AppCardComponent,
    PageHeaderComponent,
    MoneyComponent,
    StatusBadgeComponent,
    LoadingComponent,
    ErrorStateComponent,
    DemoNoticeComponent,
    LicenseFormComponent,
    PriceAdjustmentFormComponent,
    QuantityFormComponent,
    UsageFormComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './licenses.component.html',
  styleUrl: './licenses.component.scss',
})
export class LicensesComponent implements OnInit {
  private readonly api = inject(LicenseService);
  private readonly organization = inject(OrganizationApiService);

  protected readonly demoMode = inject(DEMO_MODE);

  protected readonly records = asyncSignal<PagedResult<LicenseListItem>>();
  protected readonly search = signal('');
  protected readonly units = signal<readonly OrganizationalUnitTree[]>([]);

  protected readonly dialog = signal<OpenDialog>('none');
  protected readonly selected = signal<LicenseListItem | null>(null);

  /**
   * Em demonstração, os dados fictícios são a fonte — e a tela avisa.
   * Fora dela, o que aparece é exclusivamente o que a API devolveu: uma falha
   * mostra o estado de erro, e não números plausíveis inventados.
   */
  protected readonly items = computed(() =>
    this.demoMode ? [...demoLicenses] : [...(this.records.data()?.items ?? [])],
  );

  ngOnInit(): void {
    void this.load();
    void this.loadUnits();
  }

  protected load(): Promise<PagedResult<LicenseListItem> | null> {
    if (this.demoMode) {
      return Promise.resolve(null);
    }

    return this.records.run(() => this.api.list({ search: this.search(), pageSize: 50 }));
  }

  protected open(dialog: OpenDialog, item?: LicenseListItem): void {
    this.selected.set(item ?? null);
    this.dialog.set(dialog);
  }

  protected close(): void {
    this.dialog.set('none');
  }

  /**
   * Recarrega a listagem depois de uma operação.
   *
   * A tela precisa refletir o novo estado — valor reajustado, quantidade
   * alterada — e não o que estava em memória antes da escrita.
   */
  protected async afterSave(): Promise<void> {
    this.close();
    await this.load();
  }

  private async loadUnits(): Promise<void> {
    if (this.demoMode) {
      return;
    }

    try {
      this.units.set(await this.organization.tree());
    } catch {
      // A lista de unidades só alimenta o formulário de cadastro; sem ela, o
      // restante da tela continua utilizável.
      this.units.set([]);
    }
  }
}
