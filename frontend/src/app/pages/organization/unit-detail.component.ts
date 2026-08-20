import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { TableModule } from 'primeng/table';
import type { MenuItem } from 'primeng/api';
import { OrganizationApiService } from '@app/features/services/business.service';
import { UnitEditFormComponent } from '@app/features/forms/unit-edit-form.component';
import { DeleteDialogComponent } from '@app/features/forms/delete-dialog.component';
import { UnitTreeNodeComponent } from '@app/features/organization/unit-tree-node.component';
import { MANAGEMENT_ROLE_LABELS, UNIT_TYPE_LABELS } from '@app/features/models/enums';
import type { ManagementRole, OrganizationalUnitDetail, OrganizationalUnitType } from '@app/features/models';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { MoneyComponent } from '@shared/components/money/money.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { StatCardComponent } from '@shared/components/stat-card/stat-card.component';
import { StatusBadgeComponent } from '@shared/components/status-badge/status-badge.component';
import { asyncSignal } from '@shared/models/async-state.model';

/**
 * Detalhe de uma unidade organizacional.
 *
 * A árvore da tela anterior responde "o que existe"; esta página responde "o
 * que esta unidade tem" — quantos ativos, quanto custa, quem responde por ela
 * e o que está pendurado abaixo.
 *
 * Os impedimentos de exclusão são exibidos antes de qualquer tentativa. Sem
 * isso o usuário só descobriria que a unidade não pode ser apagada depois de
 * abrir o diálogo e digitar o nome inteiro.
 */
@Component({
  selector: 'app-unit-detail',
  imports: [
    RouterLink,
    Button,
    Menu,
    TableModule,
    AppCardComponent,
    PageHeaderComponent,
    StatCardComponent,
    StatusBadgeComponent,
    MoneyComponent,
    LoadingComponent,
    ErrorStateComponent,
    UnitTreeNodeComponent,
    UnitEditFormComponent,
    DeleteDialogComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './unit-detail.component.html',
  styleUrl: './unit-detail.component.scss',
})
export class UnitDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(OrganizationApiService);

  protected readonly detail = asyncSignal<OrganizationalUnitDetail>();
  protected readonly dialog = signal<'none' | 'edit' | 'delete'>('none');

  protected readonly recordActions: MenuItem[] = [
    {
      label: 'Editar unidade',
      icon: 'pi pi-pencil',
      command: () => this.dialog.set('edit'),
    },
    {
      separator: true,
    },
    {
      label: 'Excluir definitivamente',
      icon: 'pi pi-trash',
      command: () => this.dialog.set('delete'),
    },
  ];

  /**
   * Cópia mutável para a tabela do PrimeNG, que não aceita `readonly`.
   * O modelo permanece imutável — a folga fica contida aqui.
   */
  protected readonly assignmentRows = computed(() => [...(this.detail.data()?.assignments ?? [])]);

  protected readonly deleteAction = (): Promise<void> => {
    const id = this.detail.data()?.id;

    return id ? this.api.deleteUnit(id) : Promise.resolve();
  };

  ngOnInit(): void {
    void this.load();
  }

  protected load(): Promise<OrganizationalUnitDetail | null> {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      return Promise.resolve(null);
    }

    return this.detail.run(() => this.api.getUnit(id));
  }

  protected close(): void {
    this.dialog.set('none');
  }

  protected async afterSave(): Promise<void> {
    this.close();
    await this.load();
  }

  /** Excluída a unidade, não há o que recarregar: volta para a estrutura. */
  protected async afterDelete(): Promise<void> {
    this.close();
    await this.router.navigate(['/organization']);
  }

  protected typeLabel(type: OrganizationalUnitType): string {
    return UNIT_TYPE_LABELS[type] ?? type;
  }

  protected roleLabel(role: ManagementRole): string {
    return MANAGEMENT_ROLE_LABELS[role] ?? role;
  }
}
