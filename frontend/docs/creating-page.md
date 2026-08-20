# Criando uma página

Receita completa para uma tela de listagem — o caso mais comum em sistemas internos.

> Atalho: copie `src/app/pages/example/` e adapte. Ela já contém tudo que este documento descreve.

---

## 1. Estrutura

```
src/app/pages/users/
├── user-list.component.ts
├── user-list.component.html
├── user-list.component.scss
├── user-list.component.spec.ts
├── services/user.service.ts
└── models/user.model.ts
```

---

## 2. Modelo

```ts
// pages/users/models/user.model.ts
export interface User {
  id: string;
  name: string;
  email: string;
  active: boolean;
  createdAt: string;
}

export interface UserFilters {
  term?: string;
  active?: boolean;
}
```

---

## 3. Serviço

Serviços de feature usam `ApiService`, nunca `HttpClient`.

```ts
// pages/users/services/user.service.ts
import { Injectable, inject } from '@angular/core';
import { ApiService } from '@core/services/api.service';
import { toPageParams, type Page, type PageRequest } from '@shared/models/pagination.model';
import type { User, UserFilters } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = inject(ApiService);

  list(request: PageRequest, filters: UserFilters = {}): Promise<Page<User>> {
    return this.api.get<Page<User>>('/users', {
      params: { ...toPageParams(request), ...filters },
    });
  }

  getById(id: string): Promise<User> {
    return this.api.get<User>(`/users/${id}`);
  }

  create(user: Omit<User, 'id' | 'createdAt'>): Promise<User> {
    return this.api.post<User>('/users', user);
  }

  update(id: string, changes: Partial<User>): Promise<User> {
    return this.api.patch<User>(`/users/${id}`, changes);
  }

  remove(id: string): Promise<void> {
    return this.api.delete<void>(`/users/${id}`);
  }
}
```

---

## 4. Componente

```ts
// pages/users/user-list.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { ConfirmService } from '@core/services/confirm.service';
import { ToastService } from '@core/services/toast.service';
import { TranslatePipe } from '@core/i18n/translate.pipe';
import { AppCardComponent } from '@shared/components/app-card/app-card.component';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@shared/components/error-state/error-state.component';
import { LoadingComponent } from '@shared/components/loading/loading.component';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { asyncSignal } from '@shared/models/async-state.model';
import { DEFAULT_PAGE_SIZE, type Page } from '@shared/models/pagination.model';
import { UserService } from './services/user.service';
import type { User } from './models/user.model';

@Component({
  selector: 'app-user-list',
  imports: [
    Button,
    TableModule,
    TranslatePipe,
    AppCardComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    LoadingComponent,
    PageHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent implements OnInit {
  private readonly users = inject(UserService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  protected readonly page = asyncSignal<Page<User>>();
  protected readonly pageIndex = signal(0);

  ngOnInit(): void {
    void this.load();
  }

  protected load(): Promise<Page<User> | null> {
    return this.page.run(() =>
      this.users.list({ page: this.pageIndex(), size: DEFAULT_PAGE_SIZE }),
    );
  }

  protected async remove(user: User): Promise<void> {
    if (!(await this.confirm.askDelete(user.name))) {
      return;
    }
    await this.users.remove(user.id);
    this.toast.success('users.removed');
    void this.load();
  }
}
```

---

## 5. Template

Sempre nesta ordem: cabeçalho → card → os quatro estados.

```html
<app-page-header title="users.title" description="users.subtitle" icon="pi pi-users">
  <p-button [label]="'users.new' | translate" icon="pi pi-plus" size="small" (onClick)="novo()" />
</app-page-header>

<app-card title="users.list" [flush]="true">
  @if (page.isLoading()) {
  <app-loading message="common.loading" />
  } @else if (page.hasError()) {
  <app-error-state [error]="page.error()" (retry)="load()" />
  } @else if (page.isEmpty()) {
  <app-empty-state
    title="users.emptyTitle"
    message="users.emptyMessage"
    actionLabel="users.new"
    (action)="novo()"
  />
  } @else {
  <p-table [value]="page.data()?.content ?? []" dataKey="id">
    <ng-template #header>
      <tr>
        <th scope="col">{{ 'users.name' | translate }}</th>
        <th scope="col">{{ 'users.email' | translate }}</th>
        <th scope="col"><span class="sr-only">{{ 'common.actions' | translate }}</span></th>
      </tr>
    </ng-template>

    <ng-template #body let-user>
      <tr>
        <td>{{ user.name }}</td>
        <td>{{ user.email }}</td>
        <td class="user-list__actions">
          <p-button
            *appHasPermission="'user:delete'"
            icon="pi pi-trash"
            severity="danger"
            [text]="true"
            [rounded]="true"
            size="small"
            [ariaLabel]="'common.delete' | translate"
            (onClick)="remove(user)"
          />
        </td>
      </tr>
    </ng-template>
  </p-table>
  }
</app-card>
```

---

## 6. Estilos

```scss
@use 'tokens' as tokens;

:host {
  display: block;
}

.user-list__actions {
  width: 4rem;
  text-align: right;
}
```

---

## 7. Rota

```ts
// app.routes.ts — dentro dos children do shell
{
  path: 'users',
  title: 'users.title',
  data: { breadcrumb: 'nav.users' },
  canActivate: [permissionGuard('user:read')],
  loadComponent: () => import('@pages/users/user-list.component').then((m) => m.UserListComponent),
}
```

`authGuard` já está aplicado no nó pai — não repita.

---

## 8. Menu

```ts
// config/menu.config.ts
{
  id: 'users',
  label: 'nav.users',
  icon: 'pi pi-users',
  route: '/users',
  permissions: ['user:read'],
}
```

---

## 9. Traduções

Adicione em **todos** os bundles de `public/i18n/`:

```jsonc
// pt-BR.json
"users": {
  "title": "Usuários",
  "subtitle": "Gerencie os usuários com acesso ao sistema",
  "list": "Lista de usuários",
  "new": "Novo usuário",
  "name": "Nome",
  "email": "E-mail",
  "removed": "Usuário removido com sucesso",
  "emptyTitle": "Nenhum usuário cadastrado",
  "emptyMessage": "Cadastre o primeiro usuário para começar."
}
```

E `nav.users` na seção `nav`.

---

## 10. Teste

```ts
describe('UserListComponent', () => {
  it('mostra o estado vazio quando a página não tem conteúdo', async () => { … });
  it('recarrega a lista após remover um usuário', async () => { … });
});
```

---

## Checklist

- [ ] Serviço usa `ApiService` e é tipado
- [ ] Componente standalone e OnPush
- [ ] Os quatro estados tratados (loading, erro, vazio, dados)
- [ ] `<app-page-header>` como primeiro elemento
- [ ] Rota lazy, com `title` e `data.breadcrumb`
- [ ] Guard de permissão, quando aplicável
- [ ] Item de menu adicionado
- [ ] Traduções em todos os idiomas
- [ ] Nenhuma string fixa no template
- [ ] Ações só com ícone têm `ariaLabel`
- [ ] Testado em desktop, tablet e celular, nos dois temas

---

## Leitura relacionada

- [`creating-component.md`](./creating-component.md) · [`layout.md`](./layout.md) · [`coding-standards.md`](./coding-standards.md)
