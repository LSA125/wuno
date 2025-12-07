const KEY = "pending_join_code";

export function setPendingJoin(code: string) {
    sessionStorage.setItem(KEY, (code || "").trim().toUpperCase());
}
export function getPendingJoin(): string | null {
    const v = sessionStorage.getItem(KEY);
    return v && v.trim() ? v : null;
}
export function clearPendingJoin() {
    sessionStorage.removeItem(KEY);
}
