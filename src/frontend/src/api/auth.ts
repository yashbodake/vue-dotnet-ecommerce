// Auth API functions. Shapes mirror the API contracts in
// modern/Ecommerce.Api/Contracts/AuthDtos.cs (camelCase over the wire).

import { post } from './client'

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  email: string
  roles: string[]
  expiresAt: string
}

/** POST /api/auth/login — authenticate and receive a JWT. */
export function login(email: string, password: string): Promise<LoginResponse> {
  const body: LoginRequest = { email, password }
  return post<LoginResponse>('/auth/login', body)
}

/** POST /api/auth/register — create a new customer account and receive a JWT. */
export function register(email: string, password: string): Promise<LoginResponse> {
  const body: LoginRequest = { email, password }
  return post<LoginResponse>('/auth/register', body)
}
