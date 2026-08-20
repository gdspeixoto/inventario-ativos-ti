import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-money',
  imports: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<strong>{{ amount() | currency: currency() : 'symbol' : '1.2-2' : 'pt-BR' }}</strong>`,
})
export class MoneyComponent {
  readonly amount = input.required<number>();
  readonly currency = input('BRL');
}
