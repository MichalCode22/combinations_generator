import {
  firstPage,
  lastPage,
  nextPage,
  parseGoToPage,
  previousPage,
  slidingPageWindow
} from './pager.logic';

describe('pager logic', () => {
  const totalPages = 10_000_000_000_000_000n;

  it('navigates large page counts without number conversion', () => {
    expect(firstPage(totalPages)).toBe(1n);
    expect(previousPage(9_000_000_000_000_000n, totalPages)).toBe(8_999_999_999_999_999n);
    expect(nextPage(9_000_000_000_000_000n, totalPages)).toBe(9_000_000_000_000_001n);
    expect(lastPage(totalPages)).toBe(totalPages);
  });

  it('builds a bounded window with ellipses around a distant page', () => {
    expect(slidingPageWindow(5_000_000_000_000_000n, totalPages)).toEqual([
      { kind: 'page', page: 1n },
      { kind: 'ellipsis' },
      { kind: 'page', page: 4_999_999_999_999_998n },
      { kind: 'page', page: 4_999_999_999_999_999n },
      { kind: 'page', page: 5_000_000_000_000_000n },
      { kind: 'page', page: 5_000_000_000_000_001n },
      { kind: 'page', page: 5_000_000_000_000_002n },
      { kind: 'ellipsis' },
      { kind: 'page', page: totalPages }
    ]);
  });

  it('accepts only go-to values within the page range', () => {
    expect(parseGoToPage('10000000000000000', totalPages)).toBe(totalPages);
    expect(parseGoToPage('0', totalPages)).toBeNull();
    expect(parseGoToPage('10000000000000001', totalPages)).toBeNull();
    expect(parseGoToPage('1.5', totalPages)).toBeNull();
  });

  it('keeps a short final window free of duplicate pages', () => {
    expect(slidingPageWindow(3n, 3n)).toEqual([
      { kind: 'page', page: 1n },
      { kind: 'page', page: 2n },
      { kind: 'page', page: 3n }
    ]);
  });

  it('clamps navigation and handles one-page totals', () => {
    expect(previousPage(0n, 1n)).toBe(1n);
    expect(nextPage(0n, 1n)).toBe(1n);
    expect(previousPage(1n, 1n)).toBe(1n);
    expect(nextPage(1n, 1n)).toBe(1n);
    expect(firstPage(1n)).toBe(1n);
    expect(lastPage(1n)).toBe(1n);
    expect(slidingPageWindow(1n, 1n)).toEqual([{ kind: 'page', page: 1n }]);
  });

  it('clamps invalid or out-of-range navigation', () => {
    expect(previousPage(100n, 10n)).toBe(9n);
    expect(nextPage(-5n, 10n)).toBe(1n);
    expect(nextPage(100n, 10n)).toBe(10n);
    expect(parseGoToPage('', 10n)).toBeNull();
    expect(parseGoToPage('-1', 10n)).toBeNull();
    expect(parseGoToPage('11', 10n)).toBeNull();
  });
});
