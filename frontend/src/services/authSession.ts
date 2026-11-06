/**
 * Giữ access token trong bộ nhớ (không dùng localStorage, xem docs/API_DESIGN.md mục 1.5).
 * Refresh token nằm trong cookie HttpOnly nên JavaScript không đọc được; khi tải lại trang,
 * phiên được khôi phục bằng cách gọi handler làm mới.
 */
type RefreshHandler = () => Promise<string | null>;
type Listener = () => void;

let accessToken: string | null = null;
let refreshHandler: RefreshHandler | null = null;
let refreshInFlight: Promise<string | null> | null = null;
const expiredListeners = new Set<Listener>();

export const authSession = {
  getAccessToken: () => accessToken,

  setAccessToken(token: string | null) {
    accessToken = token;
  },

  clear() {
    accessToken = null;
  },

  /** Đăng ký hàm gọi API làm mới phiên; trả token mới hoặc null nếu phiên đã hết. */
  setRefreshHandler(handler: RefreshHandler | null) {
    refreshHandler = handler;
  },

  /** Làm mới phiên; các lời gọi đồng thời dùng chung một request. */
  refresh(): Promise<string | null> {
    if (!refreshHandler) {
      return Promise.resolve(null);
    }

    refreshInFlight ??= refreshHandler()
      .catch(() => null)
      .then((token) => {
        accessToken = token;
        if (token === null) {
          expiredListeners.forEach((listener) => listener());
        }
        return token;
      })
      .finally(() => {
        refreshInFlight = null;
      });

    return refreshInFlight;
  },

  /** Lắng nghe sự kiện phiên hết hạn (ví dụ để chuyển về trang đăng nhập). */
  onSessionExpired(listener: Listener) {
    expiredListeners.add(listener);
    return () => {
      expiredListeners.delete(listener);
    };
  },
};
