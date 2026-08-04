// Fetch-based API client.
// Base URL `/api` is proxied by Vite to http://127.0.0.1:5100 in dev
// (see vite.config.ts). In production, the reverse proxy must route /api
// to the API service.

const BASE_URL = '/api'

/** Shape of an API problem response body. */
export interface ApiError {
  status: number
  message: string
}

function getToken(): string | null {
  try {
    return sessionStorage.getItem('ecommerce.token')
  } catch {
    return null
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  auth?: boolean
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = false } = options

  const headers: Record<string, string> = {
    Accept: 'application/json',
  }

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth) {
    const token = getToken()
    if (token) {
      headers.Authorization = `Bearer ${token}`
    }
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    let message = response.statusText
    try {
      const text = await response.text()
      if (text) {
        try {
          const parsed = JSON.parse(text) as { message?: string; title?: string }
          message = parsed.message ?? parsed.title ?? text
        } catch {
          message = text
        }
      }
    } catch {
      // ignore body parse failure
    }
    const error: ApiError = { status: response.status, message: message || 'Request failed' }
    throw error
  }

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (!contentType.includes('application/json')) {
    return (await response.text()) as unknown as T
  }

  return (await response.json()) as T
}

/** Issue a GET request, optionally authenticated. */
export function get<T>(path: string, auth = false): Promise<T> {
  return request<T>(path, { method: 'GET', auth })
}

/** Issue a POST request with a JSON body, optionally authenticated. */
export function post<T>(path: string, body?: unknown, auth = false): Promise<T> {
  return request<T>(path, { method: 'POST', body, auth })
}