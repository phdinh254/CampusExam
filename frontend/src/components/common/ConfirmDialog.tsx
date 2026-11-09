import { useEffect, type ReactNode } from "react";

import { Icon } from "../Icon";

type ConfirmDialogProps = {
  title: string;
  children: ReactNode;
  confirmLabel: string;
  /** Nút xác nhận màu đỏ cho thao tác không hoàn tác được. */
  danger?: boolean;
  busy?: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
};

export function ConfirmDialog({ title, children, confirmLabel, danger, busy, error, onConfirm, onCancel }: ConfirmDialogProps) {
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !busy) onCancel();
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [busy, onCancel]);

  return (
    <div className="dialog-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !busy) onCancel(); }}>
      <section aria-labelledby="confirm-dialog-title" aria-modal="true" className="form-dialog confirm-dialog" role="alertdialog">
        <header>
          <div>
            <span className={`dialog-icon${danger ? " danger" : ""}`}><Icon name="alert" size={20} /></span>
            <h3 id="confirm-dialog-title">{title}</h3>
          </div>
        </header>
        <div className="dialog-body">
          {error && <div className="form-alert" role="alert"><Icon name="alert" size={16} /><span>{error}</span></div>}
          <div className="confirm-message">{children}</div>
        </div>
        <footer>
          <button autoFocus className="button button-secondary" disabled={busy} onClick={onCancel} type="button">Hủy</button>
          <button className={`button ${danger ? "button-danger" : "button-primary"}`} disabled={busy} onClick={onConfirm} type="button">
            {busy ? "Đang xử lý…" : confirmLabel}
          </button>
        </footer>
      </section>
    </div>
  );
}
