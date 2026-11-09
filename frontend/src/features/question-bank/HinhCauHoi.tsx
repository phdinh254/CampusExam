import { useEffect, useState } from "react";

import { taiHinhCauHoi } from "./importApi";

type HinhCauHoiProps = { url: string; alt: string };

/** Tải ảnh qua httpClient (có access token) rồi hiển thị bằng object URL. */
export function HinhCauHoi({ url, alt }: HinhCauHoiProps) {
  const [src, setSrc] = useState<string | null>(null);
  const [loi, setLoi] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    let objectUrl: string | null = null;
    taiHinhCauHoi(url, controller.signal)
      .then((blob) => {
        objectUrl = URL.createObjectURL(blob);
        setSrc(objectUrl);
      })
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === "AbortError")) setLoi(true);
      });
    return () => {
      controller.abort();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [url]);

  if (loi) return <span className="question-image-fallback">Không tải được hình</span>;
  if (!src) return <span className="question-image-fallback">Đang tải…</span>;
  return <img alt={alt} className="question-image" src={src} />;
}
