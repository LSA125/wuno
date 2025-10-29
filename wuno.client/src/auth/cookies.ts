const COOKIE = "wuno_uid";
export const userCookieKey = COOKIE;


export function setCookie(value: string, days = 365) {
    const d = new Date();
    d.setTime(d.getTime() + days * 24 * 60 * 60 * 1000);
    document.cookie = `${COOKIE}=${value}; expires=${d.toUTCString()}; path=/; samesite=lax`;
}
export function getCookie() {
    const m = document.cookie.match(new RegExp("(?:^|; )" + COOKIE + "=([^;]*)"));
    return m ? decodeURIComponent(m[1]) : null;
}
export function clearCookie() {
    document.cookie = `${COOKIE}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
}