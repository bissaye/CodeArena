export interface NotificationDto {
  id: string;
  type: 'SubmissionAccepted' | 'SubmissionWrong' | 'CompetitionStarting' | 'CompetitionStarted' | 'BadgeEarned';
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}

export interface NotificationsPage {
  total: number;
  unreadCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  items: NotificationDto[];
}
