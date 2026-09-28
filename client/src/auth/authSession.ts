type RefreshAccessToken = () => Promise<void>

let accessToken: string | null = null
let refreshAccessToken: RefreshAccessToken | null = null
let activeRefresh: Promise<void> | null = null

export const authSession = {
  getAccessToken: () => accessToken,
  setAccessToken: (token: string | null) => {
    accessToken = token
  },
  setRefreshAccessToken: (refresh: RefreshAccessToken | null) => {
    refreshAccessToken = refresh
  },
  refresh: () => {
    if (!refreshAccessToken) return Promise.reject(new Error('A sess\u00e3o ainda n\u00e3o foi inicializada.'))

    activeRefresh ??= refreshAccessToken().finally(() => {
      activeRefresh = null
    })

    return activeRefresh
  },
}
