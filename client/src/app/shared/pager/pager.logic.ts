export type PagerItem = { readonly kind: 'page'; readonly page: bigint } | { readonly kind: 'ellipsis' };

export function firstPage(totalPages: bigint): bigint {
  return totalPages > 0n ? 1n : 0n;
}

export function previousPage(page: bigint, totalPages: bigint): bigint {
  if (totalPages <= 0n) {
    return 0n;
  }

  const currentPage = page > totalPages ? totalPages : page;
  return currentPage > 1n ? currentPage - 1n : 1n;
}

export function nextPage(page: bigint, totalPages: bigint): bigint {
  if (totalPages <= 0n) {
    return 0n;
  }

  if (page < 1n) {
    return 1n;
  }

  return page < totalPages ? page + 1n : totalPages;
}

export function lastPage(totalPages: bigint): bigint {
  return totalPages > 0n ? totalPages : 0n;
}

export function slidingPageWindow(
  currentPage: bigint,
  totalPages: bigint,
  windowSize = 5
): PagerItem[] {
  if (totalPages <= 0n) {
    return [];
  }

  const size = BigInt(Math.max(1, Math.floor(windowSize)));
  const page = currentPage < 1n ? 1n : currentPage > totalPages ? totalPages : currentPage;
  const halfWindow = (size - 1n) / 2n;
  let start = page - halfWindow;
  if (start < 1n) {
    start = 1n;
  }

  let end = start + size - 1n;
  if (end > totalPages) {
    end = totalPages;
    start = end - size + 1n;
    if (start < 1n) {
      start = 1n;
    }
  }

  const items: PagerItem[] = [];
  if (start > 1n) {
    items.push({ kind: 'page', page: 1n });
    if (start > 2n) {
      items.push({ kind: 'ellipsis' });
    }
  }

  for (let value = start; value <= end; value += 1n) {
    items.push({ kind: 'page', page: value });
  }

  if (end < totalPages) {
    if (end < totalPages - 1n) {
      items.push({ kind: 'ellipsis' });
    }
    items.push({ kind: 'page', page: totalPages });
  }

  return items;
}

export function parseGoToPage(value: string, totalPages: bigint): bigint | null {
  if (!/^\d+$/.test(value) || totalPages < 1n) {
    return null;
  }

  const page = BigInt(value);
  return page >= 1n && page <= totalPages ? page : null;
}
