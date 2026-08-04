// Auth API functions. Shapes mirror the API contracts in
// modern/Ecommerce.Api/Contracts/AuthDtos.cs (camelCase over the wire).

import { get, post } from './client'

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

export interface UserInfo {
  email: string
  userId: string
  roles: string[]
}

/** POST /api/auth/login — authenticate and receive a JWT. */
export function login(email: string, password: string): Promise<LoginResponse> {
  const body: LoginRequest = { email, password }
  return post<LoginResponse>('/auth/login', body)
}

/** GET /api/auth/me — fetch current user info (requires Bearer token). */
export function getMe(): Promise<UserInfo> {
  return get<UserInfo>('/auth/me', true)
}