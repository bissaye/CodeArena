export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
  country: string;
  email?: string;
  phoneNumber?: string;
  region?: string;
  school?: string;
}

export interface AuthResponse {
  token: string;
  username: string;
  role: string;
  expiresAt: string;
}

export interface CurrentUser {
  id: string;
  username: string;
  role: 'Participant' | 'Moderator' | 'Admin';
  expiresAt: Date;
}
