export interface UserActivity {
  problemId: string;
  problemTitle: string;
  competitionId: string;
  competitionName: string;
  status: 'Accepted' | 'Wrong' | 'Pending';
  submittedAt: string;
}

export interface UserProfile {
  username: string;
  avatarUrl: string | null;
  country: string;
  region: string | null;
  school: string | null;
  totalScore: number;
  competitionScore: number;
  nationalRank: number;
  createdAt: string;
  recentActivity: UserActivity[];
}

export interface UpdateUserRequest {
  country: string;
  region?: string;
  school?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ModeratorEntry {
  userId: string;
  username: string;
  avatarUrl: string | null;
  promotedAt: string | null;
}
