import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { authSession } from "./authSession";
import { ApiError, buildQuery, httpClient } from "./httpClient";

type FetchCall = { url: string; init: RequestInit };

const calls: FetchCall[] = [];
let responses: Array<Response | Error>;

function json(status: number, body: unknown, contentType = "application/json") {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": contentType } });
}

function headerOf(call: FetchCall, name: string) {
  return new Headers(call.init.headers).get(name);
}

beforeEach(() => {
  calls.length = 0;
  responses = [];
  authSession.clear();
  authSession.setRefreshHandler(null);
  vi.stubGlobal("fetch", vi.fn(async (url: string, init: RequestInit) => {
    calls.push({ url, init });
    const next = responses.shift();
    if (!next) throw new Error(`Không có response giả cho ${url}`);
    if (next instanceof Error) throw next;
    return next;
  }));
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("httpClient", () => {
  it("gửi body JSON và trả dữ liệu đã parse", async () => {
    responses.push(json(201, { maKQHT: 1 }));

    const result = await httpClient.post<{ maKQHT: number }>("/kqht", { tenKQHT: "Nhớ" });

    expect(result).toEqual({ maKQHT: 1 });
    expect(calls[0].url).toBe("/api/kqht");
    expect(calls[0].init.method).toBe("POST");
    expect(calls[0].init.body).toBe('{"tenKQHT":"Nhớ"}');
    expect(headerOf(calls[0], "Content-Type")).toBe("application/json");
    expect(headerOf(calls[0], "Accept")).toBe("application/json");
  });

  it("gửi FormData nguyên vẹn và để trình duyệt tự đặt Content-Type", async () => {
    responses.push(json(200, { tongDong: 0 }));
    const form = new FormData();
    form.append("file", new Blob(["x"]), "cau-hoi.docx");

    await httpClient.post("/nhap-cau-hoi/xem-truoc", form);

    expect(calls[0].init.body).toBe(form);
    expect(headerOf(calls[0], "Content-Type")).toBeNull();
  });

  it("thêm query string, bỏ giá trị rỗng và nối mảng bằng dấu phẩy", async () => {
    responses.push(json(200, []));

    await httpClient.get("/ky-thi", {
      query: { trangThai: ["da_phat_hanh", "dang_thi"], tuKhoa: "", trang: 2, maKQHT: undefined },
    });

    expect(calls[0].url).toBe("/api/ky-thi?trangThai=da_phat_hanh%2Cdang_thi&trang=2");
  });

  it("trả undefined cho 204", async () => {
    responses.push(new Response(null, { status: 204 }));

    await expect(httpClient.delete("/ky-thi/1/thi-sinh/SV1")).resolves.toBeUndefined();
  });

  it("đọc blob khi tải file", async () => {
    responses.push(new Response("a,b", { status: 200, headers: { "Content-Type": "text/csv" } }));

    const file = await httpClient.get<Blob>("/ky-thi/1/ket-qua/xuat", { responseType: "blob" });

    expect(file).toBeInstanceOf(Blob);
    expect(await file.text()).toBe("a,b");
  });

  it("chuyển ProblemDetails thành ApiError có code, errors và traceId", async () => {
    responses.push(json(400, {
      title: "Dữ liệu không hợp lệ",
      status: 400,
      code: "VALIDATION_FAILED",
      errors: { tenKQHT: ["Tên KQHT là bắt buộc."] },
      traceId: "00-abc",
    }, "application/problem+json"));

    const error = await httpClient.post("/kqht", {}).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    const apiError = error as ApiError;
    expect(apiError.status).toBe(400);
    expect(apiError.code).toBe("VALIDATION_FAILED");
    expect(apiError.message).toBe("Dữ liệu không hợp lệ");
    expect(apiError.errors).toEqual({ tenKQHT: ["Tên KQHT là bắt buộc."] });
    expect(apiError.traceId).toBe("00-abc");
  });

  it("ưu tiên detail làm thông báo lỗi", async () => {
    responses.push(json(409, { title: "Trùng", detail: "Đã tồn tại KQHT có ký hiệu A.", code: "KQHT_KY_HIEU_CONFLICT" }));

    await expect(httpClient.post("/kqht", {})).rejects.toMatchObject({
      status: 409,
      code: "KQHT_KY_HIEU_CONFLICT",
      message: "Đã tồn tại KQHT có ký hiệu A.",
    });
  });

  it("vẫn tạo ApiError khi lỗi không phải JSON", async () => {
    responses.push(new Response("Bad gateway", { status: 502 }));

    await expect(httpClient.get("/health")).rejects.toMatchObject({
      status: 502,
      code: "HTTP_ERROR",
      errors: {},
    });
  });

  it("báo NETWORK_ERROR khi không kết nối được máy chủ", async () => {
    responses.push(new TypeError("Failed to fetch"));

    await expect(httpClient.get("/health")).rejects.toMatchObject({ status: 0, code: "NETWORK_ERROR" });
  });

  it("không bọc lỗi hủy request", async () => {
    responses.push(new DOMException("Aborted", "AbortError"));

    await expect(httpClient.get("/health")).rejects.toMatchObject({ name: "AbortError" });
  });

  it("gắn access token vào header Authorization", async () => {
    authSession.setAccessToken("token-1");
    responses.push(json(200, {}));

    await httpClient.get("/xac-thuc/toi");

    expect(headerOf(calls[0], "Authorization")).toBe("Bearer token-1");
  });

  it("làm mới phiên một lần khi gặp 401 rồi gửi lại request", async () => {
    authSession.setAccessToken("cu");
    const refresh = vi.fn(async () => "moi");
    authSession.setRefreshHandler(refresh);
    responses.push(json(401, { code: "AUTH_TOKEN_EXPIRED" }), json(200, { ok: true }));

    const result = await httpClient.get("/cau-hoi");

    expect(result).toEqual({ ok: true });
    expect(refresh).toHaveBeenCalledTimes(1);
    expect(headerOf(calls[1], "Authorization")).toBe("Bearer moi");
    expect(authSession.getAccessToken()).toBe("moi");
  });

  it("các request 401 đồng thời dùng chung một lần làm mới", async () => {
    authSession.setAccessToken("cu");
    let resolveRefresh!: (token: string) => void;
    const refresh = vi.fn(() => new Promise<string>((resolve) => { resolveRefresh = resolve; }));
    authSession.setRefreshHandler(refresh);
    responses.push(json(401, {}), json(401, {}), json(200, { a: 1 }), json(200, { b: 2 }));

    const pending = Promise.all([httpClient.get("/a"), httpClient.get("/b")]);
    await vi.waitFor(() => expect(refresh).toHaveBeenCalled());
    resolveRefresh("moi");

    await expect(pending).resolves.toEqual([{ a: 1 }, { b: 2 }]);
    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it("xóa phiên và trả 401 khi làm mới thất bại", async () => {
    authSession.setAccessToken("cu");
    authSession.setRefreshHandler(async () => null);
    const onExpired = vi.fn();
    const unsubscribe = authSession.onSessionExpired(onExpired);
    responses.push(json(401, { code: "AUTH_TOKEN_EXPIRED" }));

    await expect(httpClient.get("/cau-hoi")).rejects.toMatchObject({ status: 401 });
    expect(authSession.getAccessToken()).toBeNull();
    expect(onExpired).toHaveBeenCalledTimes(1);
    expect(calls).toHaveLength(1);
    unsubscribe();
  });

  it("không làm mới phiên cho request skipAuth (đăng nhập sai mật khẩu)", async () => {
    const refresh = vi.fn(async () => "moi");
    authSession.setRefreshHandler(refresh);
    responses.push(json(401, { code: "AUTH_INVALID_CREDENTIALS" }));

    await expect(
      httpClient.post("/xac-thuc/dang-nhap", { tenDN: "a", matKhau: "b" }, { skipAuth: true }),
    ).rejects.toMatchObject({ code: "AUTH_INVALID_CREDENTIALS" });
    expect(refresh).not.toHaveBeenCalled();
  });
});

describe("buildQuery", () => {
  it("trả chuỗi rỗng khi không có tham số", () => {
    expect(buildQuery({ a: undefined, b: null, c: "" })).toBe("");
  });

  it("giữ số 0 và false", () => {
    expect(buildQuery({ trang: 0, an: false })).toBe("?trang=0&an=false");
  });
});
