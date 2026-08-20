import { describe, expect, it } from 'vitest';
import { InitialsPipe } from './initials.pipe';

describe('InitialsPipe', () => {
  const pipe = new InitialsPipe();

  it('takes the first and last name', () => {
    expect(pipe.transform('Gabriel dos Santos')).toBe('GS');
  });

  it('handles a single name', () => {
    expect(pipe.transform('Ana')).toBe('A');
  });

  it('derives initials from an email address', () => {
    expect(pipe.transform('ana.souza@empresa.com.br')).toBe('AB');
  });

  it('falls back to a placeholder for empty input', () => {
    expect(pipe.transform('')).toBe('?');
    expect(pipe.transform(null)).toBe('?');
  });
});
