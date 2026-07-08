export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly body?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

type RequestOptions = Omit<RequestInit, 'body'> & {
  body?: unknown;
  skipAuth?: boolean;
};

let tokenGetter: (() => string | null) | null = null;
let tenantGetter: (() => string | null) | null = null;
let refreshHandler: (() => Promise<boolean>) | null = null;

export function configureApiClient(
  getToken: () => string | null,
  refresh: () => Promise<boolean>,
  getTenantId?: () => string | null,
) {
  tokenGetter = getToken;
  refreshHandler = refresh;
  tenantGetter = getTenantId ?? null;
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, skipAuth, headers, ...rest } = options;

  const requestHeaders = new Headers(headers);
  if (body !== undefined) {
    requestHeaders.set('Content-Type', 'application/json');
  }

  if (!skipAuth && tokenGetter) {
    const token = tokenGetter();
    if (token) {
      requestHeaders.set('Authorization', `Bearer ${token}`);
    }
  }

  if (!skipAuth && tenantGetter) {
    const tenantId = tenantGetter();
    if (tenantId) {
      requestHeaders.set('X-Tenant-ID', tenantId);
    }
  }

  let response = await fetch(path, {
    ...rest,
    headers: requestHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (response.status === 401 && !skipAuth && refreshHandler) {
    const refreshed = await refreshHandler();
    if (refreshed) {
      const token = tokenGetter?.();
      if (token) {
        requestHeaders.set('Authorization', `Bearer ${token}`);
      }
      response = await fetch(path, {
        ...rest,
        headers: requestHeaders,
        body: body !== undefined ? JSON.stringify(body) : undefined,
      });
    }
  }

  if (!response.ok) {
    let errorBody: unknown;
    try {
      errorBody = await response.json();
    } catch {
      errorBody = undefined;
    }
    throw new ApiError(
      `Request failed: ${response.status} ${response.statusText}`,
      response.status,
      errorBody,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  get: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'GET' }),

  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'POST', body }),

  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'PUT', body }),

  patch: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'PATCH', body }),

  delete: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'DELETE' }),
};
