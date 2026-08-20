export interface CompetitionSummary {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: 'Draft' | 'Upcoming' | 'Ongoing' | 'Finished';
  problemCount: number;
}

export interface ProblemSummary {
  id: string;
  title: string;
  points: number;
  totalSubmissions: number;
  acceptedSubmissions: number;
  solvedByCurrentUser: boolean | null;
}

export interface CompetitionDetail {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: 'Draft' | 'Upcoming' | 'Ongoing' | 'Finished';
  problems: ProblemSummary[];
}

export interface CreateCompetitionRequest {
  name: string;
  startDate: string; // ISO string
  durationHours: number;
  durationMinutes: number;
  publish: boolean;
}

export interface UpdateCompetitionRequest extends CreateCompetitionRequest {}

export interface CompetitionMutationResponse {
  id: string;
  message: string;
}
