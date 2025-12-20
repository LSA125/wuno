const COOKIE = "wuno_uid";
export const userCookieKey = COOKIE;


export function setCookie(value: string, days = 365) {
    const d = new Date();
    d.setTime(d.getTime() + days * 24 * 60 * 60 * 1000);
    // Secure for HTTPS, SameSite=None for cross-origin requests (needed when API is on different subdomain)
    document.cookie = `${COOKIE}=${value}; expires=${d.toUTCString()}; path=/; SameSite=None; Secure`;
}
export function getCookie() {
    const m = document.cookie.match(new RegExp("(?:^|; )" + COOKIE + "=([^;]*)"));
    return m ? decodeURIComponent(m[1]) : null;
}
export function clearCookie() {
    document.cookie = `${COOKIE}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
}