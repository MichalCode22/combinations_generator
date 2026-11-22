export interface StartRequest {
  n: number;
}

export interface StartResponse {
  sessionId: string;
  total: string;
}

export interface NextResponse {
  hasMore: boolean;
  index: string | null;
  permutation: number[] | null;
  message: string | null;
}

export interface AllPermutationItem {
  index: string;
  permutation: number[];
}

export interface AllResponse {
  items: AllPermutationItem[];
  page: string;
  totalPages: string;
  startIndex: string | null;
  total: string;
  message: string | null;
}

export interface CurrentResponse {
  index: string | null;
  permutation: number[];
}

export type AppMode = 'start' | 'single' | 'all';

export interface CombinationViewState {
  mode: AppMode;
  n: number | null;
  total: bigint | null;
  currentIndex: bigint | null;
  current: number[];
  hasMore: boolean;
  items: Array<{ index: bigint; permutation: number[] }>;
  page: bigint;
  totalPages: bigint;
  pageSize: number;
  startIndex: bigint | null;
  busy: boolean;
  message: string | null;
  error: string | null;
}
