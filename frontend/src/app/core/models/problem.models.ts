export interface ProblemDetail {
  id: string;
  competitionId: string;
  competitionName: string;
  competitionStatus: 'Draft' | 'Upcoming' | 'Ongoing' | 'Finished';
  title: string;
  body: string;
  points: number;
  totalSubmissions: number;
  acceptedSubmissions: number;
  acceptanceRate: number;
  solvedByCurrentUser: boolean | null;
}

export interface SubmitSolutionResult {
  status: 'Accepted' | 'Wrong';
  message: string;
  pointsEarned: number | null;
}

export interface SubmissionRecord {
  id: string;
  submittedAt: string;
  status: 'Accepted' | 'Wrong' | 'Pending';
  isFirstAccepted: boolean;
}

export interface CreateProblemRequest {
  title: string;
  body: string;
  points: number;
}

export interface UpdateProblemRequest extends CreateProblemRequest {
  replaceInputFile: boolean;
  replaceOutputFile: boolean;
}

export interface ProblemEditFiles {
  inputFileUrl: string | null;
  outputFileUrl: string | null;
}

export interface ProblemMutationResponse {
  id: string;
  message: string;
}
