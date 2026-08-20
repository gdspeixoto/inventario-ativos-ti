import type { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Converte a data do `p-datepicker` para o formato que a API espera.
 *
 * O backend usa `DateOnly`, que aceita exclusivamente `yyyy-MM-dd`. Enviar o
 * ISO completo (`toISOString()`) falharia por dois motivos: traz hora e, pior,
 * converte para UTC — em fuso negativo, uma data escolhida à noite viraria o
 * dia anterior. Por isso a formatação usa os componentes locais.
 */
export function toDateOnly(value: Date | null | undefined): string | null {
  if (!value) {
    return null;
  }

  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');

  return `${year}-${month}-${day}`;
}

/** Converte `yyyy-MM-dd` da API para `Date` local, sem deslocamento de fuso. */
export function fromDateOnly(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }

  const [year, month, day] = value.split('-').map(Number);

  if (!year || !month || !day) {
    return null;
  }

  return new Date(year, month - 1, day);
}

/** Texto vazio vira `null`: o backend trata string em branco como ausência. */
export function trimmedOrNull(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

/**
 * Recusa um valor igual ao atual.
 *
 * O domínio rejeita reajuste e mudança de quantidade sem alteração real; barrar
 * antes do envio evita uma ida ao servidor para receber um 422 previsível.
 */
export function differentFrom(current: () => number | null | undefined): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as number | null;
    const reference = current();

    if (value === null || value === undefined || reference === null || reference === undefined) {
      return null;
    }

    return value === reference ? { sameValue: true } : null;
  };
}

/** Exige que a data do campo seja posterior à de outro campo do mesmo grupo. */
export function dateAfter(startField: string, endField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const start = group.get(startField)?.value as Date | null;
    const end = group.get(endField)?.value as Date | null;

    if (!start || !end) {
      return null;
    }

    if (end.getTime() > start.getTime()) {
      return null;
    }

    // O erro é colocado no campo final, que é onde o usuário vai corrigir.
    group.get(endField)?.setErrors({ dateOrder: true });
    return { dateOrder: true };
  };
}
