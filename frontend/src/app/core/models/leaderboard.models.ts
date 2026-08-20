export interface LeaderboardEntry {
  rank: number;
  userId: string;
  username: string;
  avatarUrl: string | null;
  country: string;
  region: string | null;
  score: number;
}

export interface LeaderboardPage {
  total: number;
  offset: number;
  limit: number;
  refreshedAt: string;
  entries: LeaderboardEntry[];
}

export interface LeaderboardFilters {
  country?: string;
  region?: string;
  school?: string;
  competitionId?: string;
  scoreMin?: number;
  scoreMax?: number;
  competitionOnly?: boolean;
  search?: string;
  offset?: number;
  limit?: number;
}
