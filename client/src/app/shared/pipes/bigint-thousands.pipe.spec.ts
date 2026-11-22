import { BigintThousandsPipe } from './bigint-thousands.pipe';

describe('BigintThousandsPipe', () => {
  const pipe = new BigintThousandsPipe();

  it('formats BigInt values with thousands separators', () => {
    expect(pipe.transform(0n)).toBe('0');
    expect(pipe.transform(1n)).toBe('1');
    expect(pipe.transform(999n)).toBe('999');
    expect(pipe.transform(1000n)).toBe('1,000');
    expect(pipe.transform(2432902008176640000n)).toBe('2,432,902,008,176,640,000');
  });

  it('returns an empty string for missing values', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });
});
