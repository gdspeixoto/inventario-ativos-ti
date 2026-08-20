/* eslint-disable no-console -- this service *is* the console boundary */
import { Injectable } from '@angular/core';
import { environment } from '@env/environment';

export type LogLevel = 'debug' | 'info' | 'warn' | 'error' | 'silent';

/** `silent` maps to nothing, which is what makes it silent. */
const CONSOLE_METHODS: Record<LogLevel, ((...args: unknown[]) => void) | null> = {
  debug: (...args) => console.log(...args),
  info: (...args) => console.info(...args),
  warn: (...args) => console.warn(...args),
  error: (...args) => console.error(...args),
  silent: null,
};

const LEVEL_WEIGHT: Record<LogLevel, number> = {
  debug: 10,
  info: 20,
  warn: 30,
  error: 40,
  silent: 100,
};

/**
 * Thin logging facade.
 *
 * Everything funnels through here so that shipping logs to an observability
 * backend later (Sentry, Application Insights, Grafana Faro) is a change in one
 * file. `no-console` is enforced everywhere else by ESLint.
 */
@Injectable({ providedIn: 'root' })
export class LoggerService {
  private readonly threshold = LEVEL_WEIGHT[environment.logLevel as LogLevel] ?? LEVEL_WEIGHT.info;

  debug(message: string, ...details: unknown[]): void {
    this.write('debug', null, message, details);
  }

  info(message: string, ...details: unknown[]): void {
    this.write('info', null, message, details);
  }

  warn(message: string, ...details: unknown[]): void {
    this.write('warn', null, message, details);
  }

  error(message: string, ...details: unknown[]): void {
    this.write('error', null, message, details);
  }

  /** Returns a logger that prefixes every message with `[context]`. */
  forContext(context: string): ScopedLogger {
    return {
      debug: (message, ...details) => this.write('debug', context, message, details),
      info: (message, ...details) => this.write('info', context, message, details),
      warn: (message, ...details) => this.write('warn', context, message, details),
      error: (message, ...details) => this.write('error', context, message, details),
    };
  }

  private write(
    level: LogLevel,
    context: string | null,
    message: string,
    details: unknown[],
  ): void {
    if (LEVEL_WEIGHT[level] < this.threshold) {
      return;
    }
    const prefix = context ? `[${context}]` : '[app]';
    const write = CONSOLE_METHODS[level];
    write?.(`${prefix} ${message}`, ...details);
  }
}

export interface ScopedLogger {
  debug(message: string, ...details: unknown[]): void;
  info(message: string, ...details: unknown[]): void;
  warn(message: string, ...details: unknown[]): void;
  error(message: string, ...details: unknown[]): void;
}
