const TOKEN_KEY = "uct_game_access_token";

export function getAuthToken() {
    return window.sessionStorage.getItem(TOKEN_KEY);
}

export function setAuthToken(token) {
    window.sessionStorage.setItem(TOKEN_KEY, token);
}

export function clearAuthToken() {
    window.sessionStorage.removeItem(TOKEN_KEY);
}

/** Attach the JWT to API requests; the server remains responsible for validating it. */
export async function authenticatedFetch(input, options = {}) {
    const { skipAuth = false, headers: suppliedHeaders, ...fetchOptions } = options;
    const headers = new Headers(suppliedHeaders);
    const token = !skipAuth ? getAuthToken() : null;

    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetch(input, { ...fetchOptions, headers });

    if (!skipAuth && response.status === 401) {
        clearAuthToken();
    }

    return response;
}

export default authenticatedFetch;
