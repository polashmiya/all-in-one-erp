export interface ApiClientConfig {
  baseUrl: string;
  getAccessToken?: () => string | null | undefined;
  getBusinessUnitId?: () => string | null | undefined;
  refreshSession?: () => Promise<boolean>;
  onUnauthorized?: () => void;
}

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ApiProblemDetails | undefined,
    readonly body?: unknown,
  ) {
    super(problem?.title ?? problem?.detail ?? `Request failed with status ${status}`);
    this.name = 'ApiError';
  }

  get fieldErrors(): Record<string, string[]> {
    return this.problem?.errors ?? {};
  }
}

const NO_REFRESH_PATHS = ['/auth/login', '/auth/refresh'];

let config: ApiClientConfig = { baseUrl: '' };
let refreshing: Promise<boolean> | null = null;

export function configureApiClient(next: ApiClientConfig) {
  config = { ...next, baseUrl: next.baseUrl.replace(/\/$/, '') };
}

export function getApiBaseUrl() {
  return config.baseUrl;
}

async function readBody(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined;
  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('json')) return response.json();
  const text = await response.text();
  return text.length ? text : undefined;
}

function refreshOnce(): Promise<boolean> {
  refreshing ??= (config.refreshSession?.() ?? Promise.resolve(false)).finally(() => {
    refreshing = null;
  });
  return refreshing;
}

function canRefresh(url: string, hadToken: boolean) {
  return (
    hadToken && !!config.refreshSession && !NO_REFRESH_PATHS.some((path) => url.includes(path))
  );
}

export async function customFetch<T>(
  url: string,
  init: RequestInit = {},
  retried = false,
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');

  const token = config.getAccessToken?.();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const businessUnitId = config.getBusinessUnitId?.();
  if (businessUnitId) headers.set('X-Business-Unit', businessUnitId);

  const response = await fetch(`${config.baseUrl}${url}`, { ...init, headers });
  const body = await readBody(response);

  if (!response.ok) {
    if (response.status === 401 && !retried && canRefresh(url, !!token) && (await refreshOnce())) {
      return customFetch<T>(url, init, true);
    }
    if (response.status === 401) config.onUnauthorized?.();
    const problem = body && typeof body === 'object' ? (body as ApiProblemDetails) : undefined;
    throw new ApiError(response.status, problem, body);
  }

  return body as T;
}

export function errorStatus(error: unknown): number {
  return error instanceof ApiError ? error.status : 0;
}

export function isTokenActive(
  token: string | null | undefined,
  expiresAt: string | null | undefined,
  now = Date.now(),
) {
  return !!token && !!expiresAt && Date.parse(expiresAt) > now;
}

export type ErrorType<_Error> = ApiError;

export type BodyType<BodyData> = BodyData;

export default customFetch;
