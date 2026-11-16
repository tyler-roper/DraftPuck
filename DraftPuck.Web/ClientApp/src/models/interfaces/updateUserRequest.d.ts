interface UpdateUserRequest {
  targetUserId?: string
  requesterUserId?: string
  email?: string
  nickname?: string
  bannerId?: string
  titleId?: string
  password?: string
  confirmPassword?: string
  currentPassword?: string
  avatarData?: string
}
