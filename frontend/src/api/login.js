import { request } from "./client";
import { setAuthToken } from "../middleware/authMiddleware";

/** Sign in with the game's email/password account and retain its JWT for this tab. */
export async function login({ email, password }) {
    const response = await request("/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password }),
        skipAuth: true,
    });

    const token = response?.accessToken ?? response?.token;
    if (typeof token !== "string" || token.length === 0) {
        throw new Error("The sign-in response did not include an access token.");
    }

    setAuthToken(token);
    return response.user;
}

export default login;
