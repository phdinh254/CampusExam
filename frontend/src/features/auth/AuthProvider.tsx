import { createContext, useCallback, useContext, useEffect, useMemo, useState, type PropsWithChildren } from "react";

import { authSession } from "../../services/authSession";
import type { DangNhapRequest, DoiMatKhauRequest, PhienDangNhap, TaiKhoanHienTai } from "../../types/api";
import { xacThucApi } from "./api";

type TrangThaiPhien = "dang_tai" | "da_dang_nhap" | "chua_dang_nhap";

type AuthContextValue = {
  trangThai: TrangThaiPhien;
  taiKhoan: TaiKhoanHienTai | null;
  dangNhap: (body: DangNhapRequest) => Promise<TaiKhoanHienTai>;
  dangXuat: () => Promise<void>;
  doiMatKhau: (body: DoiMatKhauRequest) => Promise<TaiKhoanHienTai>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [trangThai, setTrangThai] = useState<TrangThaiPhien>("dang_tai");
  const [taiKhoan, setTaiKhoan] = useState<TaiKhoanHienTai | null>(null);

  const apDungPhien = useCallback((phien: PhienDangNhap) => {
    authSession.setAccessToken(phien.accessToken);
    setTaiKhoan(phien.taiKhoan);
    setTrangThai("da_dang_nhap");
    return phien.taiKhoan;
  }, []);

  useEffect(() => {
    // httpClient gọi handler này khi gặp 401; các lời gọi đồng thời dùng chung một request.
    authSession.setRefreshHandler(async () => {
      try {
        const phien = await xacThucApi.lamMoi();
        apDungPhien(phien);
        return phien.accessToken;
      } catch {
        return null;
      }
    });
    const huyLangNghe = authSession.onSessionExpired(() => {
      setTaiKhoan(null);
      setTrangThai("chua_dang_nhap");
    });

    // Tải lại trang: access token trong bộ nhớ đã mất, khôi phục bằng cookie refresh.
    void authSession.refresh();

    return () => {
      huyLangNghe();
      authSession.setRefreshHandler(null);
    };
  }, [apDungPhien]);

  const dangNhap = useCallback(
    async (body: DangNhapRequest) => apDungPhien(await xacThucApi.dangNhap(body)),
    [apDungPhien],
  );

  const doiMatKhau = useCallback(
    async (body: DoiMatKhauRequest) => apDungPhien(await xacThucApi.doiMatKhau(body)),
    [apDungPhien],
  );

  const dangXuat = useCallback(async () => {
    try {
      await xacThucApi.dangXuat();
    } finally {
      authSession.clear();
      setTaiKhoan(null);
      setTrangThai("chua_dang_nhap");
    }
  }, []);

  const value = useMemo(
    () => ({ trangThai, taiKhoan, dangNhap, dangXuat, doiMatKhau }),
    [trangThai, taiKhoan, dangNhap, dangXuat, doiMatKhau],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth phải nằm trong AuthProvider.");
  return context;
}

/** Dùng trong layout đã được RequireRole bảo vệ: chắc chắn đã đăng nhập. */
export function useTaiKhoan(): TaiKhoanHienTai {
  const { taiKhoan } = useAuth();
  if (!taiKhoan) throw new Error("useTaiKhoan chỉ dùng bên trong route đã đăng nhập.");
  return taiKhoan;
}
